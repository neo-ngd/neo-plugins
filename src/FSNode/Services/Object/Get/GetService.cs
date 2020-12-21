using NeoFS.API.v2.Object;
using V2Object = NeoFS.API.v2.Object.Object;
using Neo.Fs.Services.Object.Range;

namespace Neo.Fs.Services.Object.Get
{
    public class GetService
    {
        private RangeService rngeService;

        public V2Object Get(GetPrm prm)
        {
            var obj = new V2Object();
            var range_prm = new RangePrm
            {
                Address = prm.Address,
                Full = true,
            };
            return new V2Object();
        }
    }
}
