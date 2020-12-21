using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using Array = Neo.VM.Types.Array;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public partial class MorphContractInvoker
    {
        private static string PutMethod = "put";
        private static string DeleteMethod = "delete";
        private static string GetMethod = "get";
        private static string ListMethod = "list";
        private static string EACLMethod = "eACL";
        private static string SetEACLMethod = "setEACL";

        private static UInt160 ContainerContractHash => Settings.Default.ContainerContractHash;

        public class PutArgs {
            public byte[] cnr;
            public byte[] sig;
            public byte[] publicKey;
        }

        public class SetEACLArgs {
            public byte[] eacl;
            public byte[] sig;
        }

        public class DeleteArgs {
            public byte[] cid;
            public byte[] sig;
        }

        public class EACLValues {
            public byte[] eacl;
            public byte[] sig;
        }

        public static bool InvokePut(Client client, PutArgs args)
        {
            return client.InvokeFunction(ContainerContractHash, PutMethod, ExtraFee, args.cnr,args.sig,args.publicKey);
        }

        public static bool InvokeSetEACL(Client client, SetEACLArgs args)
        {
            return client.InvokeFunction(ContainerContractHash, SetEACLMethod, ExtraFee,args.eacl,args.sig);
        }

        public static bool InvokeDelete(Client client, DeleteArgs args)
        {
            return client.InvokeFunction(ContainerContractHash, DeleteMethod, ExtraFee, args.cid,args.sig);
        }
        public static EACLValues InvokeGetEACL(Client client, byte[] containerID)
        {
            InvokeResult result = client.InvokeLocalFunction(ContainerContractHash, EACLMethod, containerID);
            if (result.State != VM.VMState.HALT) throw new Exception("could not invoke method (EACL)");
            Array array = (Array)result.ResultStack[0];
            var eacl = array[0].GetSpan().ToArray();
            var sig = array[1].GetSpan().ToArray();
            EACLValues eACLValues = new EACLValues()
            {
                eacl = eacl,
                sig = sig
            };
            return eACLValues;
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
