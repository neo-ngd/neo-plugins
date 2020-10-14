using Neo.IO;
using Neo.SmartContract;
using System;

namespace Neo.Plugins.FSStorage
{
    public partial class MorphEvent
    {
        public class LockEvent : IContractEvent
        {
            private byte[] id;
            private UInt160 userAccount;
            private UInt160 lockAccount;
            private long amount;
            private long util;

            public byte[] Id { get => id; set => id = value; }
            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }
            public UInt160 LockAccount { get => lockAccount; set => lockAccount = value; }
            public long Amount { get => amount; set => amount = value; }
            public long Util { get => util; set => util = value; }

            //todo
            public void ContractEvent() { }
        }

        public static LockEvent ParseLockEvent(VM.Types.Array eventParams)
        {
            var lockEvent = new LockEvent();
            if (eventParams.Count != 5) throw new Exception();
            lockEvent.Id = eventParams[0].GetSpan().ToArray();
            lockEvent.UserAccount = eventParams[1].GetSpan().AsSerializable<UInt160>();
            lockEvent.LockAccount = eventParams[2].GetSpan().AsSerializable<UInt160>();
            lockEvent.Amount = (long)eventParams[3].GetInteger();
            lockEvent.Util = (long)eventParams[4].GetInteger();
            return lockEvent;
        }
    }
}
