using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string putMethod = "put";
        private static string deleteMethod = "delete";
        private static string getMethod = "get";
        private static string listMethod = "list";
        private static string eACLMethod = "eACL";
        private static string setEACLMethod = "setEACL";

        public static UInt160 ContainerContractHash => Settings.Default.ContainerContractHash;
        public static string PutMethod { get => putMethod; set => putMethod = value; }
        public static string DeleteMethod { get => deleteMethod; set => deleteMethod = value; }
        public static string GetMethod { get => getMethod; set => getMethod = value; }
        public static string ListMethod { get => listMethod; set => listMethod = value; }
        public static string EACLMethod { get => eACLMethod; set => eACLMethod = value; }
        public static string SetEACLMethod { get => setEACLMethod; set => setEACLMethod = value; }

        public static bool InvokePut(Client client, byte[] ownerID, byte[] cnr, byte[] signature)
        {
            List<ContractParameter> contractParameters = new List<ContractParameter>();
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = ownerID });
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = cnr });
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = signature });
            return client.InvokeFunction(ContainerContractHash, PutMethod, ExtraFee, contractParameters.ToArray());
        }

        public static bool InvokeSetEACL(Client client, byte[] containerID, byte[] eacl, byte[] signature)
        {
            return client.InvokeFunction(ContainerContractHash, SetEACLMethod, ExtraFee, containerID, eacl, signature);
        }

        public static bool InvokeDelete(Client client, byte[] containerID, byte[] ownerID, byte[] signature)
        {
            return client.InvokeFunction(ContainerContractHash, DeleteMethod, ExtraFee, containerID, ownerID, signature);
        }
        public static byte[] InvokeGetEACL(Client client, byte[] containerID)
        {
            InvokeResult result = client.InvokeLocalFunction(ContainerContractHash, EACLMethod, containerID);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (EACL)");
            return result.ResultStack[0].GetSpan().ToArray();
        }

        public static byte[] InvokeGetContainer(Client client, byte[] containerID)
        {
            InvokeResult result = client.InvokeLocalFunction(ContainerContractHash, GetMethod, containerID);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Get)");
            return result.ResultStack[0].GetSpan().ToArray();
        }

        public static byte[][] InvokeGetContainerList(Client client, byte[] ownerID)
        {
            InvokeResult result = client.InvokeLocalFunction(ContainerContractHash, ListMethod, ownerID);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (List)");
            VM.Types.Array array = (VM.Types.Array)result.ResultStack[0];
            IEnumerator<StackItem> enumerator = array.GetEnumerator();
            List<byte[]> resultArray = new List<byte[]>();
            while (enumerator.MoveNext())
            {
                resultArray.Add(enumerator.Current.GetSpan().ToArray());
            }
            return resultArray.ToArray();
        }

    }
}
