
using System;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Network.Cache;
using Neo.FileStorage.Services.Reputaion.Local.Storage;
using static Neo.Utility;
using MorphClient = Neo.FileStorage.Morph.Invoker.Client;

namespace Neo.FileStorage.Services.Reputaion.Local.Client
{
    public class ReputationClientCache
    {
        public StorageService StorageNode { get; init; }
        public ClientCache BasicCache { get; init; }
        public MorphClient MorphClient { get; init; }
        public TrustStorage ReputationStorage { get; init; }

        public ReputationClient Get(Network.Address address)
        {
            var client = BasicCache.Get(address);
            try
            {
                var nm = MorphContractInvoker.InvokeSnapshot(MorphClient, 0);
                foreach (var n in nm.Nodes)
                {
                    var ipaddr = Network.Address.FromString(n.NetworkAddress);
                    if (ipaddr == address)
                    {
                        UpdatePrm prm = new(new(n.PublicKey));
                        return new()
                        {
                            ClientCache = this,
                            FSClient = client,
                            Prm = prm,
                        };
                    }
                }
            }
            catch (Exception e)
            {
                Log(nameof(ReputationClientCache), LogLevel.Debug, e.Message);
            }
            return new()
            {
                FSClient = client,
                Prm = null,
            };
        }
    }
}
