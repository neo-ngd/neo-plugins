using V2Object = NeoFS.API.v2.Object.Object;
using NeoFS.API.v2.Refs;
using Neo.Fs.Core.Container;
using Neo.Fs.Core.Netmap;
using Neo.Fs.Core.Object;
using Neo.Fs.Services.Object.Util;
using System;

namespace Neo.Fs.Services.Object.Put
{
    public class PutService
    {
        private INetmapSource netmapSource;
        private IContainerSource containerSource;
        private IMaxSizeSource maxSizeSource;
        private FormatValidator objectValidator;
        private KeyStorage keyStorage;

        public ObjectID Put(V2Object obj)
        {
            return new ObjectID();
        }

        public IPutTarget Init(PutInitPrm prm)
        {
            var target = InitTarget(prm);
            target.PutInit(prm.Init);
            return target;
        }

        private IPutTarget InitTarget(PutInitPrm prm)
        {
            PreparePrm(prm);
            var session_token = prm.SessionToken;
            if (session_token is null)
            {
                return new DistributeTarget(objectValidator);
            }
            var session_key = keyStorage.GetKey(session_token);
            if (session_key is null)
                throw new InvalidOperationException(nameof(InitTarget) + " could not get session key");
            var max_size = maxSizeSource.MaxObjectSize();
            if (max_size == 0)
                throw new InvalidOperationException(nameof(InitTarget) + " could not obtain max object size parameter");
            return new DistributeTarget(objectValidator);
        }

        private void PreparePrm(PutInitPrm prm)
        {
            var nm = netmapSource.GetLatestNetworkMap();
            if (nm is null)
                throw new InvalidOperationException(nameof(PreparePrm) + " could not get latest netmap");
            var container = containerSource.Get(prm.Init.Header.ContainerId);
            if (container is null)
                throw new InvalidOperationException(nameof(PreparePrm) + " could not get container by cid");
            //TODO: prepare travers option
        }
    }
}
