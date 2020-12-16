using NeoFS.API.v2.Object;
using V2Object = NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Services.Object.Search;
using System;

namespace Neo.Fs.Services.Object.Head
{
    public class HeadService
    {
        private RelationSearcher relationSearcher;

        public HeadService(RelationSearcher relation_searcher)
        {
            relationSearcher = relation_searcher;
        }

        public V2Object.Object Head(HeadPrm prm)
        {
            var distribute_header = new DistributedHeader();
            var obj = distribute_header.Head(prm);
            if (obj != null || prm.Local) return obj;
            var oid = relationSearcher.SearchRelation(prm.Address, prm);
            var address = new Address
            {
                ContainerId = prm.Address.ContainerId,
                ObjectId = oid,
            };
            var right_child_prm = new HeadPrm
            {
                Address = address,
            };
            right_child_prm.WithCommonPrm(prm);
            obj = Head(right_child_prm);
            if (obj is null) throw new InvalidOperationException(nameof(Head) + " could not get right child header");
            return obj;
        }
    }
}