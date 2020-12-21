using Neo.Fs.Core.Netmap;
using NeoFS.API.v2.Netmap;

namespace Neo.Fs.Services.ObjectManager.Placement
{
    public class NetMapBuilder
    {
        private ISource nmSrc;

        public NetMapBuilder(ISource source)
        {
            this.nmSrc = source;
        }

        public NetMapBuilder(NetMap netMap)
        {
            this.nmSrc = new NetMapSrc(netMap);
        }
    }

    public class NetMapSrc : ISource
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
