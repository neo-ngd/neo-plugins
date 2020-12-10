using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Cryptography;
using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using NeoFS.API.v2.Session;
using V2Cotainer = NeoFS.API.v2.Container;
using V2Acl = NeoFS.API.v2.Acl;
using V2Object = NeoFS.API.v2.Object;
using Neo.Fs.Core.Container;
using Neo.Fs.Core.Netmap;
using System;
using System.Linq;

namespace Neo.Fs.Services.Object.Acl
{
    public class RequestInfo
    {
        public readonly IInnerRingFetcher InnerRing;
        public readonly INetmapSource NetmapSource;
        private readonly IContainerSource ContainerSource;
        private readonly IRequest Request;
        private readonly NeoFS.API.v2.Acl.Operation Op;
        private bool Prepared;

        public uint BasicAcl;
        public Role Role;
        public OwnerID Owner;
        public V2Cotainer.Container Cnr;
        public ContainerID Cid;
        public byte[] SenderKey;

        public RequestInfo(IRequest request, NeoFS.API.v2.Acl.Operation op, IContainerSource container_source)
        {
            Request = request;
            Op = op;
            ContainerSource = container_source;
            Role = Role.Unspecified;
        }

        public void Prepare()
        {
            if (Prepared) return;
            Prepared = true;
            Cid = GetContainerIDFromRequest(Request);
            if (Cid is null) throw new InvalidOperationException(nameof(Prepare) + " no container id");
            Cnr = ContainerSource.Get(Cid);
            if (Cnr is null || Cnr.OwnerId is null) throw new InvalidOperationException(nameof(Prepare) + " unkown container");
            Classify();
            if (Role == Role.Unspecified)
                throw new InvalidOperationException(nameof(Prepare) + " unkown role");
            BasicAcl = Cnr.BasicAcl;
        }

        public ContainerID GetContainerIDFromRequest(IRequest req)
        {
            switch (req)
            {
                case V2Object.GetRequest getReq:
                    return getReq.Body.Address.ContainerId;
                case V2Object.PutRequest putReq:
                    var obj = putReq.Body.Init;
                    if (obj is null) throw new InvalidOperationException(nameof(GetContainerIDFromRequest) + " cannt get cid from chunk");
                    return obj.Header.ContainerId;
                case V2Object.HeadRequest headReq:
                    return headReq.Body.Address.ContainerId;
                case V2Object.SearchRequest searchReq:
                    return searchReq.Body.ContainerId;
                case V2Object.DeleteRequest deleteReq:
                    return deleteReq.Body.Address.ContainerId;
                case V2Object.GetRangeRequest rangeReq:
                    return rangeReq.Body.Address.ContainerId;
                case V2Object.GetRangeHashRequest rangeHashReq:
                    return rangeHashReq.Body.Address.ContainerId;
                default:
                    throw new FormatException(nameof(GetContainerIDFromRequest) + " unknown request type");
            }
        }

        private void Classify()
        {
            RequestOwner();
            if (Owner == Cnr.OwnerId) Role = Role.User;
            if (IsInnerRingKey(SenderKey)) Role = Role.System;
            var is_cnr_node = IsContainerKey(SenderKey);
            if (is_cnr_node) Role = Role.System;
            Role = Role.Others;
        }

        private void RequestOwner()
        {
            if (Request.VerifyHeader is null)
                throw new ArgumentNullException(nameof(RequestOwner) + " no verification header");
            if (Request.MetaHeader?.SessionToken?.Body != null)
            {
                OwnerFromToken();
                return;
            }
            var body_sig = OriginalBodySignature();
            if (body_sig is null) throw new InvalidOperationException(nameof(RequestOwner) + " no body signature");
            SenderKey = body_sig.Key.ToByteArray();
            Owner = body_sig.Key.ToByteArray().PublicKeyToOwnerID();
        }

        private void OwnerFromToken()
        {
            SessionToken token = Request.MetaHeader.SessionToken;
            if (!token.Body.VerifyMessagePart(token.Signature)) throw new InvalidOperationException(nameof(OwnerFromToken) + " verify failed");
            var tokenIssueKey = token.Signature.Key.ToByteArray();
            var tokenOwner = token.Body.OwnerId;
            if (tokenIssueKey.PublicKeyToOwnerID() != tokenOwner) throw new InvalidOperationException(nameof(OwnerFromToken) + " OwnerID and key not equal");
            SenderKey = tokenIssueKey;
            Owner = tokenOwner;
        }

        private Signature OriginalBodySignature()
        {
            var verification = Request.VerifyHeader;
            if (verification is null) return null;
            if (verification.Origin != null) verification = verification.Origin;
            return verification.BodySignature;
        }

        private bool IsInnerRingKey(byte[] key)
        {
            var inner_ring_keys = InnerRing.InnerRingKeys();
            foreach (var k in inner_ring_keys)
                if (k.SequenceEqual(key)) return true;
            return false;
        }

        private bool IsContainerKey(byte[] key)
        {
            try
            {
                var nm = NetmapSource.GetLatestNetworkMap();
                var is_in = LookUpKeyInContainer(nm, key, Cid, Cnr);
                if (is_in) return true;
                nm = NetmapSource.GetPreviousNetworkMap();
                return LookUpKeyInContainer(nm, key, Cid, Cnr);
            }
            catch
            {
                return false;
            }
        }

        private bool LookUpKeyInContainer(NetMap nm, byte[] key, ContainerID cid, V2Cotainer.Container container)
        {
            var nodes = nm.GetContainerNodes(Cnr.PlacementPolicy, cid.Value.ToByteArray());
            if (nodes is null) throw new InvalidOperationException(nameof(LookUpKeyInContainer) + " cannt get container nodes");
            var ns = nodes.Flatten();
            foreach (var n in ns)
                if (n.PublicKey.SequenceEqual(key)) return true;
            return false;
        }

        private V2Acl.Action CalculateAction()
        {

        }
    }
}
