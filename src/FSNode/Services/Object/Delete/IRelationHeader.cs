using V2Object = NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Services.Object.Util;

namespace Neo.Fs.Services.Object.Delete
{
    public interface IRelationHeader
    {
        V2Object.Object HeadRelation(Address address, CommonPrm prm);
    }
}
