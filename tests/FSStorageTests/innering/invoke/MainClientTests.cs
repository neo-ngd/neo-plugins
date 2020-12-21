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
using Neo.IO;
using Neo.VM;
using System.Linq;

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

        public static void MockInvokeScript(Mock<RpcClient> mockClient, RpcRequest request, RpcInvokeResult result)
        {
            mockClient.Setup(p => p.RpcSendAsync("invokescript", It.Is<JObject[]>(j => j.ToString() == request.Params.ToString())))
                .ReturnsAsync(result.ToJson())
                .Verifiable();
        }

        [TestMethod()]
        public void InvokeFunctionTest()
        {
            MainClient client = new MainClient(new string[] { "http://seed1t.neo.org:20332" }, wallet);
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
            client.Clients = new RpcClient[] { mockRpc.Object };
            bool result = client.InvokeFunction(NativeContract.GAS.Hash, "balanceOf", (long)100, UInt160.Zero);
            Assert.AreEqual(result, true);
        }

        [TestMethod()]
        public void InvokeRemoteFunctionTest()
        {
            RpcClient client = new RpcClient("http://localhost:10337");
            var testtext = "ANF9xQSMzxIKAAAAAJ6FEwAAAAAADD8AAAHPR3Nsc4MO/Ze+ii+PMy5LqcDoa4AAeAwUYx7B1Zp7+C0w1hNG7V0KfHh2GoMCAOH1BQwUz0dzbHODDv2XvoovjzMuS6nA6GsMIGMewdWae/gtMNYTRu1dCnx4dhqDmNAd03FDHGwLGN3tFMAMBmNoZXF1ZQwUnI97r3kkCh6SBjJEApFapjVO8rtBYn1bUgFCDEAOaeKfVR9eit0aZMLQfnvgVCdpXrst6ffMjOOyG1lbEWI0Rba6NBxXaDx0Dk7Il4Iw68FxP9TE7C64rHXU0bpUKQwhA0jLu8VeeXrNRmhXCpVevk4Of7afSFqV05Kkw3h0DSDxC0GVRA14";
            var r = client.RpcSendAsync("sendrawtransaction", testtext).Result;
            Console.WriteLine(r.ToString());
        }
    }
}
