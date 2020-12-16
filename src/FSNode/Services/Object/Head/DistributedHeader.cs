using NeoFS.API.v2.Object;
using V2Object = NeoFS.API.v2.Object;
using Neo.Fs.Core.Container;
using Neo.Fs.Core.Netmap;

namespace Neo.Fs.Services.Object.Head
{
    public class DistributedHeader
    {
        private INetmapSource netmapSource;
        private IContainerSource containerSource;

        public V2Object.Object Head(HeadPrm prm)
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

        private V2Object.Object Finish(HeadPrm prm)
        {
            //TODO
            return new V2Object.Object();
        }
    }
}
