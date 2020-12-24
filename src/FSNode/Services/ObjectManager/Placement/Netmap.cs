using Neo.FSNode.Core.Netmap;
using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System.Collections.Generic;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public class NetMapBuilder : IBuilder
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

        public List<Node[]> BuildPlacement(Address address, PlacementPolicy pp)
        {
            return new List<Node[]>();
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
