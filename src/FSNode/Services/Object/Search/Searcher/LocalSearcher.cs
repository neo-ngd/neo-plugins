using NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.LocalObjectStorage.LocalStore;
using System;
using System.Collections.Generic;

namespace Neo.Fs.Services.Object.Search.Searcher
{
    public class LocalSearcher : ISearcher
    {
        private Storage localStorage;

        public List<ObjectID> Search(ContainerID cid, SearchFilters Filters)
        {
            Filters.AddObjectContainerIDFilter(cid, MatchType.StringEqual);
            var addrs = localStorage.Select(Filters);
            if (addrs is null)
                throw new InvalidOperationException(nameof(LocalSearcher) + " could not select objects from local storage");
            return addrs;
        }
    }
}
