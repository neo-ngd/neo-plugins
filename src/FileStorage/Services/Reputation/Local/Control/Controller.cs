using Neo.FileStorage.Services.Reputaion.Local.Storage;
using Neo.FileStorage.Storage;
using System.Collections.Generic;
using System.Threading;

namespace Neo.FileStorage.Services.Reputaion.Local.Control
{
    public class Controller
    {
        public ReputationStorage ReputationStorage { get; init; }
        public NetmapCache NetmapCache { get; init; }
        public byte[] LocalKey { get; init; }

        private readonly Dictionary<ulong, CancellationTokenSource> cancels = new();

        public void Report(ulong epoch)
        {
            var context = AcquireReport(epoch);
            context.Report();
            FreeReport(epoch);
        }

        public void Stop(ulong epoch)
        {
            FreeReport(epoch);
        }

        public ReportContext AcquireReport(ulong epoch)
        {
            if (cancels.ContainsKey(epoch))
            {
                return null;
            }
            var source = new CancellationTokenSource();
            return new()
            {
                Cancel = source.Token,
                Epoch = epoch,
                Controller = this,
            };
        }

        public void FreeReport(ulong epoch)
        {
            if (cancels.TryGetValue(epoch, out CancellationTokenSource source))
            {
                source.Cancel();
                cancels.Remove(epoch);
            }
        }
    }
}