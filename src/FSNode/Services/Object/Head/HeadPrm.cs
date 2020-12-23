using NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Services.Object.Util;

namespace Neo.Fs.Services.Object.Head
{
    public class HeadPrm : CommonPrm
    {
        public bool Short;
        public Address Address;

        public static HeadPrm FromRequest(HeadRequest request)
        {
            var prm = new HeadPrm
            {
                Short = request.Body.MainOnly,
                Address = request.Body.Address,
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}