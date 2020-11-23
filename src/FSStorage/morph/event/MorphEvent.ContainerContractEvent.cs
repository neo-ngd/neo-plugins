using Neo.Cryptography.ECC;
using Neo.IO;
using System;

namespace Neo.Plugins.FSStorage
{
    public partial class MorphEvent
    {
        public class ContainerDeleteEvent : IContractEvent
        {
            private byte[] containerID;
            private byte[] signature;

            public byte[] ContainerID { get => containerID; set => containerID = value; }
            public byte[] Signature { get => signature; set => signature = value; }

            public void ContractEvent() { }
        }

        public class ContainerPutEvent : IContractEvent
        {
            private byte[] rawContainer;
            private byte[] signature;
            private ECPoint publicKey;

            public byte[] RawContainer { get => rawContainer; set => rawContainer = value; }
            public byte[] Signature { get => signature; set => signature = value; }
            public ECPoint PublicKey { get => publicKey; set => publicKey = value; }

            public void ContractEvent() { }
        }

        public static ContainerDeleteEvent ParseContainerDeleteEvent(VM.Types.Array eventParams)
        {
            var containerDeleteEvent = new ContainerDeleteEvent();
            if (eventParams.Count != 2) throw new Exception();
            containerDeleteEvent.ContainerID = eventParams[0].GetSpan().ToArray();
            containerDeleteEvent.Signature = eventParams[1].GetSpan().ToArray();
            return containerDeleteEvent;
        }

        public static ContainerPutEvent ParseContainerPutEvent(VM.Types.Array eventParams)
        {
            var containerPutEvent = new ContainerPutEvent();
            if (eventParams.Count != 3) throw new Exception();
            containerPutEvent.RawContainer = eventParams[0].GetSpan().ToArray();
            containerPutEvent.Signature = eventParams[1].GetSpan().ToArray();
            containerPutEvent.PublicKey = eventParams[2].GetSpan().ToArray().AsSerializable<ECPoint>(1);
            return containerPutEvent;
        }
    }
}
