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
        private string name;
        private int capacity;
        private int running;

        public class Timer { }
        public class NewTask { public string process; public Task task;};
        public class CompleteTask { };

        private long duration = 20000;
        private ICancelable timer_token;
        private List<Task> taskArray;

        public WorkerPool(string name,int capacity)
        {
            this.name = name;
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
            var actor = Self;
            timer_token.CancelIfNotNull();
            timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromMilliseconds(duration), Self, new Timer { }, ActorRefs.NoSender);

            foreach (Task task in taskArray)
            {
                task.ContinueWith(t => { actor.Tell(new CompleteTask()); });
                task.Start();
            }
            taskArray.Clear();
        }

        private void OnNewTask(NewTask newTask)
        {
            int free = capacity - running;
            if (free == 0)
            {
                Utility.Log(newTask.process, LogLevel.Warning, string.Format("worker pool drained,capacity:{0}", capacity.ToString()));
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

        public static Props Props(string name,int capacity)
        {
            return Akka.Actor.Props.Create(() => new WorkerPool(name,capacity));
        }
    }
}
