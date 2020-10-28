using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.util;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using static Neo.Plugins.util.WorkerPool;
using System.Threading.Tasks;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class WorkerPoolTests : TestKit
    {
        private NeoSystem system;
        private IActorRef workerpool;

        [TestInitialize]
        public void TestSetup()
        {
            system = TestBlockchain.TheNeoSystem;
            workerpool = system.ActorSystem.ActorOf(WorkerPool.Props(100));
        }

        [TestMethod()]
        public void NewTaskAndCompleteTaskTest()
        {
            workerpool.Tell(new NewTask() { task = new Task(() => { }) });
            workerpool.Tell(new CompleteTask());
        }
    }
}
