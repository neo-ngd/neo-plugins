namespace Neo.Plugins.FSStorage.innerring.timers
{
    public class EpochTickEvent
    {
        public class NewEpochTickEvent : IContractEvent
        {
            public void ContractEvent() { }
        }

        public class NewAlphabetEmitTickEvent : IContractEvent
        {
            public void ContractEvent() { }
        }

        public class NetmapCleanupTickEvent : IContractEvent
        {
            private ulong epoch;

            public ulong Epoch { get => epoch; set => epoch = value; }

            public void ContractEvent() { }
        }
    }
}
