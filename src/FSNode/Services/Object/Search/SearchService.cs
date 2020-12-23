using NeoFS.API.v2.Refs;
using Neo.FSNode.Core.Container;
using Neo.FSNode.Core.Netmap;
using Neo.FSNode.Network;
using Neo.FSNode.Services.Object.Search.Searcher;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.FSNode.Services.Object.Search
{
    public class SearchService
    {
        private INetmapSource netmapSource;
        private IContainerSource containerSource;
        private ILocalAddressSource localAddressSource;

        public List<ObjectID> Search(SearchPrm prm)
        {
            var traverser = PreparePlacementTraverser(prm);
            if (traverser is null)
                throw new InvalidOperationException(nameof(SearchService) + " could not prepare placement traverser");
            return Finish(prm, traverser);
        }

        private IPlacementTraverser PreparePlacementTraverser(SearchPrm prm)
        {
            var nm = netmapSource.GetLatestNetworkMap();
            if (nm is null)
                throw new InvalidOperationException(nameof(SearchService) + " could not get latest network map");
            var container = containerSource.Get(prm.CID);
            if (container is null)
                throw new InvalidOperationException(nameof(SearchService) + " could not get container");
            //Traverser Options
            //New traverser
            return null;
        }

        //TODO: optimize
        private List<ObjectID> Finish(SearchPrm prm, IPlacementTraverser traverser)
        {
            var oids = new ConcurrentBag<ObjectID>();
            while (true)
            {
                var addrs = traverser.Next();
                if (addrs.Count == 0) break;
                var tasks = new List<Task>();
                foreach (var addr in addrs)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        ISearcher searcher;
                        if (addr.IsLocalAddress(localAddressSource))
                        {
                            searcher = new LocalSearcher();
                        }
                        else
                        {
                            searcher = new RemoteSearcher();
                        }
                        var res = searcher.Search(prm.CID, prm.Filters);
                        oids = new ConcurrentBag<ObjectID>(oids.Union(res));
                    }));
                }
            }
            return oids.ToList();
        }
    }
}
