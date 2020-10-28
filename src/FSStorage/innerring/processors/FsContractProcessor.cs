using Akka.Actor;
using Neo.IO;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.morph.invoke;
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

        private Client client;
        private IActiveState activeState;
        private IEpochState epochState;
        private IActorRef workPool;

        public Client Client { get => client; set => client = value; }
        public string TxLogPrefix { get => txLogPrefix; set => txLogPrefix = value; }
        public ulong LockAccountLifetime { get => lockAccountLifetime; set => lockAccountLifetime = value; }
        public IActiveState ActiveState { get => activeState; set => activeState = value; }
        public IEpochState EpochState { get => epochState; set => epochState = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }

        HandlerInfo[] IProcessor.ListenerHandlers()
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

        ParserInfo[] IProcessor.ListenerParsers()
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

        HandlerInfo[] IProcessor.TimersHandlers()
        {
            return null;
        }

        public void HandleDeposit(IContractEvent morphEvent)
        {
            DepositEvent depositeEvent = (DepositEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessDeposit(depositeEvent)) });
        }

        public void HandleWithdraw(IContractEvent morphEvent)
        {
            WithdrawEvent withdrawEvent = (WithdrawEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessWithdraw(withdrawEvent)) });
        }

        public void HandleCheque(IContractEvent morphEvent)
        {
            ChequeEvent chequeEvent = (ChequeEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessCheque(chequeEvent)) });
        }

        public void HandleConfig(IContractEvent morphEvent)
        {
            ConfigEvent configEvent = (ConfigEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessConfig(configEvent)) });
        }

        public void HandleUpdateInnerRing(IContractEvent morphEvent)
        {
            UpdateInnerRingEvent updateInnerRingEvent = (UpdateInnerRingEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessUpdateInnerRing(updateInnerRingEvent)) });
        }

        public void ProcessDeposit(DepositEvent depositeEvent)
        {
            if (!IsActive()) return;
            //invoke
            List<byte> coment = new List<byte>();
            coment.AddRange(System.Text.Encoding.UTF8.GetBytes(TxLogPrefix));
            coment.AddRange(depositeEvent.Id);

            ContractInvoker.Mint(client, new MintBurnParams()
            {
                ScriptHash = depositeEvent.To.ToArray(),
                Amount = depositeEvent.Amount * 1_0000_0000,
                Comment = coment.ToArray()
            });
            //transferGas
            ((MorphClient)client).TransferGas(depositeEvent.To, 2);
        }

        public void ProcessWithdraw(WithdrawEvent withdrawEvent)
        {
            if (!IsActive()) return;
            if (withdrawEvent.Id.Length < UInt160.Length) return;

            UInt160 lockeAccount = new UInt160(withdrawEvent.Id.Take(UInt160.Length).ToArray());
            ulong curEpoch = EpochCounter();
            //invoke
            ContractInvoker.LockAsset(client, new LockParams()
            {
                ID = withdrawEvent.Id,
                UserAccount = withdrawEvent.UserAccount,
                LockAccount = lockeAccount,
                Amount = withdrawEvent.Amount * 1_0000_0000,
                Until = curEpoch + LockAccountLifetime
            });
        }

        public void ProcessCheque(ChequeEvent chequeEvent)
        {
            if (!IsActive()) return;
            //invoke
            List<byte> coment = new List<byte>();
            coment.AddRange(System.Text.Encoding.UTF8.GetBytes(TxLogPrefix));
            coment.AddRange(chequeEvent.Id);

            ContractInvoker.Burn(Client, new MintBurnParams()
            {
                ScriptHash = chequeEvent.LockAccount.ToArray(),
                Amount = chequeEvent.Amount * 1_0000_0000,
                Comment = coment.ToArray()
            });
        }

        public void ProcessConfig(ConfigEvent configEvent)
        {
            if (!IsActive()) return;
            //invoke
            ContractInvoker.SetConfig(Client, new SetConfigArgs()
            {
                Key = configEvent.Key,
                Value = configEvent.Value
            });
        }

        public void ProcessUpdateInnerRing(UpdateInnerRingEvent updateInnerRingEvent)
        {
            if (!IsActive()) return;
            //invoke
            ContractInvoker.UpdateInnerRing(Client, updateInnerRingEvent.Keys);
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
