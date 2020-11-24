using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo.Plugins.util
{
    /// <summary>
    /// WorkerPool is a thread pool that uses Akka's message mechanism internally.
    /// Multiple instances can be created, and the task scheduling is independent.
    /// Through its internal timer, it will periodically select a certain count of
    /// tasks from the task list for execution.
    /// [free=capacity-running]
    /// </summary>
    public class WorkerPool : UntypedActor
    {
        private int capacity;
        private int running;

        public class Timer { }
        public class NewTask { public string process; public Task task; };
        public class CompleteTask { };

        private long duration = 100;
        private ICancelable timer_token;
        private List<Task> taskArray;

        public WorkerPool(int capacity)
        {
            this.capacity = capacity;
            taskArray = new List<Task>();
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
            for (int i = 0; i < taskArray.Count; i++)
            {
                Task task = taskArray[i];
                task.ContinueWith(t => { Self.Tell(new CompleteTask()); });
                task.Start();
            }
            taskArray.Clear();
        }

        private void OnNewTask(NewTask newTask)
        {
            int free = capacity - running;
            if (free == 0)
            {
                Dictionary<string, string> pairs = new Dictionary<string, string>();
                pairs.Add("capacity", capacity.ToString());
                Utility.Log(string.Format("{0} processor worker pool drained", newTask.process), LogLevel.Warning, pairs.ParseToString());
                Console.WriteLine(free);
            }
            else
            {
                taskArray.Add(newTask.task);
                running++;
            }
        }

        private void OnCompleteTask()
        {
            running--;
        }

        public static Props Props(int capacity)
        {
            return Akka.Actor.Props.Create(() => new WorkerPool(capacity));
        }
    }
}
