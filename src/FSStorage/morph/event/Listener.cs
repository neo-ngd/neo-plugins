using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage
{
    public class Listener : UntypedActor
    {
        private Dictionary<ScriptHashWithType, Func<VM.Types.Array, IContractEvent>> parsers;
        private Dictionary<ScriptHashWithType, List<Action<IContractEvent>>> handlers;

        public class BindProcessorEvent { public IProcessor processor; };
        public class NewContractEvent { public NotifyEventArgs notify; };

        public Listener()
        {
            parsers = new Dictionary<ScriptHashWithType, Func<VM.Types.Array, IContractEvent>>();
            handlers = new Dictionary<ScriptHashWithType, List<Action<IContractEvent>>>();
        }

        public void ParseAndHandle(NotifyEventArgs notify)
        {
            if (notify.State is null) throw new Exception();
            var keyEvent = new ScriptHashWithType() { Type = notify.EventName, ScriptHashValue = notify.ScriptHash };
            if (!parsers.TryGetValue(keyEvent, out var parser)) return;
            IContractEvent contractEvent = parser(notify.State);
            if (!handlers.TryGetValue(keyEvent, out var handlersArray)) throw new Exception();
            if (handlersArray.Count == 0) throw new Exception();
            foreach (var handler in handlersArray)
            {
                handler(contractEvent);
            }
        }

        public void RegisterHandler(HandlerInfo p)
        {
            var handler = p.Handler;
            var parser = parsers[p.ScriptHashWithType];
            if (handlers.TryGetValue(p.ScriptHashWithType, out var value))
            {
                value.Add(p.Handler);
            }
            else
            {
                handlers.Add(p.ScriptHashWithType, new List<Action<IContractEvent>>() { p.Handler });
            }
        }

        public void SetParser(ParserInfo p)
        {
            if (p.Parser is null) throw new Exception();
            if (parsers.TryGetValue(p.ScriptHashWithType, out _))
                parsers[p.ScriptHashWithType] = p.Parser;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case NewContractEvent contractEvent:
                    ParseAndHandle(contractEvent.notify);
                    break;
                case BindProcessorEvent bindMorphProcessor:
                    BindProcessor(bindMorphProcessor.processor);
                    break;
            }
        }

        public void BindProcessor(IProcessor processor)
        {
            ParserInfo[] parsers = processor.ListenerParsers();
            HandlerInfo[] handlers = processor.ListenerHandlers();
            foreach (ParserInfo parser in parsers)
            {
                SetParser(parser);
            }
            foreach (HandlerInfo handler in handlers)
            {
                RegisterHandler(handler);
            }
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new Listener()).WithMailbox("MorphEventListener-mailbox");
        }
    }
}
