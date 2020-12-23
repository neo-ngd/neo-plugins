using NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Services.Object.Util;

namespace Neo.Fs.Services.Object.Get
{
    public class GetPrm : CommonPrm
    {
        public bool Full;
        public Address Address;

        public static GetPrm FromRequest(GetRequest request)
        {
            var prm = new GetPrm
            {
                Address = request.Body.Address,
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}
