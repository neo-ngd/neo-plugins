using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string balanceOfMethod = "balanceOf";
        private static string decimalsMethod = "decimals";

        public static UInt160 BalanceContractHash => Settings.Default.BalanceContractHash;
        public static string BalanceOfMethod { get => balanceOfMethod; set => balanceOfMethod = value; }
        public static string DecimalsMethod { get => decimalsMethod; set => decimalsMethod = value; }

        public static long InvokeBalanceOf(Client client, byte[] holder)
        {
            InvokeResult result = client.InvokeLocalFunction(BalanceContractHash, BalanceOfMethod, holder);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (BalanceOf)");
            return (long)result.ResultStack[0].GetInteger();
        }

        public static long InvokeDecimals(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(BalanceContractHash, DecimalsMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Decimals)");
            return (long)(result.ResultStack[0].GetInteger());
        }
    }

}
