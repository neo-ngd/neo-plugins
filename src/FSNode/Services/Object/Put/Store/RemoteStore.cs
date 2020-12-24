using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Session;
using V2Object = NeoFS.API.v2.Object.Object;
using Neo.FSNode.Network.Cache;
using Neo.FSNode.Services.Object.Util;
using System;

namespace Neo.FSNode.Services.Object.Put.Store
{
    public class RemoteStore : IStore
    {
        private readonly KeyStorage keyStorage;
        private readonly ClientCache clientCache;
        private readonly Network.Address node;
        private readonly SessionToken sessionToken;
        private readonly BearerToken bearerToken;

        public void Put(V2Object obj)
        {
            var key = keyStorage.GetKey(sessionToken);
            if (key is null)
                throw new InvalidOperationException(nameof(Range) + " could not receive private key");
            var addr = node.IPAddressString();
            var client = clientCache.GetClient(key, addr);
            if (client is null)
                throw new InvalidOperationException(nameof(Range) + $" could not create SDK client {addr}");
            var oid = client.PutObject(obj, new NeoFS.API.v2.Client.CallOptions
            {
                Ttl = 1,
                Session = sessionToken,
                Bearer = bearerToken,
            }).Result;
            if (oid is null)
                throw new InvalidOperationException(nameof(Range) + $" could not read object payload range from {addr}");
        }
    }
}
