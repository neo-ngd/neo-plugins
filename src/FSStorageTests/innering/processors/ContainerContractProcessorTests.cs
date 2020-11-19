using Akka.Actor;
using Akka.TestKit.Xunit2;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.SmartContract;
using Neo.Wallets;
using NeoFS.API.v2.Container;
using NeoFS.API.v2.Refs;
using System.Collections.Generic;
using System.Linq;
using Neo.VM;
using static Neo.Plugins.FSStorage.morph.invoke.MorphClient;
using static Neo.Plugins.FSStorage.morph.invoke.Tests.BalanceContractProcessorTests;
using static Neo.Plugins.FSStorage.MorphEvent;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class ContainerContractProcessorTests : TestKit
    {
        private NeoSystem system;
        private ContainerContractProcessor processor;
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
            processor = new ContainerContractProcessor()
            {
                Client = morphclient,
                ActiveState = new PositiveActiveState(),
                WorkPool = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor()))
            };
        }

        [TestMethod()]
        public void HandlePutTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            OwnerID ownerId = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new NeoFS.API.v2.Refs.Version(),
                BasicAcl = 0,
                Nonce = Google.Protobuf.ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new NeoFS.API.v2.Netmap.PlacementPolicy()
            };
            byte[] sig = Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);

            processor.HandlePut(new ContainerPutEvent()
            {
                RawContainer = container.ToByteArray(),
                PublicKey = key.PublicKey,
                Signature = sig
            });
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void HandleDeleteTest()
        {
            processor.HandleDelete(new ContainerDeleteEvent()
            {
                ContainerID = new byte[] { 0x01 },
                Signature = new byte[] { 0x01 }
            });
            var nt = ExpectMsg<BlockChainFakeActor.OperationResult2>().nt;
            Assert.IsNotNull(nt);
        }

        [TestMethod()]
        public void ProcessContainerPutTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            OwnerID ownerId = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new NeoFS.API.v2.Refs.Version(),
                BasicAcl = 0,
                Nonce = Google.Protobuf.ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new NeoFS.API.v2.Netmap.PlacementPolicy()
            };
            byte[] sig = Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            processor.ProcessContainerPut(new ContainerPutEvent()
            {
                PublicKey = key.PublicKey,
                Signature = sig,
                RawContainer = container.ToByteArray()
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult1>().tx;
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void ProcessContainerDeleteTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            OwnerID ownerId = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new NeoFS.API.v2.Refs.Version()
                {
                    Major = 1,
                    Minor = 1,
                },
                BasicAcl = 0,
                Nonce = Google.Protobuf.ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new NeoFS.API.v2.Netmap.PlacementPolicy()
            };

            byte[] sig = Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var containerContractHash = Settings.Default.ContainerContractHash;
            var script = containerContractHash.MakeScript("put", container.ToByteArray(), sig, key.PublicKey.EncodePoint(true));
            UInt160 account = accounts.ToArray()[0].ScriptHash;
            UInt160 from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators);
            Signers signers = new Signers(account);
            var engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            snapshot.Commit();

            var containerId = container.CalCulateAndGetID.Value.ToByteArray();
            sig = Crypto.Sign(containerId, key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            processor.ProcessContainerDelete(new ContainerDeleteEvent()
            {
                ContainerID = containerId,
                Signature = sig
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult1>().tx;
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void ListenerHandlersTest()
        {
            var handlerInfos = processor.ListenerHandlers();
            Assert.AreEqual(handlerInfos.Length, 2);
        }

        [TestMethod()]
        public void ListenerParsersTest()
        {
            var parserInfos = processor.ListenerParsers();
            Assert.AreEqual(parserInfos.Length, 2);
        }

        [TestMethod()]
        public void ListenerTimersHandlersTest()
        {
            var handlerInfos = processor.TimersHandlers();
            Assert.AreEqual(0, handlerInfos.Length);
        }
    }
}
