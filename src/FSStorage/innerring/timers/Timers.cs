using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.processors;
using System;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;

namespace Neo.Plugins.FSStorage.innerring.timers
{
    public class Timers : UntypedActor
    {
        public const string EpochTimer = "EpochTimer";
        public class Timer { }
        public class Start { };
        public class Stop { };
        public class BindTimersEvent { public IProcessor processor; };

        private ICancelable timer_token;
        private bool started = false;
        private long epochDuration = 0;
        private Action<IContractEvent> handler;

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case BindTimersEvent bindTimersEvent:
                    BindProcessor(bindTimersEvent.processor);
                    break;
                case Timer timer:
                    OnTimer();
                    break;
                case Start start:
                    OnStart();
                    break;
                case Stop stop:
                    OnStop();
                    break;
                default:
                    break;
            }
        }

        private void OnStart()
        {
            if (!started)
            {
                started = !started;
                OnTimer();

            }
        }

        private void OnStop()
        {
            if (started)
            {
                started = !started;
                timer_token.CancelIfNotNull();
            }
        }

        private void OnTimer()
        {
            if (started)
            {
                TimeSpan duration = TimeSpan.FromMilliseconds(epochDuration);
                timer_token.CancelIfNotNull();
                timer_token = Context.System.Scheduler.ScheduleTellOnceCancelable(duration, Self, new Timer { }, ActorRefs.NoSender);
                if (handler != null) handler(new NewEpochTickEvent() { });
            }
        }

        public void BindProcessor(IProcessor processor)
        {
            HandlerInfo[] handlers = processor.TimersHandlers();
            foreach (HandlerInfo handler in handlers)
            {
                RegisterHandler(handler);
            }
        }

        public void RegisterHandler(HandlerInfo p)
        {
            if (p.Handler is null) throw new Exception("ir/timers: can't register nil handler");
            switch (p.ScriptHashWithType.Type)
            {
                case EpochTimer:
                    this.handler = p.Handler;
                    break;
                default:
                    throw new Exception("ir/timers: unknown handler type");
            }
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new Timers());
        }
    }
}
