using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class ListenerTests : TestKit, IProcessor
    {
        private IActorRef listener;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            wallet = TestBlockchain.wallet;
            listener = Sys.ActorOf(Props.Create(() => new Listener()));
            listener.Tell(new Listener.BindProcessorEvent() { processor = this });
        }

        [TestMethod()]
        public void OnStartAndOnStopAndNewContractEventTest()
        {
            listener.Tell(new Listener.Start());

            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0,
                Nonce = 0,
                Script = new byte[] { 0x01 },
                Signers = new Signer[] { new Signer() { Account = wallet.GetAccounts().ToArray()[0].ScriptHash } },
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();

            JArray obj = new JArray();
            obj.Add(tx.ToArray().ToHexString());
            obj.Add(UInt160.Zero.ToString());
            obj.Add("test");
            obj.Add(new JArray(new VM.Types.Boolean(true).ToJson()));

            NotifyEventArgs notify = FSStorage.GetNotifyEventArgsFromJson(obj);
            listener.Tell(new Listener.NewContractEvent()
            {
                notify = notify
            });
            var result = ExpectMsg<IContractEvent>();
            Assert.IsNotNull(result);

            listener.Tell(new Listener.Stop());
        }

        public ParserInfo[] ListenerParsers()
        {
            ParserInfo parserInfo = new ParserInfo()
            {
                ScriptHashWithType = new ScriptHashWithType()
                {
                    Type = "test",
                    ScriptHashValue = UInt160.Zero
                },
                Parser = ParseContractEvent
            };
            return new ParserInfo[] { parserInfo };
        }

        public HandlerInfo[] ListenerHandlers()
        {
            HandlerInfo handlerInfo = new HandlerInfo()
            {
                ScriptHashWithType = new ScriptHashWithType()
                {
                    Type = "test",
                    ScriptHashValue = UInt160.Zero
                },
                Handler = F
            };
            return new HandlerInfo[] { handlerInfo };
        }

        public HandlerInfo[] TimersHandlers()
        {
            return null;
        }

        private void F(IContractEvent contractEvent)
        {
            TestActor.Tell(contractEvent);
        }

        public IContractEvent ParseContractEvent(VM.Types.Array eventParams)
        {
            return new TestContractEvent();
        }

        public class TestContractEvent : IContractEvent
        {
            public void ContractEvent()
            {
            }
        }
    }
}
