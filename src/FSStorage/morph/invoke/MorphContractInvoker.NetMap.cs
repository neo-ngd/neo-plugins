using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string AddPeerMethod = "addPeer";
        private static string NewEpochMethod = "newEpoch";
        private static string InnerRingListMethod = "innerRingList";
        private static string UpdateStateMethod = "updateState";
        private static string ConfigMethod = "config";
        private static string EpochMethod = "epoch";
        private static string SnapshotMethod = "snapshot";
        private static string NetMapMethod = "netmap";
        private static long ExtraFee = 0;
        private static UInt160 NetMapContractHash => Settings.Default.NetmapContractHash;

        public class UpdateStateArgs
        {
            public byte[] key;
            public long state;
        }

        public static bool InvokeAddPeer(Client client, byte[] info)
        {
            return client.InvokeFunction(NetMapContractHash, AddPeerMethod, ExtraFee, info);
        }

        public static byte[] InvokeConfig(Client client, byte[] key)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, ConfigMethod, key);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Config)");
            return result.ResultStack[0].GetSpan().ToArray();
        }
        public static long InvokeEpoch(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, EpochMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Epoch)");
            return (long)result.ResultStack[0].GetInteger();
        }

        public static bool InvokeNewEpoch(Client client, long epochNumber)
        {
            return client.InvokeFunction(NetMapContractHash, NewEpochMethod, ExtraFee, epochNumber);
        }

        public static byte[][] InvokeInnerRingList(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, InnerRingListMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (InnerRingList)");
            VM.Types.Array array = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> enumerator = array.GetEnumerator();
            List<byte[]> resultArray = new List<byte[]>();
            while (enumerator.MoveNext())
            {
                resultArray.Add(((Array)enumerator.Current)[0].GetSpan().ToArray());
            }
            return resultArray.ToArray();
        }

        public static bool InvokeUpdateState(Client client, UpdateStateArgs args)
        {
            return client.InvokeFunction(NetMapContractHash, UpdateStateMethod, ExtraFee, args.state, args.key);
        }

        public static byte[][] InvokeNetMap(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, NetMapMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (NetMap)");
            if(result.ResultStack.Length!=1) throw new Exception(string.Format("unexpected stack item count ({0})",result.ResultStack.Length));
            VM.Types.Array peers = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> peersEnumerator = peers.GetEnumerator();
            List<byte[]> res = new List<byte[]>();
            while (peersEnumerator.MoveNext())
            {
                VM.Types.Array peer = (VM.Types.Array)peersEnumerator.Current;
                if(peer.Count!=1) throw new Exception(string.Format("unexpected stack item count (PeerInfo): expected {0}, has {1}", 1, peer.Count));
                IEnumerator<StackItem> peerEnumerator = peer.GetEnumerator();
                while (peerEnumerator.MoveNext()) {
                    res.Add(peerEnumerator.Current.GetSpan().ToArray());
                }
            }
            return res.ToArray();
        }

        public static byte[][] InvokeSnapshot(Client client, int different)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, SnapshotMethod, different);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Snapshot)");
            if (result.ResultStack.Length != 1) throw new Exception(string.Format("unexpected stack item count ({0})", result.ResultStack.Length));
            VM.Types.Array peers = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> peersEnumerator = peers.GetEnumerator();
            List<byte[]> res = new List<byte[]>();
            while (peersEnumerator.MoveNext())
            {
                VM.Types.Array peer = (VM.Types.Array)peersEnumerator.Current;
                if (peer.Count != 1) throw new Exception(string.Format("unexpected stack item count (PeerInfo): expected {0}, has {1}", 1, peer.Count));
                IEnumerator<StackItem> peerEnumerator = peer.GetEnumerator();
                while (peerEnumerator.MoveNext())
                {
                    res.Add(peerEnumerator.Current.GetSpan().ToArray());
                }
            }
            return res.ToArray();
        }
    }
}
