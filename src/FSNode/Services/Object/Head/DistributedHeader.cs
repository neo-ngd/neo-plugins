using NeoFS.API.v2.Object;
using V2Object = NeoFS.API.v2.Object;
using Neo.FSNode.Core.Container;
using Neo.FSNode.Core.Netmap;

namespace Neo.FSNode.Services.Object.Head
{
    public class DistributedHeader
    {
        private INetmapSource netmapSource;
        private IContainerSource containerSource;

        public HeadResult Head(HeadPrm prm)
        {
            Prepare(prm);
            return Finish(prm);
        }

        private void Prepare(HeadPrm prm)
        {
            var netmap = netmapSource.GetLatestNetworkMap();
            var container = containerSource.Get(prm.Address.ContainerId);
            //TODO
        }

        private HeadResult Finish(HeadPrm prm)
        {
            //TODO
            return new HeadResult();
        }
    }
}
