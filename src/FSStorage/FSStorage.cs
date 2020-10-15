using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.FSStorage.innerring;
using Neo.SmartContract;
using Neo.VM;
using System.Collections.Generic;
using static Neo.Plugins.FSStorage.innerring.InneringService;

namespace Neo.Plugins.FSStorage
{
    public class FSStorage : Plugin, IPersistencePlugin
    {
        public IActorRef inneringService;

        public override string Description => "Fs StorageNode Plugin";

        public FSStorage()
        {
            inneringService = System.ActorSystem.ActorOf(InneringService.Props(Plugin.System));
            RpcServerPlugin.RegisterMethods(this);
            inneringService.Tell(new Start() { });
        }

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        public void OnPersist(StoreView snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            foreach (var appExec in applicationExecutedList)
            {
                Transaction tx = appExec.Transaction;
                VMState state = appExec.VMState;
                if (tx is null || state != VMState.HALT) continue;
                var notifys = appExec.Notifications;
                if (notifys is null) continue;
                foreach (var notify in notifys)
                {
                    inneringService.Tell(new MorphContractEvent() { notify = notify });
                }
            }
        }

        [RpcMethod]
        public void ReceiveMainNetEvent()
        {
            var notify = new NotifyEventArgs(null, null, null, null);
            inneringService.Tell(new MainContractEvent() { notify = notify });
        }
    }
}
