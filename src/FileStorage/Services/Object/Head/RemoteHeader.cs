using System;
using System.Threading;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.Services.Object.Util;
using Neo.FileStorage.Services.Reputaion.Local.Client;
using FSObject = Neo.FileStorage.API.Object.Object;

namespace Neo.FileStorage.Services.Object.Head
{
    public class RemoteHeader
    {
        public const int DefaultHeadTtl = 1;
        public KeyStorage KeyStorage { get; init; }
        public ReputationClientCache ClientCache { get; init; }

        public FSObject Head(RemoteHeadPrm prm, CancellationToken context)
        {
            var key = KeyStorage.GetKey(prm.SessionToken);
            if (key is null)
                throw new InvalidOperationException($"{nameof(RemoteHeader)} could not receive private key");
            var addr = prm.Node.ToIPAddressString();
            var client = ClientCache.Get(addr);
            if (client is null)
                throw new InvalidOperationException($"{nameof(RemoteHeader)} could not create SDK client {addr}");
            var header = client.GetObjectHeader(prm.Address, prm.Short, prm.Raw, new CallOptions
            {
                Ttl = DefaultHeadTtl,
                Session = prm.SessionToken,
                Bearer = prm.BearerToken,
                Key = key,
            }, context).Result;
            if (header is null)
                throw new InvalidOperationException(nameof(RemoteHeader) + $" could not read object payload range from {addr}");
            return header;
        }
    }
}
