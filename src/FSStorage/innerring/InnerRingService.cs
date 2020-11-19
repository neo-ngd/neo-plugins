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
using static System.IO.Path;
using System.Text;
using Neo.IO.Data.LevelDB;
using Neo.Wallets.NEP6;
using System.Collections.Generic;
using Neo.Plugins.innerring.processors;
using System.Linq;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;

namespace Neo.Plugins.FSStorage.innerring
{
    /// <summary>
    /// InneringService is the entry for processing all events related to the inner ring node.
    /// All events will be distributed according to type.(2 event types:MainContractEvent and MorphContractEvent)
    /// Life process:Start--->Assignment event--->Stop
    /// </summary>
    public class InnerRingService : UntypedActor, IActiveState, IEpochState, IEpochTimerReseter, IIndexer
    {
        public class MainContractEvent { public NotifyEventArgs notify; };
        public class MorphContractEvent { public NotifyEventArgs notify; };
        public class Start { };
        public class Stop { };

        private IActorRef morphEventListener;
        private IActorRef mainEventListener;
        private IActorRef timer;

        private BalanceContractProcessor balanceContractProcessor;
        private ContainerContractProcessor containerContractProcessor;
        private FsContractProcessor fsContractProcessor;
        private NetMapContractProcessor netMapContractProcessor;
        private AlphabetContractProcessor alphabetContractProcessor;

        private Client mainNetClient;
        private Client morphClient;
        private readonly DB db;
        private readonly NEP6Wallet wallet;

        private Fixed8ConverterUtil convert;

