using System;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage
{
    public class HandlerInfo
    {
        private ScriptHashWithType scriptHashWithType;
        private Action<IContractEvent> handler;

        internal ScriptHashWithType ScriptHashWithType { get => scriptHashWithType; set => scriptHashWithType = value; }
        internal Action<IContractEvent> Handler { get => handler; set => handler = value; }
    }
}
