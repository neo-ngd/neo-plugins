using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string AddPeerMethod = "AddPeer";
        private static string NewEpochMethod = "NewEpoch";
        private static string InnerRingListMethod = "InnerRingList";
        private static string UpdateStateMethod = "UpdateState";
        private static string NetMapMethod = "NetMap";
        private static long ExtraFee = 0;
        private static UInt160 NetMapContractHash => Settings.Default.NetmapContractHash;

        public class PeerInfo
        {
            public byte[] Address;
            public byte[] Key;
            public byte[][] Opts;
        }

        public static bool InvokeAddPeer(Client client, PeerInfo peerInfo)
        {
            VM.Types.Array array = new VM.Types.Array();
            array.Add(peerInfo.Address);
            array.Add(peerInfo.Key);
            List<ContractParameter> contractParameters = new List<ContractParameter>();
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = peerInfo.Address });
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = peerInfo.Key });
            for (int i = 0; i < peerInfo.Opts.Length; i++)
            {
                contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = peerInfo.Opts[i] });
            }

            return client.InvokeFunction(NetMapContractHash, AddPeerMethod, ExtraFee, contractParameters.ToArray());
        }

        public static bool InvokeNewEpoch(Client client, long epochNumber)
        {
            return client.InvokeFunction(NetMapContractHash, NewEpochMethod, ExtraFee, new BigInteger(epochNumber));
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
                resultArray.Add(enumerator.Current.GetSpan().ToArray());
            }
            return resultArray.ToArray();
        }

        public static bool InvokeUpdateState(Client client, int state, byte[] publicKey)
        {
            return client.InvokeFunction(NetMapContractHash, UpdateStateMethod, ExtraFee, new BigInteger(state), publicKey);
        }

        public static PeerInfo[] InvokeGetNetMap(Client client)
        {
            InvokeResult result = client.InvokeLocalFunction(NetMapContractHash, NetMapMethod);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (NetMap)");
            VM.Types.Array array = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> enumerator = array.GetEnumerator();
            List<PeerInfo> peerInfoList = new List<PeerInfo>();
            while (enumerator.MoveNext())
            {
                PeerInfo peerInfo = new PeerInfo();
                VM.Types.Array paremterArray = (VM.Types.Array)enumerator.Current;
                peerInfo.Address = paremterArray[0].GetSpan().ToArray();
                peerInfo.Key = paremterArray[1].GetSpan().ToArray();
                VM.Types.Array optsArray = (VM.Types.Array)paremterArray[2];
                IEnumerator<StackItem> optsEnumerator = optsArray.GetEnumerator();
                List<byte[]> optsList = new List<byte[]>();
                while (optsEnumerator.MoveNext())
                {
                    optsList.Add(optsEnumerator.Current.GetSpan().ToArray());
                }
                peerInfo.Opts = optsList.ToArray();
                peerInfoList.Add(peerInfo);
            }
            return peerInfoList.ToArray();
        }
    }
}
