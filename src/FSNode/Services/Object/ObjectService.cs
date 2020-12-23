using Google.Protobuf;
using Grpc.Core;
using NeoFS.API.v2.Acl;
using NeoFS.API.v2.Cryptography;
using NeoFS.API.v2.Object;
using NeoFS.API.v2.Session;
using V2Object = NeoFS.API.v2.Object.Object;
using Neo.Fs.LocalObjectStorage.LocalStore;
using Neo.Fs.Core.Container;
using Neo.Fs.Services.Object.Acl;
using Neo.Fs.Services.Object.Delete;
using Neo.Fs.Services.Object.Get;
using Neo.Fs.Services.Object.Head;
using Neo.Fs.Services.Object.Put;
using Neo.Fs.Services.Object.Range;
using Neo.Fs.Services.Object.RangeHash;
using Neo.Fs.Services.Object.Search;
using Neo.Fs.Services.Object.Sign;
using System;
using System.Linq;
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
        private readonly DeleteService deleteService;
        private readonly GetService getService;
        private readonly HeadService headService;
        private readonly PutService putService;
        private readonly RangeService rangeService;
        private readonly RangeHashService rangeHashService;
        private readonly SearchService searchService;

        public ObjectServiceImpl(IContainerSource container_source, Storage local_storage, IEAclStorage eacl_storage)
        {
            localStorage = local_storage;
            eAclStorage = eacl_storage;
            contnainerSource = container_source;
            signer = new Signer();
            deleteService = new DeleteService();
            getService = new GetService();
            headService = new HeadService(new RelationSearcher());
            putService = new PutService();
            rangeService = new RangeService();
            rangeHashService = new RangeHashService();
            searchService = new SearchService();
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
            return Task.Run(() =>
            {
                var prm = GetPrm.FromRequest(request);
                var obj = getService.Get(prm);
                var resp = new GetResponse
                {
                    Body = new GetResponse.Types.Body
                    {
                        Init = new GetResponse.Types.Body.Types.Init
                        {
                            Header = obj.Header,
                            ObjectId = obj.ObjectId,
                            Signature = obj.Signature,
                        }
                    }
                };
                resp.SignResponse(key);
                responseStream.WriteAsync(resp);
                var payload = obj.Payload.ToByteArray();
                int offset = 0;
                while (offset < obj.Payload.Length)
                {
                    var end = offset + V2Object.ChunkSize;
                    if (payload.Length < end) end = payload.Length;
                    resp = new GetResponse
                    {
                        Body = new GetResponse.Types.Body
                        {
                            Chunk = ByteString.CopyFrom(payload[offset..end]),
                        }
                    };
                    offset = end;
                    resp.SignResponse(key);
                    responseStream.WriteAsync(resp);
                }
            });
        }

        public override async Task<PutResponse> Put(IAsyncStreamReader<PutRequest> requestStream, ServerCallContext context)
        {
            var init_received = false;
            var payload = new byte[0];
            IPutTarget target = null;
            while (await requestStream.MoveNext())
            {
                var request = requestStream.Current;
                if (!init_received)
                {
                    if (request.Body.ObjectPartCase != PutRequest.Types.Body.ObjectPartOneofCase.Init)
                        new RpcException(new Status(StatusCode.DataLoss, " missing init"));
                    var init = request.Body.Init;
                    if (!init.VerifyRequest()) throw new RpcException(new Status(StatusCode.Unauthenticated, " verify header failed"));
                    var put_init_prm = PutInitPrm.FromRequest(request);
                    try
                    {
                        target = putService.Init(put_init_prm);
                    }
                    catch (Exception e)
                    {
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, e.Message));
                    }

                }
                else
                {
                    if (request.Body.ObjectPartCase != PutRequest.Types.Body.ObjectPartOneofCase.Chunk)
                        new RpcException(new Status(StatusCode.DataLoss, " missing chunk"));
                    var chunk = request.Body.Chunk;
                    payload = payload.Concat(chunk).ToArray();
                }
            }
            try
            {
                if (target is null) throw new RpcException(new Status(StatusCode.DataLoss, "init missing"));
                var result = target.PutPayload(ByteString.CopyFrom(payload));
                var resp = new PutResponse
                {
                    Body = new PutResponse.Types.Body
                    {
                        ObjectId = result.Current,
                    }
                };
                return resp;
            }
            catch (Exception e)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
            }
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
            return Task.Run(() =>
            {
                var prm = DeletePrm.FromRequest(request);
                var result = deleteService.Delete(prm);
                var resp = new DeleteResponse
                {
                    Body = new DeleteResponse.Types.Body { }
                };
                resp.SignResponse(key);
                return resp;
            });
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
            var obj = headService.Head(head_prm);
            var resp = obj.Header.ToHeadResponse(head_prm.Short);
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
            var prm = RangePrm.FromRequest(request);
            var head_result = rangeService.Range(prm);
            var resp = new GetRangeResponse
            {
                Body = new GetRangeResponse.Types.Body
                {
                    Chunk = head_result.Chunk,
                }
            };
            resp.SignResponse(key);
            return responseStream.WriteAsync(resp);
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
            return Task.Run(() =>
            {
                var prm = RangeHashPrm.FromRequest(request);
                var result = rangeHashService.RangeHash(prm);
                var resp = new GetRangeHashResponse
                {
                    Body = new GetRangeHashResponse.Types.Body { }
                };
                resp.SignResponse(key);
                return resp;
            });
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
            return Task.Run(() =>
            {
                var prm = SearchPrm.FromRequest(request);
                var oids = searchService.Search(prm);
                var resp = new SearchResponse
                {
                    Body = new SearchResponse.Types.Body { }
                };
                resp.Body.IdList.AddRange(oids);
                resp.SignResponse(key);
                responseStream.WriteAsync(resp);
            });
        }
    }
}
