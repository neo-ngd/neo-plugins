using Akka.Actor;
using Neo.IO;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.Plugins.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Neo.Plugins.FSStorage.innerring.invoke.ContractInvoker;
using static Neo.Plugins.FSStorage.MorphEvent;
using static Neo.Plugins.FSStorage.Utils;
using static Neo.Plugins.util.WorkerPool;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public class FsContractProcessor : IProcessor
    {
        private UInt160 FsContractHash = Settings.Default.FsContractHash;
        private string DepositNotification = "Deposit";
        private string WithdrawNotification = "Withdraw";
        private string ChequeNotification = "Cheque";
        private string ConfigNotification = "SetConfig";
        private string UpdateIRNotification = "InnerRingUpdate";

        private string txLogPrefix = "mainnet:";
        private ulong lockAccountLifetime = 20;
        private int mintEmitCacheSize = Settings.Default.MintEmitCacheSize;
        private ulong mintEmitThreshold = Settings.Default.MintEmitThreshold;
        private long mintEmitValue = Settings.Default.MintEmitValue;
        private Dictionary<string, ulong> mintEmitCache;
        private Fixed8ConverterUtil convert;

        private Client client;
        private IActiveState activeState;
        private IEpochState epochState;
        private IActorRef workPool;

        private static readonly object lockObj = new object();

        public Client Client { get => client; set => client = value; }
        public string TxLogPrefix { get => txLogPrefix; set => txLogPrefix = value; }
        public ulong LockAccountLifetime { get => lockAccountLifetime; set => lockAccountLifetime = value; }
        public IActiveState ActiveState { get => activeState; set => activeState = value; }
        public IEpochState EpochState { get => epochState; set => epochState = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }
        public Fixed8ConverterUtil Convert { get => convert; set => convert = value; }

        public FsContractProcessor()
        {
            mintEmitCache = new Dictionary<string, ulong>(mintEmitCacheSize);
        }

        public HandlerInfo[] ListenerHandlers()
        {
            HandlerInfo depositHandler = new HandlerInfo();
            depositHandler.ScriptHashWithType = new ScriptHashWithType() { Type = DepositNotification, ScriptHashValue = FsContractHash };
            depositHandler.Handler = HandleDeposit;

            HandlerInfo withdrwaHandler = new HandlerInfo();
            withdrwaHandler.ScriptHashWithType = new ScriptHashWithType() { Type = WithdrawNotification, ScriptHashValue = FsContractHash };
            withdrwaHandler.Handler = HandleWithdraw;

            HandlerInfo chequeHandler = new HandlerInfo();
            chequeHandler.ScriptHashWithType = new ScriptHashWithType() { Type = ChequeNotification, ScriptHashValue = FsContractHash };
            chequeHandler.Handler = HandleCheque;

            HandlerInfo configHandler = new HandlerInfo();
            configHandler.ScriptHashWithType = new ScriptHashWithType() { Type = ConfigNotification, ScriptHashValue = FsContractHash };
            configHandler.Handler = HandleConfig;

            HandlerInfo updateIRHandler = new HandlerInfo();
            updateIRHandler.ScriptHashWithType = new ScriptHashWithType() { Type = UpdateIRNotification, ScriptHashValue = FsContractHash };
            updateIRHandler.Handler = HandleUpdateInnerRing;

            return new HandlerInfo[] { depositHandler, withdrwaHandler, chequeHandler, configHandler, updateIRHandler };
        }

        public ParserInfo[] ListenerParsers()
        {
            //deposit event
            ParserInfo depositParser = new ParserInfo();
            depositParser.ScriptHashWithType = new ScriptHashWithType() { Type = DepositNotification, ScriptHashValue = FsContractHash };
            depositParser.Parser = MorphEvent.ParseDepositEvent;

            //withdraw event
            ParserInfo withdrawParser = new ParserInfo();
            withdrawParser.ScriptHashWithType = new ScriptHashWithType() { Type = WithdrawNotification, ScriptHashValue = FsContractHash };
            withdrawParser.Parser = MorphEvent.ParseWithdrawEvent;

            //cheque event
            ParserInfo chequeParser = new ParserInfo();
            chequeParser.ScriptHashWithType = new ScriptHashWithType() { Type = ChequeNotification, ScriptHashValue = FsContractHash };
            chequeParser.Parser = MorphEvent.ParseChequeEvent;

            //config event
            ParserInfo configParser = new ParserInfo();
            configParser.ScriptHashWithType = new ScriptHashWithType() { Type = ConfigNotification, ScriptHashValue = FsContractHash };
            configParser.Parser = MorphEvent.ParseConfigEvent;

            //updateIR event
            ParserInfo updateIRParser = new ParserInfo();
            updateIRParser.ScriptHashWithType = new ScriptHashWithType() { Type = UpdateIRNotification, ScriptHashValue = FsContractHash };
            updateIRParser.Parser = MorphEvent.ParseUpdateInnerRingEvent;

            return new ParserInfo[] { depositParser, withdrawParser, chequeParser, configParser, updateIRParser };
        }

        public HandlerInfo[] TimersHandlers()
        {
            return new HandlerInfo[] { };
        }

        public void HandleDeposit(IContractEvent morphEvent)
        {
            DepositEvent depositeEvent = (DepositEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "deposit");
            pairs.Add("value", depositeEvent.Id.ToHexString());
            Utility.Log("notification", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { process = "fs", task = new Task(() => ProcessDeposit(depositeEvent)) });
        }

        public void HandleWithdraw(IContractEvent morphEvent)
        {
            WithdrawEvent withdrawEvent = (WithdrawEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "withdraw");
            pairs.Add("value", withdrawEvent.Id.ToHexString());
            Utility.Log("notification", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { process = "fs", task = new Task(() => ProcessWithdraw(withdrawEvent)) });
        }

        public void HandleCheque(IContractEvent morphEvent)
        {
            ChequeEvent chequeEvent = (ChequeEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "cheque");
            pairs.Add("value", chequeEvent.Id.ToHexString());
            Utility.Log("notification", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { process = "fs", task = new Task(() => ProcessCheque(chequeEvent)) });
        }

        public void HandleConfig(IContractEvent morphEvent)
        {
            ConfigEvent configEvent = (ConfigEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "setConfig");
            pairs.Add("key", configEvent.Key.ToHexString());
            pairs.Add("value", configEvent.Value.ToHexString());
            Utility.Log("notification", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { process = "fs", task = new Task(() => ProcessConfig(configEvent)) });
        }

        public void HandleUpdateInnerRing(IContractEvent morphEvent)
        {
            UpdateInnerRingEvent updateInnerRingEvent = (UpdateInnerRingEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "update inner ring");
            Utility.Log("notification", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { process = "fs", task = new Task(() => ProcessUpdateInnerRing(updateInnerRingEvent)) });
        }

        public void ProcessDeposit(DepositEvent depositeEvent)
        {
            if (!IsActive())
            {
                Utility.Log("passive mode, ignore deposit", LogLevel.Info, null);
                return;
            }
            //invoke
            try
            {
                List<byte> coment = new List<byte>();
                coment.AddRange(System.Text.Encoding.UTF8.GetBytes(TxLogPrefix));
                coment.AddRange(depositeEvent.Id);
                ContractInvoker.Mint(client, new MintBurnParams()
                {
                    ScriptHash = depositeEvent.To.ToArray(),
                    Amount = convert.ToBalancePrecision(depositeEvent.Amount),
                    Comment = coment.ToArray()
                });
            }
            catch (Exception e)
            {
                Utility.Log("can't transfer assets to balance contract", LogLevel.Error, e.Message);
            }

            var curEpoch = epochState.EpochCounter();
            var receiver = depositeEvent.To;
            lock (lockObj)
            {
                var ok = mintEmitCache.TryGetValue(receiver.ToString(), out ulong value);
                if (ok && ((value + mintEmitThreshold) >= curEpoch))
                {
                    Dictionary<string, string> pairs = new Dictionary<string, string>();
                    pairs.Add("receiver", receiver.ToString());
                    pairs.Add("last_emission", value.ToString());
                    pairs.Add("current_epoch", curEpoch.ToString());
                    Utility.Log("double mint emission declined", LogLevel.Warning, pairs.ToString());
                }
                //transferGas
                try
                {
                    ((MorphClient)client).TransferGas(depositeEvent.To, mintEmitValue);
                }
                catch (Exception e)
                {
                    Utility.Log("can't transfer native gas to receiver", LogLevel.Error, e.Message);
                }
                mintEmitCache.Add(receiver.ToString(), curEpoch);
            }
        }

        public void ProcessWithdraw(WithdrawEvent withdrawEvent)
        {
            if (!IsActive())
            {
                Utility.Log("passive mode, ignore withdraw", LogLevel.Info, null);
                return;
            }
            if (withdrawEvent.Id.Length < UInt160.Length)
            {
                Utility.Log("tx id size is less than script hash size", LogLevel.Error, null);
                return;
            }
            UInt160 lockeAccount = null;
            try
            {
                lockeAccount = new UInt160(withdrawEvent.Id.Take(UInt160.Length).ToArray());
            }
            catch (Exception e)
            {
                Utility.Log("can't create lock account", LogLevel.Error, e.Message);
            }
            try
            {
                ulong curEpoch = EpochCounter();
                //invoke
                ContractInvoker.LockAsset(client, new LockParams()
                {
                    ID = withdrawEvent.Id,
                    UserAccount = withdrawEvent.UserAccount,
                    LockAccount = lockeAccount,
                    Amount = convert.ToBalancePrecision(withdrawEvent.Amount),
                    Until = curEpoch + LockAccountLifetime
                });
            }
            catch (Exception e)
            {
                Utility.Log("can't lock assets for withdraw", LogLevel.Error, e.Message);
            }
        }

        public void ProcessCheque(ChequeEvent chequeEvent)
        {
            if (!IsActive())
            {
                Utility.Log("passive mode, ignore cheque", LogLevel.Info, null);
                return;
            }
            //invoke
            try
            {
                List<byte> coment = new List<byte>();
                coment.AddRange(System.Text.Encoding.UTF8.GetBytes(TxLogPrefix));
                coment.AddRange(chequeEvent.Id);

                ContractInvoker.Burn(Client, new MintBurnParams()
                {
                    ScriptHash = chequeEvent.LockAccount.ToArray(),
                    Amount = convert.ToBalancePrecision(chequeEvent.Amount),
                    Comment = coment.ToArray()
                });
            }
            catch (Exception e)
            {
                Utility.Log("can't transfer assets to fed contract", LogLevel.Error, e.Message);
            }
        }

        public void ProcessConfig(ConfigEvent configEvent)
        {
            if (!IsActive())
            {
                Utility.Log("passive mode, ignore deposit", LogLevel.Info, null);
                return;
            }
            //invoke
            try
            {
                ContractInvoker.SetConfig(Client, new SetConfigArgs()
                {
                    Id = configEvent.Id,
                    Key = configEvent.Key,
                    Value = configEvent.Value
                });
            }
            catch (Exception e)
            {
                Utility.Log("can't relay set config event", LogLevel.Error, e.Message);
            }
        }

        public void ProcessUpdateInnerRing(UpdateInnerRingEvent updateInnerRingEvent)
        {
            if (!IsActive())
            {
                Utility.Log("passive mode, ignore deposit", LogLevel.Info, null);
                return;
            }
            //invoke
            try
            {
                ContractInvoker.UpdateInnerRing(Client, updateInnerRingEvent.Keys);
            }
            catch (Exception e)
            {
                Utility.Log("can't relay update inner ring event", LogLevel.Error, e.Message);
            }
        }

        public ulong EpochCounter()
        {
            return epochState.EpochCounter();
        }

        public bool IsActive()
        {
            return activeState.IsActive();
        }
    }
}
