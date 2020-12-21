using Neo.Fs.Network;
using System.Collections.Generic;

namespace Neo.Fs.Services.Object.Range
{
    public interface IPlacementTraverser
    {
        List<Address> Next();
        void SubmitSuccess();
        bool Success();
    }
}
