using Google.Protobuf;
using V2Object = NeoFS.API.v2.Object.Object;

namespace Neo.Fs.Services.Object.Range
{
    public class RangeResult
    {
        public V2Object Header;
        public ByteString Chunk;
    }
}
