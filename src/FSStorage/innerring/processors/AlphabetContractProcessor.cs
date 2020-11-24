using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.innerring.timers;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.Plugins.util;
using Neo.SmartContract;
using Neo.Wallets;
using NeoFS.API.v2.Netmap;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;
using static Neo.Plugins.FSStorage.Utils;
using static Neo.Plugins.util.WorkerPool;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public class AlphabetContractProcessor : IProcessor
    {
        public Client Client;
        public IActorRef WorkPool;
        public IIndexer Indexer;
        public ulong StorageEmission;

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
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "alphabet gas emit");
            Utility.Log("tick", LogLevel.Info, pairs.ParseToString());
            WorkPool.Tell(new NewTask() { process = "alphabet", task = new Task(() => ProcessEmit()) });
        }

        public void ProcessEmit()
        {
            int index = Indexer.Index();
            if (index < 0)
            {
                Utility.Log("passive mode, ignore gas emission event", LogLevel.Info, null);
                return;
            }
            else if (index >= Settings.Default.AlphabetContractHash.Length)
            {
                Dictionary<string, string> pairs = new Dictionary<string, string>();
                pairs.Add("index", index.ToString());
                Utility.Log("node is out of alphabet range, ignore gas emission event", LogLevel.Debug, pairs.ParseToString());
            }
            try
            {
                ContractInvoker.AlphabetEmit(Client, index);
            }
            catch (Exception)
            {
                Utility.Log("can't invoke alphabet emit method", LogLevel.Warning, null);
                return;
            }
            if (StorageEmission == 0)
            {
                Utility.Log("storage node emission is off", LogLevel.Info, null);
                return;
            }
            NodeInfo[] networkMap = null;
            try
            {
                networkMap = ContractInvoker.NetmapSnapshot(Client);
            }
            catch (Exception e)
            {
                Dictionary<string, string> pairs = new Dictionary<string, string>();
                pairs.Add("error", e.Message);
                Utility.Log("can't get netmap snapshot to emit gas to storage nodes", LogLevel.Warning, pairs.ParseToString());
                return;
            }
            if (networkMap.Length == 0)
            {
                Utility.Log("empty network map, do not emit gas", LogLevel.Debug, null);
                return;
            }
            var gasPerNode = (long)StorageEmission * 100000000 / networkMap.Length;
            for (int i = 0; i < networkMap.Length; i++)
            {
                ECPoint key = null;
                try
                {
                    key = ECPoint.FromBytes(networkMap[i].PublicKey.ToByteArray(), ECCurve.Secp256r1);
                }
                catch (Exception e)
                {
                    Dictionary<string, string> pairs = new Dictionary<string, string>();
                    pairs.Add("error", e.Message);
                    Utility.Log("can't convert node public key to address", LogLevel.Warning, pairs.ParseToString());
                    continue;
                }
                try
                {
                    ((MorphClient)Client).TransferGas(key.EncodePoint(true).ToScriptHash(), gasPerNode);
                }
                catch (Exception e)
                {
                    Dictionary<string, string> pairs = new Dictionary<string, string>();
                    pairs.Add("receiver", e.Message);
                    pairs.Add("amount", key.EncodePoint(true).ToScriptHash().ToAddress());
                    Utility.Log("can't transfer gas", LogLevel.Warning, pairs.ParseToString());
                }
            }
        }
    }
}
