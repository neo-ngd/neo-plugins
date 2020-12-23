using Neo.FSNode.Core.Container;
using Neo.FSNode.Core.Netmap;
using Neo.FSNode.Network;
using Neo.FSNode.Services.Object.RangeHash.HasherSource;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.FSNode.Services.Object.RangeHash
{
    public class DistributedHasher
    {
        private IPlacementTraverser traverser;
        private INetmapSource netmapSource;
        private IContainerSource containerSource;
        private ILocalAddressSource localAddressSource;

        public RangeHashResult Head(RangeHashPrm prm)
        {
            Prepare(prm);
            return Finish(prm);
        }

        private void Prepare(RangeHashPrm prm)
        {
            var nm = netmapSource.GetLatestNetworkMap();
            if (nm is null)
                throw new InvalidOperationException(nameof(Prepare) + " could not get latest network map");
            var container = containerSource.Get(prm.Address.ContainerId);
            if (container is null)
                throw new InvalidOperationException(nameof(Prepare) + " could not get container");
            //Traverser Options
            //New traverser
        }

        private RangeHashResult Finish(RangeHashPrm prm)
        {
            var result = new RangeHashResult();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            var once_writer = new OnceHashWriter
            {
                TokenSource = source,
                Traverser = traverser,
                Result = result
            };
            while (true)
            {
                var addrs = traverser.Next();
                if (addrs.Count == 0) break;
                var list = new List<Task>();
                foreach (var addr in addrs)
                {
                    //TODO: use workpool
                    list.Add(Task.Factory.StartNew(() =>
                    {
                        IHasherSource hasher;
                        if (addr.IsLocalAddress(localAddressSource))
                        {
                            hasher = new LocalHasherSource();
                        }
                        else
                        {
                            hasher = new RemoteHasherSource();
                        }
                        hasher.HashRange(prm, once_writer.Write);
                    }, token));
                }
                Task.WaitAll(list.ToArray());
            }
            if (!traverser.Success())
                throw new InvalidOperationException(nameof(Finish) + " incomplete object GetRangeHash operation");
            return result;
        }
    }
}
