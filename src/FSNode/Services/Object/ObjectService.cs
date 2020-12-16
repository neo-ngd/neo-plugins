using Grpc.Core;
using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Cryptography;
using NeoFS.API.v2.Object;
using NeoFS.API.v2.Session;
using V2Object = NeoFS.API.v2.Object;
using Neo.Fs.LocalObjectStorage.LocalStore;
using Neo.Fs.Core.Container;
using Neo.Fs.Services.Object.Acl;
using Neo.Fs.Services.Object.Head;
using Neo.Fs.Services.Object.Search;
using Neo.Fs.Services.Object.Sign;
using Neo.Fs.Services.Object.Util;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Neo.Fs.Services.Object
{
    public partial class ObjectServiceImpl : ObjectService.ObjectServiceBase
    {
        private readonly ECDsa key;
        private readonly Storage localStorage;
        private readonly IContainerSource contnainerSource;
        private readonly IEAclStorage eAclStorage;
        private readonly Signer signer;
        private readonly HeadService headService;

        public ObjectServiceImpl(IContainerSource container_source, Storage local_storage, IEAclStorage eacl_storage)
        {
            localStorage = local_storage;
            eAclStorage = eacl_storage;
            contnainerSource = container_source;
            signer = new Signer();
            headService = new HeadService(new RelationSearcher());
        }

        public override Task Get(GetRequest request, IServerStreamWriter<GetResponse> responseStream, ServerCallContext context)
        {
            var acl = new AclChecker(contnainerSource, localStorage, eAclStorage);

            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Get) + " basic acl failed.");
                if (!acl.EAclCheck(request)) throw new InvalidOperationException(nameof(Get) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            if (!request.VerifyRequest()) throw new RpcException(new Status(StatusCode.Unauthenticated, "verify header failed"));
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<PutResponse> Put(IAsyncStreamReader<PutRequest> requestStream, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            var acl = new AclChecker(contnainerSource, localStorage, eAclStorage);
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck(request)) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<HeadResponse> Head(HeadRequest request, ServerCallContext context)
        {
            var acl = new AclChecker(contnainerSource, localStorage, eAclStorage);
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " request basic acl failed.");
                if (!acl.EAclCheck(request)) throw new InvalidOperationException(nameof(Head) + " request extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            if (!request.VerifyRequest()) throw new RpcException(new Status(StatusCode.Unauthenticated, " verify failed"));
            var head_prm = HeadPrm.FromRequest(request);
            head_prm.WithCommonPrm(CommonPrm.FromRequest(request));
            var obj = headService.Head(head_prm);
            var resp = obj.ToHeadResponse(head_prm.Short);
            resp.SignResponse(key);
            if (!acl.EAclCheck(resp)) throw new InvalidOperationException(nameof(Head) + " response extend basic acl failed.");
            return Task.FromResult(resp);
        }


        public override Task GetRange(GetRangeRequest request, IServerStreamWriter<GetRangeResponse> responseStream, ServerCallContext context)
        {
            var acl = new AclChecker(contnainerSource, localStorage, eAclStorage);
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck(request)) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<GetRangeHashResponse> GetRangeHash(GetRangeHashRequest request, ServerCallContext context)
        {
            var acl = new AclChecker(contnainerSource, localStorage, eAclStorage);
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck(request)) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task Search(SearchRequest request, IServerStreamWriter<SearchResponse> responseStream, ServerCallContext context)
        {
            var acl = new AclChecker(contnainerSource, localStorage, eAclStorage);
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck(request)) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }
    }
}
