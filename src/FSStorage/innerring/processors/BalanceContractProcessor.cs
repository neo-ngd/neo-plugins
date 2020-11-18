using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.morph.invoke;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Neo.Plugins.FSStorage.innerring.invoke.ContractInvoker;
using static Neo.Plugins.FSStorage.MorphEvent;
using static Neo.Plugins.FSStorage.Utils;
using static Neo.Plugins.util.WorkerPool;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public class BalanceContractProcessor : IProcessor
    {
        private UInt160 BalanceContractHash => Settings.Default.BalanceContractHash;

        private string LockNotification = "Lock";

        private Client client;
        private IActiveState activeState;
        private IActorRef workPool;

        public Client Client { get => client; set => client = value; }
        public IActiveState ActiveState { get => activeState; set => activeState = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }

        public HandlerInfo[] ListenerHandlers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType()
            {
                Type = LockNotification,
                ScriptHashValue = BalanceContractHash
            };

            HandlerInfo handler = new HandlerInfo()
            {
                ScriptHashWithType = scriptHashWithType,
                Handler = HandleLock
            };
            return new HandlerInfo[] { handler };
        }

        public ParserInfo[] ListenerParsers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType()
            {
                Type = LockNotification,
                ScriptHashValue = BalanceContractHash
            };
            ParserInfo parser = new ParserInfo()
            {
                ScriptHashWithType = scriptHashWithType,
                Parser = ParseLockEvent,
            };
            return new ParserInfo[] { parser };
        }

        public HandlerInfo[] TimersHandlers()
        {
            return new HandlerInfo[] { };
        }

        public void HandleLock(IContractEvent morphEvent)
        {
            LockEvent lockEvent = (LockEvent)morphEvent;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("type", "lock");
            pairs.Add("value", lockEvent.Id.ToHexString());
            Utility.Log("notification", LogLevel.Info, pairs.ToString());
            workPool.Tell(new NewTask() { task = new Task(() => ProcessLock(lockEvent)) });
        }

        public void ProcessLock(LockEvent lockEvent)
        {
            if (!IsActive())
            {
                Utility.Log("passive mode, ignore balance lock", LogLevel.Info, null);
                return;
            }
            //invoke
            try
            {
                //to do
                //maybe need to convert precision
                ContractInvoker.CashOutCheque(Client, new ChequeParams()
                {
                    Id = lockEvent.Id,
                    Amount = lockEvent.Amount,
                    UserAccount = lockEvent.UserAccount,
                    LockAccount = lockEvent.LockAccount
                });
            }
            catch (Exception e)
            {
                Utility.Log("can't send lock asset tx", LogLevel.Error, e.Message);
            }
        }

        public bool IsActive()
        {
            return activeState.IsActive();
        }
    }
}
