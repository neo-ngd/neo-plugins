using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Session;
using Neo.Fs.Core.Container;
using Neo.Fs.Services.Object.Acl.EAcl;

namespace Neo.Fs.Services.Object.Acl
{
    public class AclChecker
    {
        private readonly IContainerSource ContainerSource;
        private readonly EAclValidator eAcl;
        private IRequest request;
        private RequestInfo requestInfo;
        private Operation op;

        public AclChecker(IContainerSource cs)
        {
            ContainerSource = cs;
            eAcl = new EAclValidator();
        }

        public void LoadRequest(IRequest request, Operation op)
        {
            this.request = request;
            this.op = op;
            requestInfo = new RequestInfo(request, op, ContainerSource);
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

        public bool EAclCheck()
        {
            if (requestInfo.BasicAcl.Final()) return true;
            if (!requestInfo.BasicAcl.BearsAllowed(op)) return false;
            return true;
        }
    }
}