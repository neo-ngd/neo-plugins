using Akka.Actor;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.FSStorage.innerring;
using Neo.SmartContract;
using Neo.VM;
using System.Collections.Generic;
using Neo.Network.RPC;
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
        public void ReceiveMainNetEvent(JArray _params)
        {
            IVerifiable container= _params[0].AsString().HexToBytes().AsSerializable<Transaction>();
            UInt160 contractHash = UInt160.Parse(_params[0].AsString());
            string eventName= _params[2].AsString();
            IEnumerator<JObject> array=((JArray)_params[3]).GetEnumerator();
            VM.Types.Array state = new VM.Types.Array();
            while (array.MoveNext()) {
                state.Add(Neo.Network.RPC.Utility.StackItemFromJson(array.Current));
            }
            var notify = new NotifyEventArgs(container, contractHash, eventName, state);
            inneringService.Tell(new MainContractEvent() { notify = notify });
        }

        public override void Dispose()
        {
            base.Dispose();
            inneringService.Tell(new Stop() {});
        }
    }
}
