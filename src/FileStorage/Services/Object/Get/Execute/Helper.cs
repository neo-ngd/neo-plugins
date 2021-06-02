using Google.Protobuf;
using Neo.FileStorage.API.Session;
using Neo.FileStorage.LocalObjectStorage.Engine;
using Neo.FileStorage.Services.Reputaion.Local.Client;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Services.Object.Get.Execute
{
    public static class Helper
    {
        public static FSObject GetObject(this ReputationClient client, ExecuteContext context)
        {
            if (!context.Assembling && context.Prm.Forwarder is not null)
                return context.Prm.Forwarder.Forward(client.FSClient);
            var options = context.Prm.CallOptions
                .WithExtraXHeaders(new XHeader[] { new() { Key = XHeader.XHeaderNetmapEpoch, Value = context.CurrentEpoch.ToString() } })
                .WithKey(context.Prm.Key);
            if (context.HeadOnly)
            {
                return client.GetObjectHeader(
                    context.Prm.Address,
                    false,
                    context.Prm.Raw,
                    options).Result;
            }
            if (context.Range is not null)
            {
                var data = client.GetObjectPayloadRangeData(context.Prm.Address,
                    context.Range,
                    context.Prm.Raw,
                    options).Result;
                return new() { Payload = ByteString.CopyFrom(data) };
            }
            return client.GetObject(
                context.Prm.Address,
                context.Prm.Raw,
                options).Result;
        }

        public static FSObject GetObject(this StorageEngine engine, ExecuteContext context)
        {
            if (context.HeadOnly)
            {
                return engine.Head(context.Prm.Address, context.Prm.Raw);
            }
            else if (context.Range is not null)
            {
                return engine.GetRange(context.Prm.Address, context.Range.Offset, context.Range.Length);
            }
            else
            {
                return engine.Get(context.Prm.Address);
            }
        }

        public static bool IsChild(this FSObject obj)
        {
            return obj.Parent != null && obj.Parent.Address == obj.Address;
        }
    }
}
