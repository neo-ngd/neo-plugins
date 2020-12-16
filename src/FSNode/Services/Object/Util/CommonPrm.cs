using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Session;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Fs.Services.Object.Util
{
    public class CommonPrm
    {
        public bool Local;
        public SessionToken Token;
        public BearerToken Bearer;

        public static CommonPrm FromRequest(IRequest request)
        {
            var meta = request.MetaHeader;
            return new CommonPrm
            {
                Local = meta.Ttl <= 1,
                Token = meta.SessionToken,
                Bearer = meta.BearerToken,
            };
        }

        public void WithCommonPrm(CommonPrm cprm)
        {
            Local = cprm.Local;
            Token = cprm.Token;
            Bearer = cprm.Bearer;
        }
    }
}
