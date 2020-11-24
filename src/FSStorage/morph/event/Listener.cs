using Akka.Actor;
using Neo.Plugins.FSStorage.innerring.processors;
using Neo.Plugins.util;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage
{
    /// <summary>
    /// Listener is an interface of smart contract notification event listener.
    /// It is a listener for contract events. It will distribute event to the corresponding processor according to the type of event.
    /// The processor must be bound to the listener during initialization, otherwise it will not work.
    /// Currently, it mainly supports four types of processor:BalanceContractProcessor,ContainerContractProcessor,FsContractProcessor and NetMapContractProcessor
    /// </summary>
    public class Listener : UntypedActor
    {
        private Dictionary<ScriptHashWithType, Func<VM.Types.Array, IContractEvent>> parsers;
        private Dictionary<ScriptHashWithType, List<Action<IContractEvent>>> handlers;
        private bool started;

        public class BindProcessorEvent { public IProcessor processor; };
        public class NewContractEvent { public NotifyEventArgs notify; };
        public class Start { };
        public class Stop { };

        public Listener()
        {
            parsers = new Dictionary<ScriptHashWithType, Func<VM.Types.Array, IContractEvent>>();
            handlers = new Dictionary<ScriptHashWithType, List<Action<IContractEvent>>>();
        }

        public void ParseAndHandle(NotifyEventArgs notify)
        {
            if (started)
            {
                Utility.Log("script hash LE", LogLevel.Info, notify.ScriptHash.ToString());
                if (notify.State is null)
                {
                    Utility.Log("stack item is not an array type", LogLevel.Warning, null);
                }
                Utility.Log("event type", LogLevel.Info, notify.EventName);
                var keyEvent = new ScriptHashWithType() { Type = notify.EventName, ScriptHashValue = notify.ScriptHash };
                if (!parsers.TryGetValue(keyEvent, out var parser))
                {
                    Utility.Log("event parser not set", LogLevel.Warning, null);
                    return;
                }
                IContractEvent contractEvent = null;
                try
                {
                    contractEvent = parser(notify.State);
                }
                catch (Exception e)
                {
                    Utility.Log("could not parse notification event", LogLevel.Warning, e.Message);
                    return;
                }
                if (!handlers.TryGetValue(keyEvent, out var handlersArray) || handlersArray.Count == 0)
                {
                    Utility.Log("handlers for parsed notification event were not registered", LogLevel.Warning, contractEvent);
                    return;
                }
                foreach (var handler in handlersArray)
                {
                    handler(contractEvent);
                }
            }
        }

        public void RegisterHandler(HandlerInfo p)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("script hash LE", p.ScriptHashWithType.ScriptHashValue.ToString());
            pairs.Add("event type", p.ScriptHashWithType.Type);
            Utility.Log("", LogLevel.Info, pairs.ParseToString());

            var handler = p.Handler;
            if (handler is null)
            {
                Utility.Log("ignore nil event handler", LogLevel.Warning, null);
                return;
            }
            if (!parsers.TryGetValue(p.ScriptHashWithType, out _))
            {
                Utility.Log("ignore handler of event w/o parser", LogLevel.Warning, null);
                return;
            }
            if (handlers.TryGetValue(p.ScriptHashWithType, out var value))
            {
                value.Add(p.Handler);
            }
            else
            {
                handlers.Add(p.ScriptHashWithType, new List<Action<IContractEvent>>() { p.Handler });
            }
            Utility.Log("registered new event handler", LogLevel.Info, null);
        }

        public void SetParser(ParserInfo p)
        {
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            pairs.Add("script hash LE", p.ScriptHashWithType.ScriptHashValue.ToString());
            pairs.Add("event type", p.ScriptHashWithType.Type);
            Utility.Log("", LogLevel.Info, pairs.ParseToString());

            if (p.Parser is null)
            {
                Utility.Log("ignore nil event parser", LogLevel.Warning, null);
                return;
            }
            if (started)
            {
                Utility.Log("listener has been already started, ignore parser", LogLevel.Warning, null);
                return;
            }
            if (!parsers.TryGetValue(p.ScriptHashWithType, out _))
                parsers[p.ScriptHashWithType] = p.Parser;
            Utility.Log("registered new event parser", LogLevel.Info, null);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start start:
                    OnStart();
                    break;
                case Stop stop:
                    OnStop();
                    break;
                case NewContractEvent contractEvent:
                    ParseAndHandle(contractEvent.notify);
                    break;
                case BindProcessorEvent bindMorphProcessor:
                    BindProcessor(bindMorphProcessor.processor);
                    break;
                default:
                    break;
            }
        }

        public void OnStart()
        {
            if (!started)
            {
                started = !started;
            }
        }

        public void OnStop()
        {
            if (started)
            {
                started = !started;
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
            return Akka.Actor.Props.Create(() => new Listener());
        }
    }
}
