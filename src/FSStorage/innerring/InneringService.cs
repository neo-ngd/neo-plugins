using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Plugins.FSStorage.innerring.timers;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Text;
using static Neo.Plugins.FSStorage.innerring.timers.Timers;
using static Neo.Plugins.FSStorage.Listener;
using Neo.IO.Data.LevelDB;
using Neo.Plugins.util;

namespace Neo.Plugins.FSStorage.innerring
{
    public class InneringService : UntypedActor, IActiveState, IEpochState, IEpochTimerReseter
    {
        public class MainContractEvent { public NotifyEventArgs notify; };
        public class MorphContractEvent { public NotifyEventArgs notify; };
        public class Start { };
        public class Stop { };

        private IActorRef morphEventListener;
        private IActorRef mainEventListener;
        private IActorRef timer;

        private Client mainClient;
        private Client morphClient;

        public InneringService(NeoSystem system)
        {
            //build clients
            mainClient = new MainClient();
            morphClient = new MorphClient();
            //buidl processors
            BalanceContractProcessor balanceContractProcessor = new BalanceContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(10))
            };
            ContainerContractProcessor containerContractProcessor = new ContainerContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(10))
            };
            FsContractProcessor fsContractProcessor = new FsContractProcessor()
            {
                Client = mainClient,
                ActiveState = this,
                EpochState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(10))
            };
            NetMapContractProcessor netMapContractProcessor = new NetMapContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                EpochState = this,
                EpochTimerReseter = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(10))
            };
            balanceContractProcessor.WorkPool.Tell(new Timer());
            containerContractProcessor.WorkPool.Tell(new Timer());
            fsContractProcessor.WorkPool.Tell(new Timer());
            netMapContractProcessor.WorkPool.Tell(new Timer());

            //build listener
            morphEventListener = system.ActorSystem.ActorOf(Listener.Props());
            mainEventListener = system.ActorSystem.ActorOf(Listener.Props());
            timer = system.ActorSystem.ActorOf(Timers.Props());

            morphEventListener.Tell(new BindProcessorEvent() { processor = netMapContractProcessor });
            morphEventListener.Tell(new BindProcessorEvent() { processor = containerContractProcessor });
            morphEventListener.Tell(new BindProcessorEvent() { processor = balanceContractProcessor });
            mainEventListener.Tell(new BindProcessorEvent() { processor = fsContractProcessor });

            timer.Tell(new BindTimersEvent() { processor = netMapContractProcessor });
            timer.Tell(new BindTimersEvent() { processor = containerContractProcessor });
            timer.Tell(new BindTimersEvent() { processor = balanceContractProcessor });
            timer.Tell(new BindTimersEvent() { processor = fsContractProcessor });
        }


        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start start:
                    OnStart();
                    break;
                case Stop stop:
                    OnStop();
                    break;
                case MainContractEvent mainEvent:
                    OnMainContractEvent(mainEvent.notify);
                    break;
                case MorphContractEvent morphEvent:
                    OnMorphContractEvent(morphEvent.notify);
                    break;
                default:
                    break;
            }
        }

        public void OnStart()
        {
            timer.Tell(new Start());
        }
        public void OnStop()
        {
            timer.Tell(new Stop());
        }

        public void OnMainContractEvent(NotifyEventArgs notify)
        {
            mainEventListener.Tell(new NewContractEvent() { notify = notify });
        }

        public void OnMorphContractEvent(NotifyEventArgs notify)
        {
            morphEventListener.Tell(new NewContractEvent() { notify = notify });
        }


        bool IActiveState.IsActive()
        {
            throw new NotImplementedException();
        }

        void IEpochState.SetEpochCounter(ulong epoch)
        {
            throw new NotImplementedException();
        }

        ulong IEpochState.EpochCounter()
        {
            throw new NotImplementedException();
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new InneringService(system)).WithMailbox("MorphEventListener-mailbox");
        }

        public void ResetEpochTimer()
        {
            timer.Tell(new Timer() { });
        }
    }
}
