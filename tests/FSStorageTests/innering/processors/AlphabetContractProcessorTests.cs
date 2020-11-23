using Akka.Actor;
using Akka.TestKit.Xunit2;
using FSStorageTests.innering.processors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Wallets;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class AlphabetContractProcessorTests : TestKit
    {
        private NeoSystem system;
        private AlphabetContractProcessor processor;
        private MorphClient morphclient;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            morphclient = new MorphClient()
            {
                Wallet = wallet,
                Blockchain = system.ActorSystem.ActorOf(Props.Create(() => new ProcessorFakeActor()))
            };
            processor = new AlphabetContractProcessor()
            {
                Client = morphclient,
                Indexer = new Indexer(),
                WorkPool = system.ActorSystem.ActorOf(Props.Create(() => new ProcessorFakeActor()))
            };
        }

        [TestMethod()]
        public void HandleHandleGasEmissionTest()
        {
            processor.HandleGasEmission(new NewAlphabetEmitTickEvent());
            var nt = ExpectMsg<ProcessorFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void ProcessEmitTest()
        {
            processor.ProcessEmit(new NewAlphabetEmitTickEvent());
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void ListenerHandlersTest()
        {
            var handlerInfos = processor.ListenerHandlers();
            Assert.AreEqual(0, handlerInfos.Length);
        }

        [TestMethod()]
        public void ListenerParsersTest()
        {
            var parserInfos = processor.ListenerParsers();
            Assert.AreEqual(0, parserInfos.Length);
        }

        [TestMethod()]
        public void ListenerTimersHandlersTest()
        {
            var handlerInfos = processor.TimersHandlers();
            Assert.AreEqual(handlerInfos.Length, 1);
        }

        public class Indexer : IIndexer
        {
            public int Index()
            {
                return 0;
            }

            public void SetIndexer(int index)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
