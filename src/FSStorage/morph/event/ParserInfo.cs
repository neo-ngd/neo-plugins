using System;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage
{
    public class ParserInfo
    {
        private ScriptHashWithType scriptHashWithType;
        private Func<VM.Types.Array, IContractEvent> parser;

        internal ScriptHashWithType ScriptHashWithType { get => scriptHashWithType; set => scriptHashWithType = value; }
        internal Func<VM.Types.Array, IContractEvent> Parser { get => parser; set => parser = value; }
    }
}
