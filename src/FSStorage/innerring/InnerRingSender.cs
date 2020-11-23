using Akka.Actor;
using Neo.SmartContract;
using System.Linq;
using Neo.Network.RPC;
using Neo.IO;
using Neo.VM;

namespace Neo.Plugins.FSStorage.innerring
{
    public class InnerRingSender : UntypedActor
    {
        public class MainContractEvent { public NotifyEventArgs notify; };

        private RpcClient client;

        public InnerRingSender()
        {
            this.client = new RpcClient(Settings.Default.Url);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case MainContractEvent mainEvent:
                    OnMainContractEvent(mainEvent.notify);
                    break;
                default:
                    break;
            }
        }

        private void OnMainContractEvent(NotifyEventArgs notify)
        {
            var container = notify.ScriptContainer.ToArray().ToHexString();
            var scriptHash = notify.ScriptHash.ToArray().ToHexString();
            var eventName = notify.EventName;
            var state = notify.State.ToJson();
            var result = client.RpcSendAsync("receiveMainNetEvent", container, scriptHash, eventName, state).Result;
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new InnerRingSender());
        }
    }
}
