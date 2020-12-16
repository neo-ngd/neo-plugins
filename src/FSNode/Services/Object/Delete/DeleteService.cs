using Google.Protobuf;
using NeoFS.API.v2.Refs;
using V2Object = NeoFS.API.v2.Object;
using Neo.Fs.Services.Object.Head;
using Neo.Fs.Services.Object.Put;
using System.Collections.Generic;

namespace Neo.Fs.Services.Object.Delete
{
    public class DeleteService
    {
        private readonly OwnerID selfId;
        private readonly IRelationHeader relationHeader;
        private readonly HeadService headService;
        private readonly PutService putService;

        public bool Delete(DeletePrm prm)
        {
            var owner = prm.Token?.Body.OwnerId ?? selfId;
            if (owner is null) return false;
            var addrs = GetRelations(prm);
            var obj = new V2Object.Object
            {
                Header = new V2Object.Header
                {
                    ContainerId = prm.Address.ContainerId,
                    OwnerId = owner,
                    ObjectType = V2Object.ObjectType.Tombstone,
                },
                Payload = ByteString.CopyFrom(addrs.ToRepeatedField().ToByteArray()),
            };
            putService.Put(obj);
            return true;
        }

        private List<Address> GetRelations(DeletePrm prm)
        {
            var res = new List<Address>();
            var linking = relationHeader.HeadRelation(prm.Address, prm);
            if (linking != null)
            {
                var cid = prm.Address.ContainerId;
                var prev = prm.Address.ObjectId;
                while (prev != prm.Address.ObjectId)
                {
                    var address = new Address
                    {
                        ObjectId = prev,
                        ContainerId = cid,
                    };
                    var head_prm = new HeadPrm();
                    head_prm.WithCommonPrm(prm);
                    head_prm.Address = address;
                    var head_result = headService.Head(head_prm);
                    var oid = head_result.Header.ObjectId;
                    prev = head_result.Header.Parent().ObjectId;
                    if (head_result.RightChild != null)
                    {
                        oid = head_result.RightChild.ObjectId;
                        prev = head_result.RightChild.Parent().ObjectId;
                    }
                    address.ObjectId = oid;
                    res.Add(address);
                }
            }
            else
            {
                var child_list = linking.Header.Split.Children;
                foreach (var oid in child_list)
                    res.Add(new Address
                    {
                        ContainerId = prm.Address.ContainerId,
                        ObjectId = oid,
                    });
                res.Add(new Address
                {
                    ContainerId = prm.Address.ContainerId,
                    ObjectId = linking.ObjectId,
                });
            }
            res.Add(prm.Address);
            return res;
        }
    }
}