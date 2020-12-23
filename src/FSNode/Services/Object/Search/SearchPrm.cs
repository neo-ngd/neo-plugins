using NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Services.Object.Util;

namespace Neo.Fs.Services.Object.Search
{
    public class SearchPrm : CommonPrm
    {
        public ContainerID CID;
        public SearchFilters Filters;

        public static SearchPrm FromRequest(SearchRequest request)
        {
            var prm = new SearchPrm
            {
                CID = request.Body.ContainerId,
                Filters = new SearchFilters(request.Body.Filters),
            };
            prm.WithCommonPrm(CommonPrm.FromRequest(request));
            return prm;
        }
    }
}
