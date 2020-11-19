using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.FSStorage.innerring;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.Wallets.NEP6;

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
