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
    public class ContainerContractProcessor : IProcessor
    {
        private UInt160 ContainerContractHash = Settings.Default.ContainerContractHash;

        private string PutNotification = "containerPut";
        private string DeleteNotification = "containerDelete";

        private Client client;
        private IActiveState activeState;
        private IActorRef workPool;

        public Client Client { get => client; set => client = value; }
        public IActiveState ActiveState { get => activeState; set => activeState = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }

        HandlerInfo[] IProcessor.ListenerHandlers()
        {
            HandlerInfo putHandler = new HandlerInfo();
            putHandler.ScriptHashWithType = new ScriptHashWithType() { Type = PutNotification, ScriptHashValue = ContainerContractHash };
            putHandler.Handler = HandlePut;

            HandlerInfo deleteHandler = new HandlerInfo();
            putHandler.ScriptHashWithType = new ScriptHashWithType() { Type = DeleteNotification, ScriptHashValue = ContainerContractHash };
            putHandler.Handler = HandleDelete;

            return new HandlerInfo[] { putHandler, deleteHandler };

        }

        ParserInfo[] IProcessor.ListenerParsers()
        {
            //container put event
            ParserInfo putParser = new ParserInfo();
            putParser.ScriptHashWithType = new ScriptHashWithType() { Type = PutNotification, ScriptHashValue = ContainerContractHash };
            putParser.Parser = MorphEvent.ParseContainerPutEvent;

            //container delete event
            ParserInfo deleteParser = new ParserInfo();
            deleteParser.ScriptHashWithType = new ScriptHashWithType() { Type = DeleteNotification, ScriptHashValue = ContainerContractHash };
            deleteParser.Parser = MorphEvent.ParseContainerDeleteEvent;
            return new ParserInfo[] { putParser, deleteParser };
        }

        HandlerInfo[] IProcessor.TimersHandlers()
        {
            return null;
        }

        public void HandlePut(IContractEvent morphEvent)
        {
            ContainerPutEvent putEvent = (ContainerPutEvent)morphEvent;
            //send event to workpool
            workPool.Tell(new NewTask() { task = new Task(() => ProcessContainerPut(putEvent)) });
        }

        public void HandleDelete(IContractEvent morphEvent)
        {
            ContainerDeleteEvent deleteEvent = (ContainerDeleteEvent)morphEvent;
            //send event to workpool
            workPool.Tell(new NewTask() { task = new Task(() => ProcessContainerDelete(deleteEvent)) });
        }

        public void ProcessContainerPut(ContainerPutEvent putEvent)
        {
            if (!IsActive()) return;
            //invoke
            ContractInvoker.RegisterContainer(Client, new ContainerParams()
            {
                Key = putEvent.PublicKey,
                Container = putEvent.RawContainer,
                Signature = putEvent.Signature
            });
        }

        public void ProcessContainerDelete(ContainerDeleteEvent deleteEvent)
        {
            if (!IsActive()) return;
            //invoke
            ContractInvoker.RemoveContainer(Client, new RemoveContainerParams()
            {
                ContainerID = deleteEvent.ContainerID,
                Signature = deleteEvent.Signature
            });
        }

        public bool IsActive()
        {
            return activeState.IsActive();
        }
    }
}
