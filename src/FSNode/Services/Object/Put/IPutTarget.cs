using Google.Protobuf;
using V2Object = NeoFS.API.v2.Object;

namespace Neo.FSNode.Services.Object.Put
{
    public interface IPutTarget
    {
        void PutInit(V2Object.Object obj);
        PutResult PutPayload(ByteString payload);
    }
}