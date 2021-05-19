using System;
using Neo.FileStorage.Services.Reputaion.EigenTrust;
using Neo.FileStorage.Services.Reputaion.EigenTrust.Storage.Consumers;
using Neo.FileStorage.Services.Reputaion.EigenTrust.Storage.Daughters;

namespace Neo.FileStorage.Services.Reputaion.Intermediate
{
    public class DaughterTrustIteratorProvider
    {
        public ConsumersStorage ConsumerStorage { get; init; }
        public DaughtersStorage DaughterStorage { get; init; }

        public DaughterTrusts InitDaughterIterator(IterationContext context, PeerID peer)
        {
            if (DaughterStorage.DaughterTrusts(context.Epoch, peer, out DaughterTrusts storage))
                return storage;
            throw new InvalidOperationException();
        }

        public DaughterStorage InitAllDaughtersIterator(IterationContext context)
        {
            if (DaughterStorage.AllDaughterTrusts(context.Epoch, out DaughterStorage storage))
                return storage;
            throw new InvalidOperationException();
        }

        public ConsumerStorage InitConsumerIterator(IterationContext context)
        {
            if (ConsumerStorage.Consumers(context.Epoch, context.Index, out ConsumerStorage storage))
                return storage;
            throw new InvalidOperationException();
        }
    }
}