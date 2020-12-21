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
using Neo.Wallets.NEP6;
using System.Collections.Generic;
using System.Linq;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;
using System.Threading;

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
        private readonly NEP6Wallet wallet;
        private long epochCounter;
        private int indexer = -1;

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
                mainNetClient = new MainClient(Settings.Default.Urls, wallet);
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
                Client = mainNetClient,
                Convert = convert,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props("BalanceContract Processor", Settings.Default.BalanceContractWorkersSize))
            };
            containerContractProcessor = new ContainerContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props("ContainerContract Processor", Settings.Default.ContainerContractWorkersSize))
            };
            fsContractProcessor = new FsContractProcessor()
            {
                Client = morphClient,
                Convert = convert,
                ActiveState = this,
                EpochState = this,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props("FsContract Processor", Settings.Default.FsContractWorkersSize))
            };
            netMapContractProcessor = new NetMapContractProcessor()
            {
                Client = morphClient,
                ActiveState = this,
                EpochState = this,
                EpochTimerReseter = this,
                NetmapSnapshot = new NetMapContractProcessor.CleanupTable(Settings.Default.CleanupEnabled, Settings.Default.CleanupThreshold),
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props("NetMapContract Processor", Settings.Default.NetmapContractWorkersSize))
            };
            alphabetContractProcessor = new AlphabetContractProcessor()
            {
                Client = morphClient,
                Indexer = this,
                StorageEmission = Settings.Default.StorageEmission,
                WorkPool = system.ActorSystem.ActorOf(WorkerPool.Props("AlphabetContract Processor", Settings.Default.AlphabetContractWorkersSize))
            };
            balanceContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            containerContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            fsContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            netMapContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            alphabetContractProcessor.WorkPool.Tell(new WorkerPool.Timer());
            //Build listener
            morphEventListener = system.ActorSystem.ActorOf(Listener.Props("MorphEventListener"));
            mainEventListener = system.ActorSystem.ActorOf(Listener.Props("MainEventListener"));
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
            long epoch;
            try
            {
                epoch = ContractInvoker.GetEpoch(morphClient);
            }
            catch
            {
                throw new Exception("can't read epoch");
            }
            var key = wallet.GetAccounts().ToArray()[0].GetKey().PublicKey;
            int index;
            try
            {
                index = ContractInvoker.InnerRingIndex(mainNetClient, key);
            }
            catch
            {
                throw new Exception("can't read inner ring list");
            }
            uint balancePrecision;
            try
            {
                balancePrecision = ContractInvoker.BalancePrecision(morphClient);
            }
            catch
            {
                throw new Exception("can't read balance contract precision");
            }

            SetEpochCounter((ulong)epoch);
            SetIndexer(index);
            convert.SetBalancePrecision(balancePrecision);
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("read config from blockchain", ":");
            pairs.Add("active", IsActive().ToString());
            pairs.Add("epoch", epoch.ToString());
            pairs.Add("precision", balancePrecision.ToString());
            Utility.Log("", LogLevel.Info, pairs.ParseToString());
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start _:
                    OnStart();
                    break;
                case Stop _:
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
            try {
                InitConfig();
                morphEventListener.Tell(new Listener.Start());
                mainEventListener.Tell(new Listener.Start());
                timer.Tell(new Timers.Start());
            } catch (Exception e){
                Utility.Log("", LogLevel.Error, e.Message);
                return;
            }
        }

        private void OnStop()
        {
            timer.Tell(new Timers.Stop());
            morphEventListener.Tell(new Listener.Stop());
            mainEventListener.Tell(new Listener.Stop());
        }

        private void OnMainContractEvent(NotifyEventArgs notify)
        {
            Console.WriteLine("接收到主网事件：" + notify.ScriptHash.ToString() + ";" + notify.EventName);
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
            long temp = Convert.ToInt64(epoch);
            Interlocked.Exchange(ref epochCounter, temp);
        }

        public int Index()
        {
            return indexer;
        }

        public void SetIndexer(int index)
        {
            Interlocked.Exchange(ref indexer, index);
        }

        public ulong EpochCounter()
        {
            return Convert.ToUInt64(epochCounter);
        }

        public void ResetEpochTimer()
        {
            timer.Tell(new Timers.Timer() { contractEvent = new NewEpochTickEvent() { } });
        }

        public static Props Props(NeoSystem system, NEP6Wallet pwallet = null, Client pMainNetClient = null, Client pMorphClient = null)
        {
            return Akka.Actor.Props.Create(() => new InnerRingService(system, pwallet, pMainNetClient, pMorphClient));
        }
    }
}
