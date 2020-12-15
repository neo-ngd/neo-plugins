using Grpc.Core;
using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Object;
using V2Object = NeoFS.API.v2.Object;
using Neo.Fs.LocalObjectStorage.LocalStore;
using Neo.Fs.Core.Container;
using Neo.Fs.Services.Object.Acl;
using Neo.Fs.Services.Object.Sign;
using System;
using System.Threading.Tasks;

namespace Neo.Fs.Services.Object
{
    public partial class ObjectServiceImpl : ObjectService.ObjectServiceBase
    {
        private readonly Storage localStorage;
        private readonly IContainerSource contnainerSource;
        private readonly IEAclStorage eAclStorage;
        private readonly Signer signer;

        public ObjectServiceImpl(IContainerSource container_source, Storage local_storage, IEAclStorage eacl_storage)
        {
            localStorage = local_storage;
            eAclStorage = eacl_storage;
            contnainerSource = container_source;
            signer = new Signer();
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
            //Verify
            //Get
            //Sign
            //Send
        }

        public override Task<PutResponse> Put(IAsyncStreamReader<PutRequest> requestStream, ServerCallContext context)
        {
            //Put
            //Sign
            //Send
        }

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck()) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            //Verify
            //Delete
            //Sign
            //Send
        }

        public override Task<HeadResponse> Head(HeadRequest request, ServerCallContext context)
        {
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck()) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
            //Head
            //Sign
            //Send
        }

        public override Task GetRange(GetRangeRequest request, IServerStreamWriter<GetRangeResponse> responseStream, ServerCallContext context)
        {
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck()) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
        }

        public override Task<GetRangeHashResponse> GetRangeHash(GetRangeHashRequest request, ServerCallContext context)
        {
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck()) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
        }

        public override Task Search(SearchRequest request, IServerStreamWriter<SearchResponse> responseStream, ServerCallContext context)
        {
            try
            {
                acl.LoadRequest(request, Operation.Get);
                if (!acl.BasicAclCheck()) throw new InvalidOperationException(nameof(Head) + " basic acl failed.");
                if (!acl.EAclCheck()) throw new InvalidOperationException(nameof(Head) + " extend basic acl failed.");
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, e.Message));
            }
        }
    }
}
