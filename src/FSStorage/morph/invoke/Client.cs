using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public interface Client
    {
        public bool InvokeFunction(UInt160 contractHash, string method, long fee, params object[] args);
        public InvokeResult InvokeLocalFunction(UInt160 contractHash, string method, params object[] args);
    }
    public class InvokeResult
    {
        private VMState state;
        private long gasConsumed;
        private byte[] script;
        private StackItem[] resultStack;

        public VMState State { get => state; set => state = value; }
        public long GasConsumed { get => gasConsumed; set => gasConsumed = value; }
        public byte[] Script { get => script; set => script = value; }
        public StackItem[] ResultStack { get => resultStack; set => resultStack = value; }
    }
}
