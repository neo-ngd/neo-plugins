using Neo.Plugins.FSStorage.morph.invoke;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160 BalanceContractHash => Settings.Default.BalanceContractHash;
        private const string TransferXMethod = "transferX";
        private const string LockMethod = "lock";
        private const string MintMethod = "mint";
        private const string BurnMethod = "burn";
        private const string PrecisionMethod = "decimals";

        private const long ExtraFee = 1_5000_0000;

        public class TransferXParams
        {
            public byte[] Sender;
            public byte[] Receiver;
            public long Amount;
            public byte[] Comment;
        }

        public class LockParams
        {
            public byte[] ID;
            public UInt160 UserAccount;
            public UInt160 LockAccount;
            public long Amount;
            public ulong Until;
        }

        public class MintBurnParams
        {
            public byte[] ScriptHash;
            public long Amount;
            public byte[] Comment;
        }

        public static bool TransferBalanceX(Client client, TransferXParams p)
        {
            return client.InvokeFunction(BalanceContractHash, TransferXMethod, ExtraFee, p.Sender, p.Receiver, p.Amount, p.Comment);
        }

        public static bool Mint(Client client, MintBurnParams p)
        {
            return client.InvokeFunction(BalanceContractHash, MintMethod, ExtraFee, p.ScriptHash, p.Amount, p.Comment);
        }

        public static bool Burn(Client client, MintBurnParams p)
        {
            return client.InvokeFunction(BalanceContractHash, BurnMethod, ExtraFee, p.ScriptHash, p.Amount, p.Comment);
        }

        public static bool LockAsset(Client client, LockParams p)
        {
            return client.InvokeFunction(BalanceContractHash, LockMethod, ExtraFee, p.ID, p.UserAccount, p.LockAccount, p.Amount, (int)p.Until);
        }

        public static uint BalancePrecision(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(BalanceContractHash, PrecisionMethod);
            return (uint)result.ResultStack[0].GetInteger();
        }
    }
}
