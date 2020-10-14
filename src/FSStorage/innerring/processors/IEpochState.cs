using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins.FSStorage.innerring.processors
{
    public interface IEpochState
    {
        public void SetEpochCounter(ulong epoch);
        public ulong EpochCounter();
    }
}
