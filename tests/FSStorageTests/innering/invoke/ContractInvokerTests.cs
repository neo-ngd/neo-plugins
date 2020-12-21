using Akka.Actor;
using Akka.TestKit.Xunit2;
using FSStorageTests.innering.processors;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.Plugins.FSStorage.innerring.invoke.ContractInvoker;
using static Neo.Plugins.FSStorage.morph.invoke.MorphClient;
using Container = NeoFS.API.v2.Container.Container;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class ContractInvokerTests : TestKit
    {
        private MorphClient morphclient;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            NeoSystem system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            morphclient = new MorphClient()
            {
                Wallet = wallet,
                Blockchain = system.ActorSystem.ActorOf(Props.Create(() => new ProcessorFakeActor()))
            };
        }

        [TestMethod()]
        public void InvokeTransferBalanceXTest()
        {
            bool result = ContractInvoker.TransferBalanceX(morphclient, new TransferXParams()
            {
                Sender = UInt160.Zero.ToArray(),
                Receiver = UInt160.Zero.ToArray(),
                Amount = 0,
                Comment = new byte[] { 0x01 }
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeMintTest()
        {
            bool result = ContractInvoker.Mint(morphclient, new MintBurnParams()
            {
                ScriptHash = Settings.Default.NetmapContractHash.ToArray(),
                Amount = 0,
                Comment = new byte[] { 0x01 }
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeBurnTest()
        {
            bool result = ContractInvoker.Burn(morphclient, new MintBurnParams()
            {
                ScriptHash = Settings.Default.NetmapContractHash.ToArray(),
                Amount = 0,
                Comment = new byte[] { 0x01 }
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeLockAssetTest()
        {
            LockParams lockparam = new LockParams()
            {
                ID = new byte[] { 0x01 },
                UserAccount = UInt160.Zero,
                LockAccount = UInt160.Zero,
                Amount = 0,
                Until = 100,
            };
            bool result = ContractInvoker.LockAsset(morphclient, lockparam);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeBalancePrecisionTest()
        {
            uint result = ContractInvoker.BalancePrecision(morphclient);
            Assert.AreEqual(result, (uint)12);
        }

        [TestMethod()]
        public void InvokeRegisterContainerTest()
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
            bool result = ContractInvoker.RegisterContainer(morphclient, new ContainerParams()
            {
                Key = key.PublicKey,
                Container = container.ToByteArray(),
                Signature = sig
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeRemoveContainerTest()
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
            FakeSigners signers = new FakeSigners(account);
            var engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            snapshot.Commit();

            var containerId = container.CalCulateAndGetID.Value.ToByteArray();
            sig = Crypto.Sign(containerId, key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            var result = ContractInvoker.RemoveContainer(morphclient, new RemoveContainerParams()
            {
                ContainerID = containerId,
                Signature = sig
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeGetEpochTest()
        {
            long result = ContractInvoker.GetEpoch(morphclient);
            Assert.AreEqual(result, 2);
        }

        [TestMethod()]
        public void InvokeSetNewEpochTest()
        {
            bool result = ContractInvoker.SetNewEpoch(morphclient, 100);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeApproveAndUpdatePeerTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            var nodeInfo = new NodeInfo()
            {
                PublicKey = Google.Protobuf.ByteString.CopyFrom(key.PublicKey.ToArray()),
                Address = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToAddress(key.PublicKey.ToArray()),
                State = NodeInfo.Types.State.Online
            };
            bool result = ContractInvoker.ApprovePeer(morphclient, nodeInfo.ToByteArray());
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
            result = ContractInvoker.UpdatePeerState(morphclient, new UpdatePeerArgs()
            {
                Key = key.PublicKey,
                Status = (int)NodeInfo.Types.State.Offline
            });
            tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeSetConfigTest()
        {
            bool result = ContractInvoker.SetConfig(morphclient, new SetConfigArgs()
            {
                Id = new byte[] { 0x01 },
                Key = Utility.StrictUTF8.GetBytes("ContainerFee"),
                Value = BitConverter.GetBytes(0)
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeUpdateInnerRingTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            bool result = ContractInvoker.UpdateInnerRing(morphclient, new ECPoint[] { key.PublicKey });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeNetmapSnapshotTest()
        {
            NodeInfo[] result = ContractInvoker.NetmapSnapshot(morphclient);
            Assert.AreEqual(result.Length, 1);
        }

        [TestMethod()]
        public void InvokeAlphabetEmitTest()
        {
            bool result = ContractInvoker.AlphabetEmit(morphclient, 0);
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeIsInnerRingTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            bool result = ContractInvoker.IsInnerRing(morphclient, key.PublicKey);
            Assert.AreEqual(result, true);
        }

        [TestMethod()]
        public void InvokeInnerRingIndexTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            KeyPair key = accounts.ToArray()[0].GetKey();
            int result = ContractInvoker.InnerRingIndex(morphclient, key.PublicKey);
            Assert.AreEqual(result, 0);
        }

        [TestMethod()]
        public void InvokeCashOutChequeTest()
        {
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            bool result = ContractInvoker.CashOutCheque(morphclient, new ChequeParams()
            {
                Id = new byte[] { 0x01 },
                Amount = 1,
                LockAccount = accounts.ToArray()[0].ScriptHash,
                UserAccount = accounts.ToArray()[0].ScriptHash
            });
            var tx = ExpectMsg<ProcessorFakeActor.OperationResult1>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }
    }
}