        /// <summary>
        /// Constructor.
        /// 4 Tasks:
        /// 1)Build mainnet and morph clients
        /// 2)Build mainnet and morph contract event handlers
        /// 3)Build mainnet and morph event listeners
        /// 4)Initialization
        /// </summary>
        /// <param name="system">NeoSystem</param>
        public InnerRingService(NeoSystem system, NEP6Wallet pwallet = null, Client pMainNetClient = null, Client pMorphClient = null)
        {
            convert = new Fixed8ConverterUtil();
            db = DB.Open(GetFullPath(Settings.Default.Path), new Options { CreateIfMissing = true });
            //Create wallet
            if (pwallet is null)
            {
                wallet = new NEP6Wallet(Settings.Default.WalletPath);
                wallet.Unlock(Settings.Default.Password);
            }
            else
            {
                wallet = pwallet;
            }
            //Build 2 clients(MainNetClient&MorphClient).
            if (pMainNetClient is null)
            {
                mainNetClient = new MainClient(Settings.Default.Url, wallet);
            }
            else
            {
                mainNetClient = pMainNetClient;
            }
            if (pMorphClient is null)
            {
                morphClient = new MorphClient()
                {
                    Wallet = wallet,
                    Blockchain = system.Blockchain,
                };
            }
            else
            {
                morphClient = pMorphClient;
            }
            //Build processor of contract.
            //There are 5 processors.
            balanceContractProcessor = new BalanceContractProcessor()
            {
                Client = morphClient,
                Convert= convert,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.BalanceContractWorkersSize))
            };
            containerContractProcessor = new ContainerContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.ContainerContractWorkersSize))
            };
            fsContractProcessor = new FsContractProcessor()
            {
                Client = mainNetClient,
                Convert = convert,
                ActiveState = this,
                EpochState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.FsContractWorkersSize))
            };
            netMapContractProcessor = new NetMapContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                EpochState = this,
                EpochTimerReseter = this,
                NetmapSnapshot=new NetMapContractProcessor.CleanupTable(Settings.Default.CleanupEnabled,Settings.Default.CleanupThreshold),
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.NetmapContractWorkersSize))
            };
            alphabetContractProcessor = new AlphabetContractProcessor()
            {
                Client = morphClient,
                Indexer = this,
                StorageEmission = Settings.Default.StorageEmission,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props(Settings.Default.AlphabetContractWorkersSize))
            };
            balanceContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            containerContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            fsContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            netMapContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            alphabetContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            //Build listener
            morphEventListener = system.ActorSystem.ActorOf(Listener.Props());
            mainEventListener = system.ActorSystem.ActorOf(Listener.Props());
            morphEventListener.Tell(new BindProcessorEvent() { processor = netMapContractProcessor });
            morphEventListener.Tell(new BindProcessorEvent() { processor = containerContractProcessor });
            morphEventListener.Tell(new BindProcessorEvent() { processor = balanceContractProcessor });
            mainEventListener.Tell(new BindProcessorEvent() { processor = fsContractProcessor });
            //Build timer
            timer = system.ActorSystem.ActorOf(Timers.Props());
            timer.Tell(new BindTimersEvent() { processor = netMapContractProcessor });
            timer.Tell(new BindTimersEvent() { processor = containerContractProcessor });
            timer.Tell(new BindTimersEvent() { processor = balanceContractProcessor });
            timer.Tell(new BindTimersEvent() { processor = fsContractProcessor });
        }

        public void InitConfig()
        {
            long epoch = 0;
            try
            {
                epoch = ContractInvoker.GetEpoch(morphClient);
            }
            catch (Exception e)
            {
                throw new Exception("can't read epoch");
            }
            var key = wallet.GetAccounts().ToArray()[0].GetKey().PublicKey;
            int index = 0;
            try
            {
                index = ContractInvoker.InnerRingIndex(mainNetClient, key);
            }
            catch (Exception e)
            {
                throw new Exception("can't read inner ring list");
            }
            uint balancePrecision = 0;
            try
            {
                balancePrecision = ContractInvoker.BalancePrecision(morphClient);
            }
            catch (Exception e)
            {
                throw new Exception("can't read balance contract precision");
            }

            SetEpochCounter((ulong)epoch);
            SetIndexer(index);
            convert.SetBalancePrecision(balancePrecision);
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("active", IsActive().ToString());
            pairs.Add("epoch", epoch.ToString());
            pairs.Add("precision", balancePrecision.ToString());
            Utility.Log("read config from blockchain", LogLevel.Info, pairs.ToString());
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

        private void OnStart()
        {
            InitConfig();
            morphEventListener.Tell(new Listener.Start());
            mainEventListener.Tell(new Listener.Start());
            timer.Tell(new Timers.Start());
        }

        private void OnStop()
        {
            timer.Tell(new Timers.Stop());
            morphEventListener.Tell(new Listener.Stop());
            mainEventListener.Tell(new Listener.Stop());
            if (db != null) db.Dispose();
        }

        private void OnMainContractEvent(NotifyEventArgs notify)
        {
            mainEventListener.Tell(new NewContractEvent() { notify = notify });
        }

        private void OnMorphContractEvent(NotifyEventArgs notify)
        {
            morphEventListener.Tell(new NewContractEvent() { notify = notify });
        }

        public bool IsActive()
        {
            return Index() >= 0;
        }

        public void SetEpochCounter(ulong epoch)
        {
            WriteBatch writeBatch = new WriteBatch();
            writeBatch.Put(Encoding.UTF8.GetBytes("Epoch"), BitConverter.GetBytes(epoch));
            db.Write(WriteOptions.Default, writeBatch);
        }

        public int Index()
        {
            byte[] value = db.Get(ReadOptions.Default, Encoding.UTF8.GetBytes("Epoch"));
            if (value is null) return -1;
            return BitConverter.ToInt32(value);
        }

        public void SetIndexer(int index)
        {
            WriteBatch writeBatch = new WriteBatch();
            writeBatch.Put(Encoding.UTF8.GetBytes("Index"), BitConverter.GetBytes(index));
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
            timer.Tell(new Timer() {contractEvent=new NewEpochTickEvent() { } });
        }

        public static Props Props(NeoSystem system, NEP6Wallet pwallet = null, Client pMainNetClient = null, Client pMorphClient = null)
        {
            return Akka.Actor.Props.Create(() => new InnerRingService(system, pwallet, pMainNetClient, pMorphClient));
        }
    }
}
