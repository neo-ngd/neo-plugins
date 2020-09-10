using Google.Protobuf;
using Grpc.Core;
using Neo.ConsoleService;
using System;
using System.IO;
using System.Threading.Tasks;
using NeoFS.API.Object;
using NeoFS.API.Service;
using NeoFS.API.State;
using NeoFS.Crypto;
using NeoFS.Utils;

namespace Neo.Plugins
{
    public partial class FSClient : Plugin
    {
        [ConsoleCommand("object get", Category = "NeoFS Client", Description = "commands for neofs")]
        private async void ObjectGet(string sCID, string sOID, string file_name)
        {
            byte[] cid;
            Guid oid;

            if (sCID == "" || sOID == "")
            {
                Console.WriteLine("Invalid input");
                return;
            }
            var key = Settings.Default.PrivateKey.FromHex().LoadKey();
            try
            {
                oid = Guid.Parse(sOID);
            }
            catch (Exception err)
            {
                Console.WriteLine("wrong oid format: {0}", err.Message);
                return;
            }
            try
            {
                cid = Base58.Decode(sCID);
            }
            catch (Exception err)
            {
                Console.WriteLine("wrong cid format: {0}", err.Message);
                return;
            }
            FileStream file;
            string path;
            try
            {
                path = global::System.IO.Path.GetFullPath(Settings.Default.DefaultFilePath + "/" + file_name);
                if (File.Exists(path)) File.Delete(path);
                file = File.Create(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("can't prepare file: {0}", e.Message);
                return;
            }
            Console.WriteLine($"start get object, destination={path}");

            var channel = new Channel(Settings.Default.Host, ChannelCredentials.Insecure);

            channel.UsedHost().GetHealth(SingleForwardedTTL, key, false).Say();

            var req = new GetRequest
            {
                Address = new NeoFS.API.Refs.Address
                {
                    CID = ByteString.CopyFrom(cid),
                    ObjectID = ByteString.CopyFrom(oid.Bytes()),
                },
            };

            req.SetTTL(SingleForwardedTTL);
            req.SignHeader(key, false);

            var client = new Service.ServiceClient(channel);

            using var call = client.Get(req);
            ProgressBar progress = null;

            double len = 0;
            double off = 0;

            while (await call.ResponseStream.MoveNext())
            {
                var res = call.ResponseStream.Current;


                if (res.Object != null)
                {
                    len = (double)res.Object.SystemHeader.PayloadLength;

                    Console.WriteLine("Received object");
                    Console.WriteLine("PayloadLength = {0}", len);

                    Console.WriteLine("Headers:");
                    for (var i = 0; i < res.Object.Headers.Count; i++)
                    {
                        Console.WriteLine(res.Object.Headers[i]);
                    }


                    if (res.Object.Payload.Length > 0)
                    {
                        off += (double)res.Object.Payload.Length;
                        res.Object.Payload.WriteTo(file);
                    }

                    Console.Write("Receive chunks: ");
                    progress = new ProgressBar();
                }
                else if (res.Chunk != null && res.Chunk.Length > 0)
                {
                    //Console.Write("#");
                    off += res.Chunk.Length;

                    res.Chunk.WriteTo(file);

                    if (progress != null)
                    {

                        progress.Report(off / len);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));

            if (progress != null)
            {
                progress.Dispose();
            }

            Console.Write("Done!");

            Console.WriteLine();
        }
    }
}
