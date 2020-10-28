using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Plugins.FSStorage.innerring.timers;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using System;
using static Neo.Plugins.FSStorage.innerring.timers.Timers;
using static Neo.Plugins.FSStorage.Listener;
using Neo.Plugins.util;
using Neo.Wallets;
using Neo.Cryptography.ECC;
using static System.IO.Path;
using System.Text;
using Neo.IO.Data.LevelDB;
using System.IO;
using Neo.Wallets.NEP6;
using Neo.IO.Json;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage.innerring
{
    /// <summary>
    /// InneringService is the entry for processing all events related to the inner ring node.
    /// All events will be distributed according to type.(2 event types:MainContractEvent and MorphContractEvent)
    /// Life process:Start--->Assignment event--->Stop
    /// </summary>
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
        private readonly DB db;

        /// <summary>
        /// Constructor.
        /// 4 Tasks:
        /// 1)Build mainnet and morph clients
        /// 2)Build mainnet and morph contract event handlers
        /// 3)Build mainnet and morph event listeners
        /// 4)Initialization
        /// </summary>
        /// <param name="system">NeoSystem</param>
        public InneringService(NeoSystem system)
        {
            db = DB.Open(GetFullPath(Settings.Default.Path), new Options { CreateIfMissing = true });

            NEP6Wallet wallet=new NEP6Wallet(Settings.Default.WalletPath);
            wallet.Unlock(Settings.Default.Password);
            //Build clients
            mainClient = new MainClient(Settings.Default.Url, wallet);
            morphClient = new MorphClient() {
                Wallet = wallet,
                Blockchain = system.Blockchain,
            };
            //Build processors
            BalanceContractProcessor balanceContractProcessor = new BalanceContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.BalanceContractWorkersSize))
            };
            ContainerContractProcessor containerContractProcessor = new ContainerContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.ContainerContractWorkersSize))
            };
            FsContractProcessor fsContractProcessor = new FsContractProcessor()
            {
                Client = mainClient,
                ActiveState = this,
                EpochState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.FsContractWorkersSize))
            };
            NetMapContractProcessor netMapContractProcessor = new NetMapContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                EpochState = this,
                EpochTimerReseter = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.NetmapContractWorkersSize))
            };
            balanceContractProcessor.WorkPool.Tell(new Timer());
            containerContractProcessor.WorkPool.Tell(new Timer());
            fsContractProcessor.WorkPool.Tell(new Timer());
            netMapContractProcessor.WorkPool.Tell(new Timer());
            //Build listener
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
            //Initialization
            IEnumerator<WalletAccount> accounts=wallet.GetAccounts().GetEnumerator();
            while (accounts.MoveNext()) {
                InitConfig(mainClient, morphClient, accounts.Current.GetKey().PublicKey);
                break;
            }
            Self.Tell(new Start() { });
        }

        public void InitConfig(Client mainClient, Client morphClient, ECPoint publicKey)
        {
            long epoch = ContractInvoker.GetEpoch(morphClient);
            bool state = ContractInvoker.IsInnerRing(mainClient, publicKey);
            SetEpochCounter((ulong)epoch);
            SetActiveState(state);
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
            if(db!=null) db.Dispose();
        }

        public void OnMainContractEvent(NotifyEventArgs notify)
        {
            mainEventListener.Tell(new NewContractEvent() { notify = notify });
        }

        public void OnMorphContractEvent(NotifyEventArgs notify)
        {
            morphEventListener.Tell(new NewContractEvent() { notify = notify });
        }

        public void SetActiveState(bool state)
        {
            WriteBatch writeBatch = new WriteBatch();
            writeBatch.Put(Encoding.UTF8.GetBytes("ActiveState"), BitConverter.GetBytes(state));
            db.Write(WriteOptions.Default, writeBatch);
        }

        public bool IsActive()
        {
            byte[] value = db.Get(ReadOptions.Default, Encoding.UTF8.GetBytes("ActiveState"));
            if (value is null) return false;
            return BitConverter.ToBoolean(value);
        }

        public void SetEpochCounter(ulong epoch)
        {
            WriteBatch writeBatch = new WriteBatch();
            writeBatch.Put(Encoding.UTF8.GetBytes("Epoch"), BitConverter.GetBytes(epoch));
            db.Write(WriteOptions.Default, writeBatch);
        }

        public ulong EpochCounter()
        {
            byte[] value = db.Get(ReadOptions.Default, Encoding.UTF8.GetBytes("Epoch"));
            if (value is null) return 0;
            return BitConverter.ToUInt64(value);
        }

        public void ResetEpochTimer()
        {
            timer.Tell(new Timer() { });
        }

        public static Props Props(NeoSystem system)
        {
            return Akka.Actor.Props.Create(() => new InneringService(system)).WithMailbox("InneringService-mailbox");
        }
    }
}
