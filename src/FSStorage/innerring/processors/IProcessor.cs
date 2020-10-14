using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public interface IProcessor
    {
        public ParserInfo[] ListenerParsers();
        public HandlerInfo[] ListenerHandlers();
        public HandlerInfo[] TimersHandlers();
    }
}
