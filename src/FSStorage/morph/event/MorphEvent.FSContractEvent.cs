using Neo.Cryptography.ECC;
using Neo.IO;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage
{
    partial class MorphEvent
    {
        public class BindEvent : IContractEvent
        {
            private UInt160 userAccount;
            private ECPoint[] keys;

            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }
            public ECPoint[] Keys { get => keys; set => keys = value; }

            public void ContractEvent() { }
        }

        public class UnbindEvent : IContractEvent
        {
            private UInt160 userAccount;
            private ECPoint[] keys;

            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }
            public ECPoint[] Keys { get => keys; set => keys = value; }

            public void ContractEvent() { }
        }

        public class ChequeEvent : IContractEvent
        {
            private byte[] id;
            private long amount;
            private UInt160 userAccount;
            private UInt160 lockAccount;

            public byte[] Id { get => id; set => id = value; }
            public long Amount { get => amount; set => amount = value; }
            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }
            public UInt160 LockAccount { get => lockAccount; set => lockAccount = value; }

            public void ContractEvent() { }
        }

        public class DepositEvent : IContractEvent
        {
            private byte[] id;
            private long amount;
            private UInt160 from;
            private UInt160 to;

            public byte[] Id { get => id; set => id = value; }
            public long Amount { get => amount; set => amount = value; }
            public UInt160 From { get => from; set => from = value; }
            public UInt160 To { get => to; set => to = value; }

            public void ContractEvent() { }
        }

        public class WithdrawEvent : IContractEvent
        {
            private byte[] id;
            private long amount;
            private UInt160 userAccount;

            public byte[] Id { get => id; set => id = value; }
            public long Amount { get => amount; set => amount = value; }
            public UInt160 UserAccount { get => userAccount; set => userAccount = value; }

            public void ContractEvent() { }
        }

        public class ConfigEvent : IContractEvent
        {
            private byte[] id;
            private byte[] key;
            private byte[] value;

            public byte[] Key { get => key; set => key = value; }
            public byte[] Value { get => value; set => this.value = value; }
            public byte[] Id { get => id; set => id = value; }

            public void ContractEvent() { }
        }

        public class UpdateInnerRingEvent : IContractEvent
        {
            private ECPoint[] keys;

            public ECPoint[] Keys { get => keys; set => keys = value; }

            public void ContractEvent() { }
        }


        public static BindEvent ParseBindEvent(VM.Types.Array eventParams)
        {
            var bindEvent = new BindEvent();
            if (eventParams.Count != 2) throw new Exception();
            bindEvent.UserAccount = eventParams[0].GetSpan().AsSerializable<UInt160>();
            List<ECPoint> keys = new List<ECPoint>();
            var bindKeys = ((VM.Types.Array)eventParams[1]).GetEnumerator();
            while (bindKeys.MoveNext())
            {
                var key = bindKeys.Current.GetSpan().AsSerializable<ECPoint>();
                keys.Add(key);
            }
            bindEvent.Keys = keys.ToArray();
            return bindEvent;
        }

        public static UnbindEvent ParseUnbindEvent(VM.Types.Array eventParams)
        {
            var unbindEvent = new UnbindEvent();
            if (eventParams.Count != 2) throw new Exception();
            unbindEvent.UserAccount = eventParams[0].GetSpan().AsSerializable<UInt160>();
            List<ECPoint> keys = new List<ECPoint>();
            var bindKeys = ((VM.Types.Array)eventParams[1]).GetEnumerator();
            while (bindKeys.MoveNext())
            {
                var key = bindKeys.Current.GetSpan().AsSerializable<ECPoint>();
                keys.Add(key);
            }
            unbindEvent.Keys = keys.ToArray();
            return unbindEvent;
        }

        public static ChequeEvent ParseChequeEvent(VM.Types.Array eventParams)
        {
            var chequeEvent = new ChequeEvent();
            if (eventParams.Count != 4) throw new Exception();
            chequeEvent.Id = eventParams[0].GetSpan().ToArray();
            chequeEvent.UserAccount = eventParams[1].GetSpan().AsSerializable<UInt160>();
            chequeEvent.Amount = (long)eventParams[2].GetInteger();
            chequeEvent.LockAccount = eventParams[3].GetSpan().AsSerializable<UInt160>();
            return chequeEvent;
        }

        public static DepositEvent ParseDepositEvent(VM.Types.Array eventParams)
        {
            var depositEvent = new DepositEvent();
            if (eventParams.Count != 4) throw new Exception();
            depositEvent.From = eventParams[0].GetSpan().AsSerializable<UInt160>();
            depositEvent.Amount = (long)eventParams[1].GetInteger();
            depositEvent.To = eventParams[2].GetSpan().AsSerializable<UInt160>();
            depositEvent.Id = eventParams[3].GetSpan().ToArray();
            return depositEvent;
        }

        public static WithdrawEvent ParseWithdrawEvent(VM.Types.Array eventParams)
        {
            var withdrawEvent = new WithdrawEvent();
            if (eventParams.Count != 3) throw new Exception();
            withdrawEvent.UserAccount = eventParams[0].GetSpan().AsSerializable<UInt160>();
            withdrawEvent.Amount = (long)eventParams[1].GetInteger();
            withdrawEvent.Id = eventParams[2].GetSpan().ToArray();
            return withdrawEvent;
        }

        public static ConfigEvent ParseConfigEvent(VM.Types.Array eventParams)
        {
            var configEvent = new ConfigEvent();
            if (eventParams.Count != 2) throw new Exception();
            configEvent.Key = eventParams[0].GetSpan().ToArray();
            configEvent.Value = eventParams[1].GetSpan().ToArray();
            return configEvent;
        }

        public static UpdateInnerRingEvent ParseUpdateInnerRingEvent(VM.Types.Array eventParams)
        {
            var updateInnerRingEvent = new UpdateInnerRingEvent();
            if (eventParams.Count != 1) throw new Exception();

            List<ECPoint> keys = new List<ECPoint>();
            var irKeys = ((VM.Types.Array)eventParams[0]).GetEnumerator();
            while (irKeys.MoveNext())
            {
                var key = irKeys.Current.GetSpan().AsSerializable<ECPoint>();
                keys.Add(key);
            }
            updateInnerRingEvent.Keys = keys.ToArray();
            return updateInnerRingEvent;
        }
    }
}
