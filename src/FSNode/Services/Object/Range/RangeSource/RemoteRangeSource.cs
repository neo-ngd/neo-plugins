using V2Range = NeoFS.API.v2.Object.Range;
using NeoFS.API.v2.Refs;
using System;

namespace Neo.Fs.Services.Object.Range.RangeSource
{
    public class RemoteRangeSource : IRangeSource
    {
        public byte[] Range(Address address, V2Range range)
        {
            return Array.Empty<byte>();
        }
    }
}
