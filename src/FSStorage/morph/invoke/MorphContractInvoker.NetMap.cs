using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string addPeerMethod = "AddPeer";
        private static string newEpochMethod = "NewEpoch";
        private static string innerRingListMethod = "InnerRingList";
        private static string updateStateMethod = "UpdateState";
        private static string netMapMethod = "NetMap";
        private static long extraFee = 0;

        public static UInt160 NetMapContractHash => Settings.Default.NetmapContractHash;
        public static string AddPeerMethod { get => addPeerMethod; set => addPeerMethod = value; }
        public static string NewEpochMethod { get => newEpochMethod; set => newEpochMethod = value; }
        public static string InnerRingListMethod { get => innerRingListMethod; set => innerRingListMethod = value; }
        public static string UpdateStateMethod { get => updateStateMethod; set => updateStateMethod = value; }
        public static string NetMapMethod { get => netMapMethod; set => netMapMethod = value; }
        public static long ExtraFee { get => extraFee; set => extraFee = value; }

        public class PeerInfo
        {
            byte[] address;
            byte[] key;
            byte[][] opts;

            public byte[] Address { get => address; set => address = value; }
            public byte[] Key { get => key; set => key = value; }
            public byte[][] Opts { get => opts; set => opts = value; }
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
