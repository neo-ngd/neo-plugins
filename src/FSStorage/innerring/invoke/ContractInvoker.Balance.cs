using Neo.Plugins.FSStorage.morph.invoke;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160 BalanceContractHash => Settings.Default.BalanceContractHash;
        private static string TransferXMethod = "transferX";
        private static string LockMethod = "lock";
        private static string MintMethod = "mint";
        private static string BurnMethod = "burn";

        private static long ExtraFee = 1_5000_0000;

        public class TransferXParams
        {
            private byte[] sender;
            private byte[] receiver;
            private long amount;
            private byte[] comment;

            public byte[] Sender { get => sender; set => sender = value; }
            public byte[] Receiver { get => receiver; set => receiver = value; }
            public long Amount { get => amount; set => amount = value; }
            public byte[] Comment { get => comment; set => comment = value; }
        }

        public class LockParams
        {
            private byte[] id;
            private UInt160 userAccount;
            private UInt160 lockAccount;
            private long amount;
            private ulong until;

            public byte[] ID { get => ID; set => ID = value; }
            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }
            public UInt160 LockAccount { get => lockAccount; set => lockAccount = value; }
            public long Amount { get => amount; set => amount = value; }
            public ulong Until { get => until; set => until = value; }
        }

        public class MintBurnParams
        {
            private byte[] scriptHash;
            private long amount;
            private byte[] comment;

            public byte[] ScriptHash { get => scriptHash; set => scriptHash = value; }
            public long Amount { get => amount; set => amount = value; }
            public byte[] Comment { get => comment; set => comment = value; }
        }

        public static void TransferBalanceX(Client client, TransferXParams p)
        {
            client.InvokeFunction(BalanceContractHash, TransferXMethod, ExtraFee, p.Sender, p.Receiver, p.Amount, p.Comment);
        }

        public static void Mint(Client client, MintBurnParams p)
        {
            client.InvokeFunction(BalanceContractHash, MintMethod, ExtraFee, p.ScriptHash, p.Amount, p.Comment);
        }

        public static void Burn(Client client, MintBurnParams p)
        {
            client.InvokeFunction(BalanceContractHash, BurnMethod, ExtraFee, p.ScriptHash, p.Amount, p.Comment);
        }

        public static void LockAsset(Client client, LockParams p)
        {
            client.InvokeFunction(BalanceContractHash, LockMethod, ExtraFee, p.ID, p.UserAccount, p.LockAccount, p.Amount, p.Until);
        }


    }
}
