using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Plugins.FSStorage.innerring.timers;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using System;
using static Neo.Plugins.FSStorage.innerring.timers.Timers;
using static Neo.Plugins.FSStorage.Listener;
using Neo.Plugins.util;
using static System.IO.Path;
using System.Text;
using Neo.IO.Data.LevelDB;
using Neo.Wallets.NEP6;
using System.Collections.Generic;
using Neo.Plugins.innerring.processors;
using System.Linq;
using static Neo.Plugins.FSStorage.innerring.timers.EpochTickEvent;
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

        private void OnMainContractEvent(NotifyEventArgs notify) {
            var container = notify.ScriptContainer.ToArray().ToHexString();
            var scriptHash = notify.ScriptHash.ToArray().ToHexString();
            var eventName = notify.EventName;
            var state = notify.State.ToJson();
            var result=client.RpcSendAsync("receiveMainNetEvent", container,scriptHash,eventName,state).Result;
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new InnerRingSender());
        }
    }
}
