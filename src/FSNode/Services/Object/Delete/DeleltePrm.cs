using NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Services.Object.Util;

namespace Neo.Fs.Services.Object.Delete
{
    public class DeletePrm : CommonPrm
    {
        public Address Address;

        public static DeletePrm FromRequest(DeleteRequest request)
        {
            return new DeletePrm
            {
                Address = request.Body.Address,
            };
        }
    }
}
