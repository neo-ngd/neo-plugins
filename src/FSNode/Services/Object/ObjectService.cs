using Grpc.Core;
using NeoFS.API.v2.Object;
using V2Object = NeoFS.API.v2.Object;
using Neo.Fs.Core.Container;
using Neo.Fs.Services.Object.Acl;
using Neo.Fs.Services.Object.Sign;
using System.Threading.Tasks;

namespace Neo.Fs.Services.Object
{
    public partial class ObjectServiceImpl : ObjectService.ObjectServiceBase
    {
        private readonly AclChecker acl;
        private readonly Signer signer;

        public ObjectServiceImpl(IContainerSource cs)
        {
            acl = new AclChecker(cs);
            signer = new Signer();
        }

        public override Task Get(GetRequest request, IServerStreamWriter<GetResponse> responseStream, ServerCallContext context)
        {
            base.Get(request, responseStream, context);
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<PutResponse> Put(IAsyncStreamReader<PutRequest> requestStream, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<DeleteResponse> Delete(DeleteRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<HeadResponse> Head(HeadRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task GetRange(GetRangeRequest request, IServerStreamWriter<GetRangeResponse> responseStream, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task<GetRangeHashResponse> GetRangeHash(GetRangeHashRequest request, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }

        public override Task Search(SearchRequest request, IServerStreamWriter<SearchResponse> responseStream, ServerCallContext context)
        {
            throw new RpcException(new Status(StatusCode.Unimplemented, ""));
        }
    }
}
