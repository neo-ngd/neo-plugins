using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.morph.invoke;
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

        HandlerInfo[] IProcessor.ListenerHandlers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType();
            scriptHashWithType.Type = LockNotification;
            scriptHashWithType.ScriptHashValue = BalanceContractHash;

            HandlerInfo handler = new HandlerInfo();
            handler.ScriptHashWithType = scriptHashWithType;
            handler.Handler = HandleLock;
            return new HandlerInfo[] { handler };

        }

        ParserInfo[] IProcessor.ListenerParsers()
        {
            ScriptHashWithType scriptHashWithType = new ScriptHashWithType();
            scriptHashWithType.Type = LockNotification;
            scriptHashWithType.ScriptHashValue = BalanceContractHash;

            ParserInfo parser = new ParserInfo();
            parser.ScriptHashWithType = scriptHashWithType;
            parser.Parser = ParseLockEvent;
            return new ParserInfo[] { parser };
        }

        HandlerInfo[] IProcessor.TimersHandlers()
        {
            return null;
        }

        public void HandleLock(IContractEvent morphEvent)
        {
            LockEvent lockEvent = (LockEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessLock(lockEvent)) });
        }

        public void ProcessLock(LockEvent lockEvent)
        {
            if (!IsActive()) return;
            //invoke
            ContractInvoker.CashOutCheque(Client, new ChequeParams()
            {
                Id = lockEvent.Id,
                Amount = lockEvent.Amount,
                UserAccount = lockEvent.UserAccount,
                LockAccount = lockEvent.LockAccount
            });
        }

        public bool IsActive()
        {
            return activeState.IsActive();
        }
    }
}
