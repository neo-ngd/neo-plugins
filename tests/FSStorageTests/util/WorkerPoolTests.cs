using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.util;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using static Neo.Plugins.util.WorkerPool;
using System.Threading.Tasks;
using System;

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
            workerpool = system.ActorSystem.ActorOf(WorkerPool.Props("test",2));
        }

        [TestMethod()]
        public void NewTaskAndCompleteTaskTest()
        {
            workerpool.Tell(new NewTask() { process = "aaa", task = new Task(() => { Console.WriteLine("aaa"); }) });
            workerpool.Tell(new NewTask() { process = "bbb", task = new Task(() => { Console.WriteLine("bbb"); }) });
            workerpool.Tell(new Timer());
            workerpool.Tell(new CompleteTask());
        }
    }
}
