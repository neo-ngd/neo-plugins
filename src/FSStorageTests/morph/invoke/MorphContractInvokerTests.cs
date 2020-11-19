using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Wallets;
using System.Linq;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class MorphContractInvokerTests : TestKit
    {
        private MorphClient client;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            NeoSystem system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            client = new MorphClient()
            {
                Wallet = wallet,
                Blockchain = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor()))
            };
        }

        [TestMethod()]
        public void InvokeBalanceOfTest()
        {
            long result = MorphContractInvoker.InvokeBalanceOf(client, UInt160.Zero.ToArray());
            Assert.AreEqual(result, 0);
        }

        [TestMethod()]
        public void InvokeDecimalsTest()
        {
            long result = MorphContractInvoker.InvokeDecimals(client);
            Assert.AreEqual(result, 12);
        }
    }
}
