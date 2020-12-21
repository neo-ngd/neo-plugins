using System;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string BalanceOfMethod = "balanceOf";
        private static string DecimalsMethod = "decimals";

        private static UInt160 BalanceContractHash => Settings.Default.BalanceContractHash;

        public static long InvokeBalanceOf(Client client, byte[] holder)
        {
            InvokeResult result=client.InvokeLocalFunction(BalanceContractHash, BalanceOfMethod, holder);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})",BalanceOfMethod));
            return (long)result.ResultStack[0].GetInteger();
        }

        public static long InvokeDecimals(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(BalanceContractHash, DecimalsMethod);
            if (result.State != VM.VMState.HALT) throw new Exception(string.Format("could not perform test invocation ({0})", DecimalsMethod));
            return (long)(result.ResultStack[0].GetInteger());
        }
    }
}
