using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public class MainClient : Client
    {
        public Wallet Wallet;
        public RpcClient Client;

        public MainClient(string url, Wallet wallet)
        {
            this.Client = new RpcClient(url);
            this.Wallet = wallet;
        }

        public bool InvokeFunction(UInt160 contractHash, string method, long fee, params object[] args)
        {
            InvokeResult result = InvokeLocalFunction(contractHash, method, args);
            var blockHeight = (uint)(Client.RpcSendAsync("getblockcount").Result.AsNumber());
            Random rand = new Random();
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = result.Script,
                ValidUntilBlock = blockHeight + Transaction.MaxValidUntilBlockIncrement,
                Signers = new Signer[] { new Signer() { Account = Wallet.GetAccounts().ToArray()[0].ScriptHash } },
                Attributes = System.Array.Empty<TransactionAttribute>(),
                SystemFee = result.GasConsumed + fee,
                NetworkFee = 0
            };
            var data = new ContractParametersContext(tx);
            Wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var networkFee = (uint)Client.RpcSendAsync("calculatenetworkfee", tx.ToArray().ToHexString()).Result["networkfee"].AsNumber();
            tx.NetworkFee = networkFee;
            data = new ContractParametersContext(tx);
            Wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            return Client.RpcSendAsync("sendrawtransaction", tx.ToArray().ToHexString()).Result.AsBoolean();
        }

        public InvokeResult InvokeLocalFunction(UInt160 contractHash, string method, params object[] args)
        {
            byte[] script = contractHash.MakeScript(method, args);
            List<JObject> parameters = new List<JObject> { script.ToHexString() };
            Signer[] signers = new Signer[] { new Signer() { Account = Wallet.GetAccounts().ToArray()[0].ScriptHash } };
            if (signers.Length > 0)
            {
                parameters.Add(signers.Select(p => p.ToJson()).ToArray());
            }
            var result = Client.RpcSendAsync("invokescript", parameters.ToArray()).Result;
            RpcInvokeResult rpcInvokeResult = RpcInvokeResult.FromJson(result);
            return new InvokeResult()
            {
                Script = Convert.FromBase64String(rpcInvokeResult.Script),
                State = rpcInvokeResult.State,
                GasConsumed = long.Parse(rpcInvokeResult.GasConsumed),
                ResultStack = rpcInvokeResult.Stack
            };
        }
    }
}
