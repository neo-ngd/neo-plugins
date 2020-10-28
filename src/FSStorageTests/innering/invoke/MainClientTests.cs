using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using Moq;
using Neo.Network.RPC.Models;
using Neo.IO.Json;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class MainClientTests
    {
        private NeoSystem system;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = new MyWallet("test");
            wallet.CreateAccount("2931fe84623e29817503fd9529bb10472cbb02b4e2de404a8edbfdc669262e16".HexToBytes());
        }

        [TestMethod()]
        public void InvokeLocalFunctionTest()
        {
            MainClient client = new MainClient("http://seed1t.neo.org:20332", wallet);
            var mockRpc = new Mock<RpcClient>(MockBehavior.Strict, "http://seed1t.neo.org:20332", null, null);
            var test = TestUtils.RpcTestCases.Find(p => p.Name == "InvokeLocalScriptAsync");
            var request = test.Request;
            var response = test.Response;
            MockInvokeScript(mockRpc, request, RpcInvokeResult.FromJson(response.Result));
            client.Client = mockRpc.Object;
            InvokeResult result = client.InvokeLocalFunction(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);
            Assert.AreEqual(result.State, VM.VMState.HALT);
            Assert.AreEqual(result.GasConsumed > 0, true);
            Assert.AreEqual(result.ResultStack[0].GetInteger(), 0);
        }

        public static void MockInvokeScript(Mock<RpcClient> mockClient, RpcRequest request, RpcInvokeResult result)
        {
            mockClient.Setup(p => p.RpcSendAsync("invokescript", It.Is<JObject[]>(j => j.ToString() == request.Params.ToString())))
                .ReturnsAsync(result.ToJson())
                .Verifiable();
        }

        [TestMethod()]
        public void InvokeFunctionTest()
        {
            MainClient client = new MainClient("http://seed1t.neo.org:20332", wallet);
            var mockRpc = new Mock<RpcClient>(MockBehavior.Strict, "http://seed1t.neo.org:20332", null, null);
            // MockInvokeScript
            var test = TestUtils.RpcTestCases.Find(p => p.Name == "InvokeLocalScriptAsync");
            var request = test.Request;
            var response = test.Response;
            MockInvokeScript(mockRpc, request, RpcInvokeResult.FromJson(response.Result));
            // MockHeight
            mockRpc.Setup(p => p.RpcSendAsync("getblockcount")).ReturnsAsync(100).Verifiable();
            // MockCalculateNetworkfee
            var networkfee = new JObject();
            networkfee["networkfee"] = 100000000;
            mockRpc.Setup(p => p.RpcSendAsync("calculatenetworkfee", It.Is<JObject[]>(u => true)))
                .ReturnsAsync(networkfee)
                .Verifiable();
            // MockCalculatenetworkfee
            mockRpc.Setup(p => p.RpcSendAsync("sendrawtransaction", It.Is<JObject[]>(u => true)))
                .ReturnsAsync(true)
                .Verifiable();
            client.Client = mockRpc.Object;
            bool result = client.InvokeFunction(NativeContract.GAS.Hash, "balanceOf", (long)100, UInt160.Zero);
            Assert.AreEqual(result, true);
        }

        public class MyWallet : Wallet
        {
            public string path;

            public override string Name => "MyWallet";

            public override Version Version => Version.Parse("0.0.1");

            Dictionary<UInt160, WalletAccount> accounts = new Dictionary<UInt160, WalletAccount>();

            public MyWallet(string path) : base(path)
            {
            }

            public override bool ChangePassword(string oldPassword, string newPassword)
            {
                throw new NotImplementedException();
            }

            public override bool Contains(UInt160 scriptHash)
            {
                return accounts.ContainsKey(scriptHash);
            }

            public void AddAccount(WalletAccount account)
            {
                accounts.Add(account.ScriptHash, account);
            }

            public override WalletAccount CreateAccount(byte[] privateKey)
            {
                KeyPair key = new KeyPair(privateKey);
                Neo.Wallets.SQLite.VerificationContract contract = new Neo.Wallets.SQLite.VerificationContract
                {
                    Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                    ParameterList = new[] { ContractParameterType.Signature }
                };
                MyWalletAccount account = new MyWalletAccount(contract.ScriptHash);
                account.SetKey(key);
                account.Contract = contract;
                AddAccount(account);
                return account;
            }

            public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
            {
                MyWalletAccount account = new MyWalletAccount(contract.ScriptHash)
                {
                    Contract = contract
                };
                account.SetKey(key);
                AddAccount(account);
                return account;
            }

            public override WalletAccount CreateAccount(UInt160 scriptHash)
            {
                MyWalletAccount account = new MyWalletAccount(scriptHash);
                AddAccount(account);
                return account;
            }

            public override bool DeleteAccount(UInt160 scriptHash)
            {
                return accounts.Remove(scriptHash);
            }

            public override WalletAccount GetAccount(UInt160 scriptHash)
            {
                accounts.TryGetValue(scriptHash, out WalletAccount account);
                return account;
            }

            public override IEnumerable<WalletAccount> GetAccounts()
            {
                return accounts.Values;
            }

            public override bool VerifyPassword(string password)
            {
                return true;
            }
        }

        public class MyWalletAccount : WalletAccount
        {
            private KeyPair key = null;
            public override bool HasKey => key != null;

            public MyWalletAccount(UInt160 scriptHash)
                : base(scriptHash)
            {
            }

            public override KeyPair GetKey()
            {
                return key;
            }

            public void SetKey(KeyPair inputKey)
            {
                key = inputKey;
            }
        }
    }
}
