using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract.Native;
using Neo.Wallets;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class MorphClientTests : TestKit
    {
        private NeoSystem system;
        private MorphClient client;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            wallet = TestBlockchain.wallet;
            client = new MorphClient()
            {
                Wallet = wallet,
                Blockchain = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor()))
            };
        }

        [TestMethod()]
        public void InvokeLocalFunctionTest()
        {
            InvokeResult result = client.InvokeLocalFunction(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);
            Assert.AreEqual(result.State, VM.VMState.HALT);
            Assert.AreEqual(result.GasConsumed, 2007750);
            Assert.AreEqual(result.ResultStack[0].GetInteger(), 0);
        }

        [TestMethod()]
        public void InvokeFunctionTest()
        {
            client.InvokeFunction(NativeContract.GAS.Hash, "balanceOf", 0, UInt160.Zero);
            var result = ExpectMsg<BlockChainFakeActor.OperationResult>().tx;
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void TransferGasTest()
        {
            client.TransferGas(UInt160.Zero, 0);
            var result = ExpectMsg<BlockChainFakeActor.OperationResult>().tx;
            Assert.IsNotNull(result);
        }

        public class BlockChainFakeActor : ReceiveActor
        {
            public BlockChainFakeActor()
            {
                Receive<Transaction>(create =>
                {
                    Sender.Tell(new OperationResult() { tx = create });
                });
            }

            public class OperationResult { public Transaction tx; };
        }
    }
}
