using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Plugins.FSStorage.innerring.timers;
using System;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class TimersTests : TestKit, IProcessor
    {
        private IActorRef timers;

        [TestInitialize]
        public void TestSetup()
        {
            timers = Sys.ActorOf(Props.Create(() => new Timers()));
            timers.Tell(new Timers.BindTimersEvent() { processor = this });
        }

        [TestMethod()]
        public void OnTimerTest()
        {
            timers.Tell(new Timers.Start());
            var ce = ExpectMsg<IContractEvent>();
            Assert.IsNotNull(ce);
        }

        private void F(IContractEvent contractEvent)
        {
            TestActor.Tell(contractEvent);
        }

        public ParserInfo[] ListenerParsers()
        {
            throw new System.NotImplementedException();
        }

        public HandlerInfo[] ListenerHandlers()
        {
            throw new System.NotImplementedException();
        }

        public HandlerInfo[] TimersHandlers()
        {
            HandlerInfo handlerInfo = new HandlerInfo()
            {
                ScriptHashWithType = new ScriptHashWithType()
                {
                    Type = Timers.EpochTimer
                },
                Handler = F
            };
            return new HandlerInfo[] { handlerInfo };
        }
    }
}
