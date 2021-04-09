using Neo.FileStorage.Morph.Invoker;
using FSAnnouncement = Neo.FileStorage.API.Container.AnnounceUsedSpaceRequest.Types.Body.Types.Announcement;

namespace Neo.FileStorage.Services.Container.Announcement.Control
{
    public class MorphLoadWriter
    {
        private readonly MorphClient client;
        private readonly byte[] key;

        public void Put(FSAnnouncement announcement)
        {
            MorphContractInvoker.InvokePutSize(client, announcement.Epoch, announcement.ContainerId, announcement.UsedSpace, key);
        }
    }
}
