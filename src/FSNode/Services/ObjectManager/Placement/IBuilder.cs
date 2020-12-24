using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System.Collections.Generic;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public interface IBuilder
    {
        List<Node[]> BuildPlacement(Address address, PlacementPolicy pp);
    }
}
