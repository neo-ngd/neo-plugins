using Neo.Fs.LocalObjectStorage.LocalStore;
using NeoFS.API.v2.Object;
using NeoFS.API.v2.Refs;
using System.Linq;

namespace Neo.Fs.Services.Policer
{
    public class JobQueue
    {
        private Storage localStorage;
        private static SearchFilters jobFilters;


        public Address[] Select(int limit)
        {
            // TODO: optimize the logic for selecting objects
            // We can prioritize objects for migration, newly arrived objects, etc.
            // It is recommended to make changes after updating the metabase
            var res = this.localStorage.Select(GetJobFilter());
            if (res.Length < limit) return res;

            return res.Take(limit).ToArray();
        }

        private SearchFilters GetJobFilter()
        {
            if (jobFilters.Filters.Length == 0)
                jobFilters.AddPhyFilter();

            return jobFilters;
        }
    }
}
