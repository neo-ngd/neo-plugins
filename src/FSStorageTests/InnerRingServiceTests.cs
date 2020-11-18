using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.FSStorage.innerring;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class InnerRingServiceTests : TestKit
    {
        private NeoSystem system;
        private NEP6Wallet wallet;
        private IActorRef innerring;
        private Client client;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            client = new MorphClient()
            {
                Wallet = wallet,
                Blockchain = TestActor
            };
            innerring = system.ActorSystem.ActorOf(Props.Create(() => new innerring.InnerRingService(system, wallet, client, client)));
        }

        [TestMethod()]
        public void InitConfigTest()
        {
            innerring.Tell(new InnerRingService.Start());
        }
    }
}
