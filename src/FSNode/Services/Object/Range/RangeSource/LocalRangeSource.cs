using V2Range = NeoFS.API.v2.Object.Range;
using NeoFS.API.v2.Refs;
using Neo.Fs.LocalObjectStorage.LocalStore;
using System;

namespace Neo.Fs.Services.Object.Range.RangeSource
{
    public class LocalRangeSource : IRangeSource
    {
        private Storage localStorage;

        public byte[] Range(Address address, V2Range range)
        {
            return Array.Empty<byte>();
        }
    }
}
