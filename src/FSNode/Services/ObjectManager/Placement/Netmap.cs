using Neo.FSNode.Core.Netmap;
using NeoFS.API.v2.Netmap;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public class NetMapBuilder
    {
        private INetmapSource nmSrc;

        public NetMapBuilder(INetmapSource source)
        {
            this.nmSrc = source;
        }

        public NetMapBuilder(NetMap netMap)
        {
            this.nmSrc = new NetMapSrc(netMap);
        }
    }

    public class NetMapSrc : INetmapSource
    {
        private NetMap nm;

        public NetMapSrc(NetMap netMap)
        {
            this.nm = netMap;
        }

        public NetMap GetNetMap(ulong diff)
        {
            return this.nm;
        }
    }
}
