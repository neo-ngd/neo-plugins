using System;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage
{
    public class HandlerInfo
    {
        private ScriptHashWithType scriptHashWithType;
        private Action<IContractEvent> handler;

        public ScriptHashWithType ScriptHashWithType { get => scriptHashWithType; set => scriptHashWithType = value; }
        public Action<IContractEvent> Handler { get => handler; set => handler = value; }
    }
}
