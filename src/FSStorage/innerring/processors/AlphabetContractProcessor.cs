using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.innerring.timers;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.Plugins.innerring.processors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Neo.Plugins.FSStorage.innerring.invoke.ContractInvoker;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;
using static Neo.Plugins.FSStorage.MorphEvent;
using static Neo.Plugins.FSStorage.Utils;
using static Neo.Plugins.util.WorkerPool;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public class AlphabetContractProcessor : IProcessor
    {
        private UInt160[] AlphabetContractHash => Settings.Default.AlphabetContractHash;

        private Client client;
        private IActorRef workPool;
        private IIndexer indexer;
        private ulong storageEmission;

        public Client Client { get => client; set => client = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }
        public IIndexer Indexer { get => indexer; set => indexer = value; }
        public ulong StorageEmission { get => storageEmission; set => storageEmission = value; }

        public HandlerInfo[] ListenerHandlers()
        {
            return new HandlerInfo[] { };
        }

        public ParserInfo[] ListenerParsers()
        {
            return new ParserInfo[] { };
        }

        public HandlerInfo[] TimersHandlers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType()
            {
                Type = Timers.AlphabetTimer,
            };

            HandlerInfo handler = new HandlerInfo()
            {
                ScriptHashWithType = scriptHashWithType,
                Handler = HandleGasEmission
            };
            return new HandlerInfo[] { handler };
        }

        public void HandleGasEmission(IContractEvent morphEvent)
        {
            NewAlphabetEmitTickEvent newAlphabetEmitTickEvent = (NewAlphabetEmitTickEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "alphabet gas emit");
            Utility.Log("tick", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { task = new Task(() => ProcessEmit(newAlphabetEmitTickEvent)) });
        }

        public void ProcessEmit(NewAlphabetEmitTickEvent newAlphabetEmitTickEvent)
        {
            int index = Indexer.Index();
            if (index < 0) {
                Utility.Log("passive mode, ignore gas emission event", LogLevel.Info, null);
                return;
            } else if (index>=Settings.Default.AlphabetContractHash.Length) {
                Dictionary<string, string> pairs = new Dictionary<string, string>();
                pairs.Add("index", index.ToString());
                Utility.Log("node is out of alphabet range, ignore gas emission event", LogLevel.Debug, pairs.ToString());
            }
            try {
                ContractInvoker.AlphabetEmit(Client, index);
            } catch (Exception e) {
                Utility.Log("can't invoke alphabet emit method", LogLevel.Warning,null);
            }
            if (StorageEmission == 0) {
                Utility.Log("storage node emission is off", LogLevel.Info, null);
                return;
            }

        }
    }
}
