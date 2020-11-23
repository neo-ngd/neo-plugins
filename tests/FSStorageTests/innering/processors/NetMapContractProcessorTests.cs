using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Wallets;
using static Neo.Plugins.FSStorage.morph.invoke.Tests.BalanceContractProcessorTests;
using static Neo.Plugins.FSStorage.MorphEvent;
using static Neo.Plugins.FSStorage.morph.invoke.Tests.FsContractProcessorTests;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;
using System.Collections.Generic;
using System.Linq;
using NeoFS.API.v2.Netmap;
using Neo.IO;
using Google.Protobuf;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class NetMapContractProcessorTests : TestKit
    {
        private NeoSystem system;
        private NetMapContractProcessor processor;
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
            processor = new NetMapContractProcessor()
            {
                Client = morphclient,
                ActiveState = new PositiveActiveState(),
                EpochState = new EpochState(),
                EpochTimerReseter = new EpochTimerReseter(),
                WorkPool = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor())),
                NetmapSnapshot = new NetMapContractProcessor.CleanupTable(true, 1)
            };
        }

        [TestMethod()]
        public void HandleNewEpochTickTest()
        {
            processor.HandleNewEpochTick(new NewEpochTickEvent());
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void HandleNewEpochTest()
        {
            processor.HandleNewEpoch(new NewEpochEvent());
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void HandleAddPeerTest()
        {
            processor.HandleAddPeer(new AddPeerEvent());
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void HandleUpdateStateTest()
        {
            IEnumerator<WalletAccount> accounts = wallet.GetAccounts().GetEnumerator();
            accounts.MoveNext();
            processor.HandleUpdateState(new UpdatePeerEvent()
            {
                PublicKey = accounts.Current.GetKey().PublicKey,
                Status = 2
            });
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void HandleCleanupTickTest()
        {
            processor.HandleCleanupTick(new NetmapCleanupTickEvent());
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void ListenerHandlersTest()
        {
            var handlerInfos = processor.ListenerHandlers();
            Assert.AreEqual(handlerInfos.Length, 3);
        }

        [TestMethod()]
        public void ListenerParsersTest()
        {
            var parserInfos = processor.ListenerParsers();
            Assert.AreEqual(parserInfos.Length, 3);
        }

        [TestMethod()]
        public void ListenerTimersHandlersTest()
        {
            var handlerInfos = processor.TimersHandlers();
            Assert.AreEqual(handlerInfos.Length, 1);
        }

        [TestMethod()]
        public void ProcessNewEpochTest()
        {
            processor.ProcessNewEpoch(new NewEpochEvent()
            {
                EpochNumber = 1
            });
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void ProcessNewEpochTickTest()
        {
            processor.ProcessNewEpochTick(new NewEpochTickEvent());
        }

        [TestMethod()]
        public void ProcessAddPeerTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            var nodeInfo = new NodeInfo()
            {
                PublicKey = Google.Protobuf.ByteString.CopyFrom(key.PublicKey.ToArray()),
                Address = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToAddress(key.PublicKey.ToArray()),
                State = NodeInfo.Types.State.Online
            };
            processor.ProcessAddPeer(new AddPeerEvent()
            {
                Node = nodeInfo.ToByteArray()
            });
        }

        [TestMethod()]
        public void ProcessUpdateStateTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            var nodeInfo = new NodeInfo()
            {
                PublicKey = Google.Protobuf.ByteString.CopyFrom(key.PublicKey.ToArray()),
                Address = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToAddress(key.PublicKey.ToArray()),
                State = NodeInfo.Types.State.Online
            };
            processor.ProcessUpdateState(new UpdatePeerEvent()
            {
                PublicKey = key.PublicKey,
                Status = (int)NodeInfo.Types.State.Offline
            });
        }

        [TestMethod()]
        public void ProcessNetmapCleanupTickTest()
        {
            processor.ProcessNetmapCleanupTick(new NetmapCleanupTickEvent()
            {
                Epoch = 1
            });
        }

        public class EpochTimerReseter : IEpochTimerReseter
        {
            public void ResetEpochTimer()
            {
                return;
            }
        }
    }
}
