using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Wallets;
using System.Collections.Generic;
using System.Linq;
using static Neo.Plugins.FSStorage.MorphEvent;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class BalanceContractProcessorTests : TestKit
    {
        private NeoSystem system;
        private BalanceContractProcessor processor;
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
                Blockchain = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor()))
            };
            processor = new BalanceContractProcessor()
            {
                Client = morphclient,
                ActiveState = new PositiveActiveState(),
                WorkPool = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor()))
            };
        }

        [TestMethod()]
        public void HandleLockTest()
        {
            processor.HandleLock(new LockEvent()
            {
                Id = new byte[] { 0x01 }
            });
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void ProcessLockTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            processor.ProcessLock(new LockEvent()
            {
                Id = new byte[] { 0x01 },
                Amount = 0,
                LockAccount = accounts.ToArray()[0].ScriptHash,
                UserAccount = accounts.ToArray()[0].ScriptHash
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult1>().tx;
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void ListenerHandlersTest()
        {
            var handlerInfos = processor.ListenerHandlers();
            Assert.AreEqual(handlerInfos.Length, 1);
        }

        [TestMethod()]
        public void ListenerParsersTest()
        {
            var parserInfos = processor.ListenerParsers();
            Assert.AreEqual(parserInfos.Length, 1);
        }

        [TestMethod()]
        public void ListenerTimersHandlersTest()
        {
            var handlerInfos = processor.TimersHandlers();
            Assert.IsNull(handlerInfos);
        }

        public class PositiveActiveState : IActiveState
        {
            public bool IsActive()
            {
                return true;
            }
        }

        public class NegativeActiveState : IActiveState
        {
            public bool IsActive()
            {
                return false;
            }
        }
    }
}
