using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private UInt160 balanceContractHash = UInt160.Zero;
        private string balanceOfMethod = "balanceOf";
        private string decimalsMethod = "decimals";

        public UInt160 BalanceContractHash { get => balanceContractHash; set => balanceContractHash = value; }
        public string BalanceOfMethod { get => balanceOfMethod; set => balanceOfMethod = value; }
        public string DecimalsMethod { get => decimalsMethod; set => decimalsMethod = value; }

        public long InvokeBalanceOf(Client client, byte[] holder)
        {
            InvokeResult result = client.InvokeLocalFunction(BalanceContractHash, BalanceOfMethod, holder);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (BalanceOf)");
            return (long)result.ResultStack[0].GetInteger();
        }

        public long InvokeDecimals(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(BalanceContractHash, DecimalsMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Decimals)");
            return (long)result.ResultStack[0].GetInteger();
        }
    }

}
