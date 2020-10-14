using Neo.Cryptography.ECC;
using Neo.Plugins.FSStorage.morph.invoke;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160 FsContractHash = Settings.Default.FsContractHash;
        private static string CheckIsInnerRingMethod = "isInnerRing";
        private static string ChequeMethod = "cheque";

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
            client.InvokeLocalFunction(FsContractHash, CheckIsInnerRingMethod, p.EncodePoint(true));
            return true;
        }

        public static void CashOutCheque(Client client, ChequeParams p)
        {
            client.InvokeLocalFunction(FsContractHash, CheckIsInnerRingMethod, p.Id, p.UserAccount, p.Amount, p.LockAccount);
        }
    }
}
