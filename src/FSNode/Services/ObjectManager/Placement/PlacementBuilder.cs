using Neo.FSNode.Core.Netmap;
using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System.Collections.Generic;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public class PlacementBuilder : IBuilder
    {
        private INetmapSource nmSrc;

        public PlacementBuilder(INetmapSource source)
        {
            nmSrc = source;
        }

        public PlacementBuilder(NetMap netMap)
        {
            nmSrc = new NetMapSrc(netMap);
        }

        public virtual List<Node[]> BuildPlacement(Address address, PlacementPolicy pp)
        {
            return new List<Node[]>();
        }
    }

    public class NetMapSrc : INetmapSource
    {
        private NetMap nm;

        public NetMapSrc(NetMap netMap)
        {
            nm = netMap;
        }

        public NetMap GetNetMap(ulong diff)
        {
            return nm;
        }
    }
}
