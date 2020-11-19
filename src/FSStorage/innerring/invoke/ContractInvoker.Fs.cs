using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160 FsContractHash = Settings.Default.FsContractHash;
        private const string CheckIsInnerRingMethod = "isInnerRing";
        private const string ChequeMethod = "cheque";
        private const string InnerRingListMethod = "innerRingList";

        private const long FeeHalfGas = 50_000_000;
        private const long FeeOneGas = FeeHalfGas * 2;

        public class ChequeParams
        {
            private byte[] id;
            private long amount;
            private UInt160 userAccount;
            private UInt160 lockAccount;

            public byte[] Id { get => id; set => id = value; }
            public long Amount { get => amount; set => amount = value; }
            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }
            public UInt160 LockAccount { get => lockAccount; set => lockAccount = value; }
        }

        public static bool IsInnerRing(Client client, ECPoint p)
        {
            InvokeResult result = client.InvokeLocalFunction(FsContractHash, CheckIsInnerRingMethod, p.EncodePoint(true));
            return result.ResultStack[0].GetBoolean();
        }

        public static bool CashOutCheque(Client client, ChequeParams p)
        {
            return client.InvokeFunction(FsContractHash, ChequeMethod, ExtraFee, p.Id, p.UserAccount, p.Amount, p.LockAccount);
        }

        public static int InnerRingIndex(Client client, ECPoint p)
        {
            InvokeResult result = client.InvokeLocalFunction(FsContractHash, InnerRingListMethod);
            var irNodes = (Array)result.ResultStack[0];
            IEnumerator<StackItem> enumerator = irNodes.GetEnumerator();
            var index = -1;
            var i = -1;
            while (enumerator.MoveNext())
            {
                i++;
                var key = (Array)enumerator.Current;
                var keyValue = key[0].GetSpan().ToArray();
                if (p.ToArray().ToHexString().Equals(keyValue.ToHexString()))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }
}
