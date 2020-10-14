using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Neo.Plugins.util.WorkerPool;

namespace Neo.Plugins.util
{
    public class WorkerPool : UntypedActor
    {
        private int poolSize;
        private int hasUsed;
        public class Timer { }

        public class NewTask { public Task task; };

        public class CompleteTask { };

        private long duration = 0;
        private ICancelable timer_token;
        private List<Task> taskArray;

        public WorkerPool(int poolSize)
        {
            this.poolSize = poolSize;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Timer timer:
                    OnTimer();
                    break;
                case NewTask newTask:
                    OnNewTask(newTask);
                    break;
                case CompleteTask completeTask:
                    OnCompleteTask();
                    break;
                default:
                    break;
            }
        }

        private void OnTimer()
        {
            timer_token.CancelIfNotNull();
            timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromMilliseconds(duration), Self, new Timer { }, ActorRefs.NoSender);
            int canUse = poolSize - hasUsed;
            if (canUse <= 0) return;
            for (int i = 0; i < canUse; i++)
            {
                Task task = taskArray[i];
                task.ContinueWith(t => { Self.Tell(new CompleteTask()); });
                task.Start();
            }
            for (int i = 0; i < canUse; i++)
            {
                taskArray.RemoveAt(0);
            }
        }

        private void OnNewTask(NewTask newTask)
        {
            taskArray.Add(newTask.task);
        }

        private void OnCompleteTask()
        {
            hasUsed--;
        }

        public static Props Props(int PoolSize)
        {
            return Akka.Actor.Props.Create(() => new WorkerPool(PoolSize)).WithMailbox("Timers-mailbox");
        }
    }
}
