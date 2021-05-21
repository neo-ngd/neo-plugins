using System.Security.Cryptography;
using Google.Protobuf;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Reputation;
using Neo.FileStorage.Morph.Invoker;
using Neo.FileStorage.Services.Reputaion.EigenTrust;

namespace Neo.FileStorage.Services.Reputaion.Intermediate
{
    public class FinalWriter
    {
        public ECDsa PrivateKey { get; init; }
        public byte[] PublicKey { get; init; }
        public Client MorphClient { get; init; }

        public void WriteIntermediateTrust(IterationTrust it)
        {
            GlobalTrust gt = new()
            {
                Body = new()
                {
                    Manager = new()
                    {
                        PublicKey = ByteString.CopyFrom(PublicKey),
                    },
                    Trust = new()
                    {
                        Peer = new()
                        {
                            PublicKey = ByteString.CopyFrom(it.Trust.Peer.ToByteArray()),
                        },
                        Value = it.Trust.Value,
                    }
                }
            };
            gt.Signature = PrivateKey.SignMessagePart(gt.Body);
            MorphClient.InvokeReputationPut(it.Epoch, it.Trust.Peer.ToByteArray(), gt.ToByteArray());
        }
    }
}
