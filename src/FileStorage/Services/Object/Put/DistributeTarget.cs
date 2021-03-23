using Google.Protobuf;
using V2Address = Neo.FileStorage.API.Refs.Address;
using V2Object = Neo.FileStorage.API.Object.Object;
using Neo.FileStorage.Network;
using Neo.FileStorage.Services.Object.Put.Store;
using Neo.FileStorage.Services.ObjectManager.Placement;
using Neo.FileStorage.Services.ObjectManager.Transformer;
using System;
using System.Threading.Tasks;

namespace Neo.FileStorage.Services.Object.Put
{
    public class DistributeTarget : ValidatingTarget
    {
        private ILocalAddressSource localAddressSource;
        public PutInitPrm Prm;
        private V2Object obj;

        public override void WriteHeader(V2Object init)
        {
            base.WriteHeader(init);
            obj = init;
        }

        public override void WriteChunk(byte[] chunk)
        {
            base.WriteChunk(chunk);
        }

        public override AccessIdentifiers Close()
        {
            base.Close();
            var traverser = new Traverser()
            {
                Address = new V2Address
                {
                    ContainerId = Prm.Container.CalCulateAndGetId,
                    ObjectId = Prm.Init.ObjectId,
                },
                Builder = Prm.Builder,
                Policy = Prm.Container.PlacementPolicy,
            };
            if (!ObjectValidator.ValidateContent(obj))
                throw new InvalidOperationException(nameof(DistributeTarget) + " invalid content");
            obj.Payload = ByteString.CopyFrom(payload);
            while (true)
            {
                var addrs = traverser.Next();
                if (addrs.Length == 0) break;
                var tasks = new Task[addrs.Length];
                for (int i = 0; i < addrs.Length; i++)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        IStore store = null;
                        if (addrs[i].IsLocalAddress(localAddressSource))
                        {
                            store = new LocalStore();
                        }
                        else
                        {
                            store = new RemoteStore();
                        }
                        try
                        {
                            store.Put(obj);
                            traverser.SubmitSuccess();
                        }
                        catch
                        {

                        }
                    });
                }
                Task.WaitAll(tasks);
            }
            if (!traverser.Success())
                throw new InvalidOperationException(nameof(DistributeTarget) + " incomplete object put");
            return new AccessIdentifiers
            {
                Self = obj.ObjectId,
            };
        }
    }
}
