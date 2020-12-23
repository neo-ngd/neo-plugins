using Neo.FSNode.Network;
using System.Collections.Generic;

namespace Neo.FSNode.Services.Object
{
    public interface IPlacementTraverser
    {
        List<Address> Next();
        void SubmitSuccess();
        bool Success();
    }
}
