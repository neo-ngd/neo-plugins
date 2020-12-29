using Neo.FSNode.Core.Netmap;
using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System.Collections.Generic;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public class NetmapBuilder : IBuilder
    {
        private readonly INetmapSource netmapSource;

        public NetmapBuilder(INetmapSource source)
        {
            netmapSource = source;
        }

        public NetmapBuilder(NetMap netMap)
        {
            netmapSource = new NetmapSource(netMap);
        }

        public virtual List<Node[]> BuildPlacement(Address address, PlacementPolicy pp)
        {
            return new List<Node[]>();
        }
    }

    public class NetmapSource : INetmapSource
    {
        private NetMap netmap;

        public NetmapSource(NetMap netmap)
        {
            this.netmap = netmap;
        }

        public NetMap GetNetMap(ulong diff)
        {
            return netmap;
        }
    }
}
