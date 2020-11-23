using Akka.Actor;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public class MorphClient : Client
    {
        private Wallets.Wallet wallet;
        private IActorRef blockchain;

        public Wallet Wallet { get => wallet; set => wallet = value; }
        public IActorRef Blockchain { get => blockchain; set => blockchain = value; }

        public class Signers : IVerifiable
        {
            private readonly UInt160[] _hashForVerify;
            Witness[] IVerifiable.Witnesses { get; set; }

            int ISerializable.Size => throw new NotImplementedException();

            void ISerializable.Deserialize(BinaryReader reader)
            {
                throw new NotImplementedException();
            }

            void IVerifiable.DeserializeUnsigned(BinaryReader reader)
            {
                throw new NotImplementedException();
            }

            public Signers(params UInt160[] hashForVerify)
            {
                _hashForVerify = hashForVerify ?? new UInt160[0];
            }

            UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
            {
                return _hashForVerify;
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }

            void IVerifiable.SerializeUnsigned(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public bool InvokeFunction(UInt160 contractHash, string method, long fee, params object[] args)
        {
            InvokeResult result = InvokeLocalFunction(contractHash, method, args);
            if (result.State != VMState.HALT) return false;

            StoreView snapshot = Ledger.Blockchain.Singleton.GetSnapshot().Clone();
            Random rand = new Random();
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = result.Script,
                ValidUntilBlock = snapshot.Height + Transaction.MaxValidUntilBlockIncrement,
                Signers = new Signer[] { new Signer() { Account = Wallet.GetAccounts().ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                Attributes = System.Array.Empty<TransactionAttribute>(),
            };
            tx.SystemFee = result.GasConsumed + fee;
            //todo
            tx.NetworkFee = wallet.CalculateNetworkFee(snapshot, tx);
            var data = new ContractParametersContext(tx);
            Wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            Blockchain.Tell(tx);
            return true;
        }

        public InvokeResult InvokeLocalFunction(UInt160 contractHash, string method, params object[] args)
        {
            byte[] script = contractHash.MakeScript(method, args);
            IEnumerable<WalletAccount> accounts = Wallet.GetAccounts();
            Signers signers = new Signers(accounts.ToArray()[0].ScriptHash);
            return GetInvokeResult(script, signers);
        }

        private InvokeResult GetInvokeResult(byte[] script, Signers signers = null, bool testMode = true)
        {
            StoreView snapshot = Ledger.Blockchain.Singleton.GetSnapshot().Clone();
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 20000000000);
            return new InvokeResult() { State = engine.State, GasConsumed = (long)engine.GasConsumed, Script = script, ResultStack = engine.ResultStack.ToArray<StackItem>() };
        }

        public void TransferGas(UInt160 to, long amount)
        {
            UInt160 assetId = NativeContract.GAS.Hash;
            AssetDescriptor descriptor = new AssetDescriptor(assetId);
            BigDecimal pamount = BigDecimal.Parse(amount.ToString(), descriptor.Decimals);
            Transaction tx = wallet.MakeTransaction(new[]
            {
                new TransferOutput
                {
                    AssetId = assetId,
                    Value = pamount,
                    ScriptHash = to
                }
            });
            if (tx == null) throw new Exception("Insufficient funds");
            ContractParametersContext data = new ContractParametersContext(tx);
            wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            Blockchain.Tell(tx);
        }
    }
}
