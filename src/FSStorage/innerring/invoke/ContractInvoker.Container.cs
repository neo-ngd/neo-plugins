using Neo.Cryptography.ECC;
using Neo.Plugins.FSStorage.morph.invoke;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160 ContainerContractHash => Settings.Default.ContainerContractHash;
        private static string PutContainerMethod = "put";
        private static string DeleteContainerMethod = "delete";

        public class ContainerParams
        {
            private ECPoint key;
            private byte[] container;
            private byte[] signature;

            public ECPoint Key { get => key; set => key = value; }
            public byte[] Container { get => container; set => container = value; }
            public byte[] Signature { get => signature; set => signature = value; }
        }

        public class RemoveContainerParams
        {
            private byte[] containerID;
            private byte[] signature;

            public byte[] ContainerID { get => containerID; set => containerID = value; }
            public byte[] Signature { get => signature; set => signature = value; }
        }

        public static void RegisterContainer(Client client, ContainerParams p)
        {
            client.InvokeFunction(ContainerContractHash, PutContainerMethod, 2 * ExtraFee, p.Container, p.Signature, p.Key.EncodePoint(true));
        }

        public static void RemoveContainer(Client client, RemoveContainerParams p)
        {
            client.InvokeFunction(ContainerContractHash, DeleteContainerMethod, ExtraFee, p.ContainerID, p.Signature);
        }


    }
}
