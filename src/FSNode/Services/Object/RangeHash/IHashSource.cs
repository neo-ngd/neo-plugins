using System;
using System.Collections.Generic;

namespace Neo.Fs.Services.Object.RangeHash
{
    public interface IHasherSource
    {
        void HashRange(RangeHashPrm prm, Action<List<byte[]>> handler);
    }
}