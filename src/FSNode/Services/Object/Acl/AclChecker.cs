using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Cryptography;
using NeoFS.API.v2.Refs;
using NeoFS.API.v2.Session;
using Neo.Fs.LocalObjectStorage.LocalStore;
using Neo.Fs.Core.Container;
using Neo.Fs.Services.Object.Acl.EAcl;

namespace Neo.Fs.Services.Object.Acl
{
    public class AclChecker
    {
        private readonly IContainerSource containerSource;
        private readonly Storage localStorage;
        private readonly EAclValidator eAclValidator;
        private IRequest request;
        private RequestInfo requestInfo;
        private Operation op;

        public AclChecker(IContainerSource cs, Storage local_storage, IEAclStorage storage)
        {
            containerSource = cs;
            localStorage = local_storage;
            eAclValidator = new EAclValidator(storage);
        }

        public void LoadRequest(IRequest request, Operation op)
        {
            this.request = request;
            this.op = op;
            requestInfo = new RequestInfo(request, op, containerSource);
            requestInfo.Prepare();
        }

        public bool BasicAclCheck()
        {
            return requestInfo.Role switch
            {
                Role.User => requestInfo.BasicAcl.UserAllowed(op),
                Role.System => requestInfo.BasicAcl.SystemAllowed(op),
                Role.Others => requestInfo.BasicAcl.OthersAllowed(op),
                _ => false,
            };
        }

        public bool EAclCheck(object message)
        {
            if (requestInfo.BasicAcl.Final()) return true;
            if (!requestInfo.BasicAcl.BearsAllowed(op)) return false;
            if (!requestInfo.IsValidBearer()) return false;
            var unit = new ValidateUnit
            {
                Cid = requestInfo.Cid,
                Role = requestInfo.Role,
                Op = requestInfo.Op,
                Bearer = requestInfo.Bearer,
                HeaderSource = new HeaderSource(localStorage, message),
            };
            var action = eAclValidator.CalculateAction(unit);
            return Action.Allow == action;
        }

        public bool StickyBitCheck(OwnerID owner)
        {
            if (owner is null || requestInfo.SenderKey is null || requestInfo.SenderKey.Length == 0)
                return false;
            if (!requestInfo.BasicAcl.Sticky())
                return false;
            return requestInfo.SenderKey.PublicKeyToOwnerID().Value == owner.Value;
        }
    }
}