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
    }
}
