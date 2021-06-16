
using System;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Cache;
using Neo.FileStorage.Morph.Invoker;

namespace Neo.FileStorage.Services.Object.Acl
{
    public class EAclCache
    {
        public const int DefaultCacheSize = 100;
        public const int DefaultTTLMilliseconds = 30000;

        private readonly TTLNetworkCache<ContainerID, EACLTable> cache;

        public EAclCache(Client client, int size = DefaultCacheSize, int ttl = DefaultTTLMilliseconds)
        {
            cache = new(size, TimeSpan.FromMilliseconds(ttl), cid =>
             {
                 var result = MorphContractInvoker.GetEACL(client, cid);
                 if (!result.Signature.VerifyMessagePart(result.Table))
                     throw new InvalidOperationException("incorrect signature of eacl from morph client");
                 return result.Table;
             });
        }

        public EACLTable GetEACL(ContainerID cid)
        {
            return cache.Get(cid);
        }

    }
}
