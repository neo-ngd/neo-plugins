using System;
using static Neo.Plugins.FSStorage.Utils;

namespace Neo.Plugins.FSStorage
{
    public class ParserInfo
    {
        private ScriptHashWithType scriptHashWithType;
        private Func<VM.Types.Array, IContractEvent> parser;

        public ScriptHashWithType ScriptHashWithType { get => scriptHashWithType; set => scriptHashWithType = value; }
        public Func<VM.Types.Array, IContractEvent> Parser { get => parser; set => parser = value; }
    }
}
