using System.Collections.Generic;

namespace Neo.Fs.Services.Object.Acl
{
    public interface IInnerRingFetcher
    {
        List<byte[]> InnerRingKeys();
    }
}
