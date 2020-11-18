using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Plugins.FSStorage.morph.invoke;
using NeoFS.API.v2.Netmap;
using System;
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
        private static string SetConfigMethod = "setConfig";
        private static string UpdateInnerRingMethod = "updateInnerRing";
        private static string GetNetmapSnapshotMethod = "netmap";

        public class UpdatePeerArgs
        {
            private ECPoint key;
            private int status;

            public ECPoint Key { get => key; set => key = value; }
            public int Status { get => status; set => status = value; }
        }

        public class SetConfigArgs
        {
            private byte[] id;
            private byte[] key;
            private byte[] value;

            public byte[] Id { get => id; set => id = value; }
            public byte[] Key { get => key; set => key = value; }
            public byte[] Value { get => value; set => this.value = value; }
        }

        public static long GetEpoch(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, GetEpochMethod);
            if (result.State != VM.VMState.HALT) throw new Exception();
            return (long)(result.ResultStack[0].GetInteger());
        }

        public static bool SetNewEpoch(Client client, ulong epoch)
        {
           return client.InvokeFunction(NetMapContractHash, SetNewEpochMethod, ExtraFee, epoch);
        }

        public static bool ApprovePeer(Client client, byte[] peer)
        {
            return client.InvokeFunction(NetMapContractHash, ApprovePeerMethod, ExtraFee, peer);
        }

        public static bool UpdatePeerState(Client client, UpdatePeerArgs p)
        {
           return client.InvokeFunction(NetMapContractHash, UpdatePeerStateMethod, ExtraFee, p.Status, p.Key.ToArray());
        }

        public static bool SetConfig(Client client, SetConfigArgs p)
        {
           return client.InvokeFunction(NetMapContractHash, SetConfigMethod, ExtraFee,p.Id, p.Key, p.Value);
        }

        public static bool UpdateInnerRing(Client client, ECPoint[] p)
        {
            List<byte[]> keys = new List<byte[]>();
            foreach (ECPoint e in p)
            {
                keys.Add(e.ToArray());
            }
            return client.InvokeFunction(NetMapContractHash, UpdateInnerRingMethod, ExtraFee, keys.ToArray());
        }

        public static NodeInfo[] NetmapSnapshot(Client client)
        {
            InvokeResult invokeResult=client.InvokeLocalFunction(NetMapContractHash, GetNetmapSnapshotMethod);
            if (invokeResult.State != VM.VMState.HALT) throw new Exception("invalid RPC response");
            var rawNodeInfos = ((VM.Types.Array)invokeResult.ResultStack[0]).GetEnumerator();
            var result = new List<NeoFS.API.v2.Netmap.NodeInfo>();
            while (rawNodeInfos.MoveNext()) {
                var item = (VM.Types.Array)rawNodeInfos.Current;
                var rawNodeInfo = item[0].GetSpan().ToArray();
                NeoFS.API.v2.Netmap.NodeInfo node = NeoFS.API.v2.Netmap.NodeInfo.Parser.ParseFrom(rawNodeInfo);
                result.Add(node);
            }
            return result.ToArray();
        }
    }
}
