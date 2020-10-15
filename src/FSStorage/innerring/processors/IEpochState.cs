namespace Neo.Plugins.FSStorage.innerring.processors
{
    public interface IEpochState
    {
        public void SetEpochCounter(ulong epoch);
        public ulong EpochCounter();
    }
}
