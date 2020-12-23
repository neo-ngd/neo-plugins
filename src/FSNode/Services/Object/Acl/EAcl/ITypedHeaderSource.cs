using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Session;
using System.Collections.Generic;

namespace Neo.Fs.Services.Object.Acl.EAcl
{
    public interface ITypedHeaderSource
    {
        List<XHeader> HeadersOfSource(HeaderType type);
    }
}
