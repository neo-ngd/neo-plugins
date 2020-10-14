using Akka.Actor;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
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
        private IActorRef localNode;

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

        public MorphClient()
        {

        }

        public MorphClient(Wallet wallet, IActorRef localNode, long fee)
        {
            this.wallet = wallet;
            this.localNode = localNode;
        }

        public bool InvokeFunction(UInt160 contractHash, string method, long fee, object[] args = null)
        {
            InvokeResult result = InvokeLocalFunction(contractHash, method, args);
            if (result.State != VMState.HALT) return false;

            using (SnapshotView snapshot = Blockchain.Singleton.GetSnapshot())
            {
                Random rand = new Random();
                Transaction tx = new Transaction
                {
                    Version = 0,
                    Nonce = (uint)rand.Next(),
                    Script = result.Script,
                    ValidUntilBlock = snapshot.Height + Transaction.MaxValidUntilBlockIncrement,
                    Signers = new Signer[] { new Signer() { Account = wallet.GetAccounts().ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                    Attributes = System.Array.Empty<TransactionAttribute>(),
                };
                tx.SystemFee = result.GasConsumed + fee;
                //todo
                tx.NetworkFee = 0;//wallet.CalculateNetworkFee();
                var data = new ContractParametersContext(tx);
                wallet.Sign(data);
            }
            return true;
        }

        public InvokeResult InvokeLocalFunction(UInt160 contractHash, string method, params object[] args)
        {
            byte[] script = contractHash.MakeScript(method, args);
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            Signers signers = new Signers(accounts.ToArray()[0].ScriptHash);
            return GetInvokeResult(script, signers);
        }

        private InvokeResult GetInvokeResult(byte[] script, Signers signers = null, bool testMode = true)
        {
            ApplicationEngine engine = ApplicationEngine.Run(script, container: signers, null, 0, testMode);
            return new InvokeResult() { State = engine.State, GasConsumed = (long)engine.GasConsumed, Script = script, ResultStack = engine.ResultStack.ToArray<StackItem>() };
        }

        public void TransferGas(UInt160 receiver, long amount)
        {

        }
    }
}
