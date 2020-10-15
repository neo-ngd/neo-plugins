using Neo.Cryptography.ECC;
using Neo.Plugins.FSStorage.morph.invoke;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160 NetMapContractHash => Settings.Default.NetmapContractHash;
        private static string GetEpochMethod = "epoch";
        private static string SetNewEpochMethod = "newEpoch";
        private static string ApprovePeerMethod = "addPeer";
        private static string UpdatePeerStateMethod = "updateState";
        private static string SetConfigMethod = "setConfigMethod";
        private static string UpdateInnerRingMethod = "updateInnerRingMethod";

        public class UpdatePeerArgs
        {
            private ECPoint key;
            private uint status;

            public ECPoint Key { get => key; set => key = value; }
            public uint Status { get => status; set => status = value; }
        }

        public class SetConfigArgs
        {
            private byte[] key;
            private byte[] value;

            public byte[] Key { get => key; set => key = value; }
            public byte[] Value { get => value; set => this.value = value; }
        }

        public static long GetEpoch(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, GetEpochMethod);
            if (result.State != VM.VMState.HALT) return 0;
            return (long)(result.ResultStack[0].GetInteger());
        }

        public static void SetNewEpoch(Client client, ulong epoch)
        {
            client.InvokeFunction(NetMapContractHash, SetConfigMethod, ExtraFee, epoch);
        }

        public static void ApprovePeer(Client client, byte[] peer)
        {
            client.InvokeFunction(NetMapContractHash, ApprovePeerMethod, ExtraFee, peer);
        }

        public static void UpdatePeerState(Client client, UpdatePeerArgs p)
        {
            client.InvokeFunction(NetMapContractHash, UpdatePeerStateMethod, ExtraFee, p.Key.EncodePoint(true), p.Status);
        }

        public static void SetConfig(Client client, SetConfigArgs p)
        {
            client.InvokeFunction(NetMapContractHash, SetConfigMethod, ExtraFee, p.Key, p.Value);
        }

        public static void UpdateInnerRing(Client client, ECPoint[] p)
        {
            List<byte[]> keys = new List<byte[]>();
            foreach (ECPoint e in p)
            {
                keys.Add(e.EncodePoint(true));
            }
            client.InvokeFunction(NetMapContractHash, UpdateInnerRingMethod, ExtraFee, keys.ToArray());
        }
    }
}
