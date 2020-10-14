using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.innerring.timers;
using Neo.Plugins.FSStorage.morph.invoke;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;
using static Neo.Plugins.FSStorage.MorphEvent;
using static Neo.Plugins.FSStorage.Utils;
using static Neo.Plugins.util.WorkerPool;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public class NetMapContractProcessor : IProcessor
    {
        private string newEpochNotification = "NewEpoch";
        private string addPeerNotification = "AddPeer";
        private string updatePeerStateNotification = "UpdateState";

        private UInt160 netmapContractHash;

        private ContractInvoker invoker;
        private Client client;

        private IActiveState activeState;
        private IEpochState epochState;
        private IEpochTimerReseter epochTimerReseter;
        private IActorRef workPool;

        public string NewEpochNotification { get => newEpochNotification; set => newEpochNotification = value; }
        public string AddPeerNotification { get => addPeerNotification; set => addPeerNotification = value; }
        public string UpdatePeerStateNotification { get => updatePeerStateNotification; set => updatePeerStateNotification = value; }
        public UInt160 NetmapContractHash { get => netmapContractHash; set => netmapContractHash = value; }
        public ContractInvoker Invoker { get => invoker; set => invoker = value; }
        public Client Client { get => client; set => client = value; }
        public IActiveState ActiveState { get => activeState; set => activeState = value; }
        public IEpochState EpochState { get => epochState; set => epochState = value; }
        public IEpochTimerReseter EpochTimerReseter { get => epochTimerReseter; set => epochTimerReseter = value; }
        public IActorRef WorkPool { get => workPool; set => workPool = value; }

        public NetMapContractProcessor()
        {

        }

        public bool IsActive()
        {
            return activeState.IsActive();
        }

        HandlerInfo[] IProcessor.ListenerHandlers()
        {
            HandlerInfo newEpochHandler = new HandlerInfo();
            newEpochHandler.ScriptHashWithType = new ScriptHashWithType() { Type = NewEpochNotification, ScriptHashValue = NetmapContractHash };
            newEpochHandler.Handler = HandleNewEpoch;

            HandlerInfo addPeerHandler = new HandlerInfo();
            addPeerHandler.ScriptHashWithType = new ScriptHashWithType() { Type = AddPeerNotification, ScriptHashValue = NetmapContractHash };
            addPeerHandler.Handler = HandleAddPeer;

            HandlerInfo updatePeerStateHandler = new HandlerInfo();
            updatePeerStateHandler.ScriptHashWithType = new ScriptHashWithType() { Type = UpdatePeerStateNotification, ScriptHashValue = NetmapContractHash };
            updatePeerStateHandler.Handler = HandleUpdateState;

            return new HandlerInfo[] { newEpochHandler, addPeerHandler, updatePeerStateHandler };
        }

        ParserInfo[] IProcessor.ListenerParsers()
        {
            ParserInfo newEpochParser = new ParserInfo();
            newEpochParser.ScriptHashWithType = new ScriptHashWithType() { Type = NewEpochNotification, ScriptHashValue = NetmapContractHash };
            newEpochParser.Parser = MorphEvent.ParseNewEpochEvent;

            ParserInfo addPeerParser = new ParserInfo();
            addPeerParser.ScriptHashWithType = new ScriptHashWithType() { Type = AddPeerNotification, ScriptHashValue = NetmapContractHash };
            addPeerParser.Parser = MorphEvent.ParseAddPeerEvent;

            ParserInfo updatePeerParser = new ParserInfo();
            updatePeerParser.ScriptHashWithType = new ScriptHashWithType() { Type = UpdatePeerStateNotification, ScriptHashValue = NetmapContractHash };
            updatePeerParser.Parser = MorphEvent.ParseUpdatePeerEvent;

            return new ParserInfo[] { newEpochParser, addPeerParser, updatePeerParser };
        }

        HandlerInfo[] IProcessor.TimersHandlers()
        {
            HandlerInfo newEpochHandler = new HandlerInfo();
            newEpochHandler.ScriptHashWithType = new ScriptHashWithType() { Type = Timers.EpochTimer };
            newEpochHandler.Handler = HandleNewEpochTick;
            return new HandlerInfo[] { newEpochHandler };
        }

        public void HandleNewEpochTick(IContractEvent timersEvent)
        {
            NewEpochTickEvent newEpochTickEvent = (NewEpochTickEvent)timersEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessNewEpochTick(newEpochTickEvent)) });
        }

        public void HandleNewEpoch(IContractEvent morphEvent)
        {
            NewEpochEvent newEpochEvent = (NewEpochEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessNewEpoch(newEpochEvent)) });
        }

        public void HandleAddPeer(IContractEvent morphEvent)
        {
            AddPeerEvent addPeerEvent = (AddPeerEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessAddPeer(addPeerEvent)) });
        }

        public void HandleUpdateState(IContractEvent morphEvent)
        {
            UpdatePeerEvent updateStateEvent = (UpdatePeerEvent)morphEvent;
            workPool.Tell(new NewTask() { task = new Task(() => ProcessUpdateState(updateStateEvent)) });
        }

        public void ProcessNewEpochTick(NewEpochTickEvent timersEvent)
        {
            if (!IsActive()) return;
            ulong nextEpoch = EpochCounter() + 1;
            ContractInvoker.SetNewEpoch(client, nextEpoch);
        }

        public void ProcessNewEpoch(NewEpochEvent newEpochEvent)
        {
            if (!IsActive()) return;
            ulong nextEpoch = EpochCounter() + 1;
            SetEpochCounter(nextEpoch);
            ResetEpochTimer();
        }

        public void ProcessAddPeer(AddPeerEvent addPeerEvent)
        {
            if (!IsActive()) return;
            // todo

        }

        public void ProcessUpdateState(UpdatePeerEvent updateStateEvent)
        {
            if (!IsActive()) return;
            // todo
        }

        public ulong EpochCounter()
        {
            return epochState.EpochCounter();
        }

        public void SetEpochCounter(ulong epoch)
        {
            epochState.SetEpochCounter(epoch);
        }

        public void ResetEpochTimer()
        {
            epochTimerReseter.ResetEpochTimer();
        }
    }
}
