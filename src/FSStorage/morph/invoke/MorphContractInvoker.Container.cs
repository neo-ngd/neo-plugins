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
        private UInt160 containerContractHash = UInt160.Zero;
        private string putMethod = "put";
        private string deleteMethod = "delete";
        private string getMethod = "get";
        private string listMethod = "list";
        private string eACLMethod = "eACL";
        private string setEACLMethod = "setEACL";

        public UInt160 ContainerContractHash { get => containerContractHash; set => containerContractHash = value; }
        public string PutMethod { get => putMethod; set => putMethod = value; }
        public string DeleteMethod { get => deleteMethod; set => deleteMethod = value; }
        public string GetMethod { get => getMethod; set => getMethod = value; }
        public string ListMethod { get => listMethod; set => listMethod = value; }
        public string EACLMethod { get => eACLMethod; set => eACLMethod = value; }
        public string SetEACLMethod { get => setEACLMethod; set => setEACLMethod = value; }

        public bool InvokePut(Client client, byte[] ownerID, byte[] cnr, byte[] signature)
        {
            List<ContractParameter> contractParameters = new List<ContractParameter>();
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = ownerID });
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = cnr });
            contractParameters.Add(new ContractParameter() { Type = ContractParameterType.ByteArray, Value = signature });
            return client.InvokeFunction(ContainerContractHash, PutMethod, ExtraFee, contractParameters.ToArray());
        }

        public bool InvokeSetEACL(Client client, byte[] containerID, byte[] eacl, byte[] signature)
        {
            return client.InvokeFunction(ContainerContractHash, SetEACLMethod, ExtraFee, containerID, eacl, signature);
        }

        public bool InvokeDelete(Client client, byte[] containerID, byte[] ownerID, byte[] signature)
        {
            return client.InvokeFunction(ContainerContractHash, DeleteMethod, ExtraFee, containerID, ownerID, signature);
        }
        public byte[] InvokeGetEACL(Client client, byte[] containerID)
        {
            InvokeResult result = client.InvokeLocalFunction(ContainerContractHash, EACLMethod, containerID);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (EACL)");
            return result.ResultStack[0].GetSpan().ToArray();
        }

        public byte[] InvokeGetContainer(Client client, byte[] containerID)
        {
            InvokeResult result = client.InvokeLocalFunction(ContainerContractHash, GetMethod, containerID);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (Get)");
            return result.ResultStack[0].GetSpan().ToArray();
        }

        public byte[][] InvokeGetContainerList(Client client, byte[] ownerID)
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
