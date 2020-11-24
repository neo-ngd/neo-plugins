using Neo.Cryptography.ECC;
using Neo.IO;
using System;

namespace Neo.Plugins.FSStorage
{
    partial class MorphEvent
    {
        public class NewEpochEvent : IContractEvent
        {
            public ulong EpochNumber;

            public void ContractEvent() { }
        }

        public class AddPeerEvent : IContractEvent
        {
            public byte[] Node;

            public void ContractEvent() { }
        }

        public class UpdatePeerEvent : IContractEvent
        {
            public ECPoint PublicKey;
            public uint Status;

            public void ContractEvent() { }
        }

        public static NewEpochEvent ParseNewEpochEvent(VM.Types.Array eventParams)
        {
            var newEpochEvent = new NewEpochEvent();
            if (eventParams.Count != 1) throw new Exception();
            newEpochEvent.EpochNumber = (ulong)eventParams[0].GetInteger();
            return newEpochEvent;
        }

        public static AddPeerEvent ParseAddPeerEvent(VM.Types.Array eventParams)
        {
            var addPeerEvent = new AddPeerEvent();
            if (eventParams.Count != 1) throw new Exception();
            addPeerEvent.Node = eventParams[0].GetSpan().ToArray();
            return addPeerEvent;
        }

        public static UpdatePeerEvent ParseUpdatePeerEvent(VM.Types.Array eventParams)
        {
            var updatePeerEvent = new UpdatePeerEvent();
            if (eventParams.Count != 2) throw new Exception();
            updatePeerEvent.PublicKey = eventParams[0].GetSpan().ToArray().AsSerializable<ECPoint>(1);
            updatePeerEvent.Status = (uint)eventParams[1].GetInteger();
            return updatePeerEvent;
        }
    }
}
