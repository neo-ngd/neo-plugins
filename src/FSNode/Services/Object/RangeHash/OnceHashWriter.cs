using System.Collections.Generic;
using System.Threading;

namespace Neo.Fs.Services.Object.RangeHash
{
    public class OnceHashWriter
    {
        private bool writed = false;
        public CancellationTokenSource TokenSource;
        public IPlacementTraverser Traverser;
        public RangeHashResult Result;


        public void Write(List<byte[]> hashes)
        {
            if (writed) return;
            Result.Hashes = hashes;
            Traverser.SubmitSuccess();
            TokenSource.Cancel();
        }
    }
}