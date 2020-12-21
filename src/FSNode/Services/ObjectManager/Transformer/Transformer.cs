using Google.Protobuf;
using NeoFS.API.v2.Refs;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static NeoFS.API.v2.Object.Header.Types;
using V2Attribute = NeoFS.API.v2.Object.Header.Types.Attribute;
using V2Object = NeoFS.API.v2.Object.Object;


namespace Neo.Fs.Services.ObjectManager.Transformer
{
    public class PayloadSizeLimiter : IObjectTarget
    {
        private ulong maxSize;
        private ulong written;
        private TargetInitializer targetInit;
        private IObjectTarget target;
        private V2Object current;
        private V2Object parent;
        private PayloadChecksumHasher[] currentHashers;
        private PayloadChecksumHasher[] parentHashers;
        private ObjectID[] previous;

        //private StreamWriter
        private BinaryWriter chunkWriter;
        private Guid splitID;
        private V2Attribute[] parAttrs;

        public PayloadSizeLimiter(ulong maxSz, TargetInitializer targetInitializer)
        {
            this.maxSize = maxSz;
            this.targetInit = targetInitializer;
            this.splitID = Guid.NewGuid();
        }

        public void WriteHeader(V2Object obj)
        {
            this.current = FromObject(obj);
            this.Initialize();
        }

        public int Write(byte[] p)
        {
            this.WriteChunk(p);
            return p.Length;
        }

        public AccessIdentifiers Close()
        {
            return this.Release(true);
        }

        private void Initialize()
        {
            var len = this.previous.Length;
            if (len > 0)
            {
                if (len == 1)
                {
                    this.parent = this.current;
                    this.parent.Header.Split = null; // resetRelations
                    this.parentHashers = this.currentHashers;
                    this.current = this.parent;
                }

                this.current.Header.Split.Previous = this.previous[len - 1];
            }

            this.InitializeCurrent();
        }

        private V2Object FromObject(V2Object obj)
        {
            var res = new V2Object();
            res.Header.ContainerId = obj.Header.ContainerId;
            res.Header.OwnerId = obj.Header.OwnerId;
            res.Header.Attributes.AddRange(obj.Header.Attributes);
            res.Header.ObjectType = obj.Header.ObjectType;

            if (obj.Header.Split.SplitId != null)
                res.Header.Split.SplitId = obj.Header.Split.SplitId;

            return res;
        }

        private void InitializeCurrent()
        {
            // initialize current object target
            this.target = this.targetInit();
            // create payload hashers
            this.currentHashers = PayloadHashersForObject(this.current);

            // TBD, add writer
            // compose multi-writer from target and all payload hashers
        }

        private PayloadChecksumHasher[] PayloadHashersForObject(V2Object obj)
        {
            return new PayloadChecksumHasher[] { }; // TODO, need TzHash dependency
        }

        private AccessIdentifiers Release(bool close)
        {
            // Arg close is true only from Close method.
            // We finalize parent and generate linking objects only if it is more
            // than 1 object in split-chain
            var withParent = close && this.previous.Length > 0;

            if (withParent)
            {
                WriteHashes(this.parentHashers);
                this.parent.Header.PayloadLength = this.written;
                this.current.Header.Split.Parent = this.parent.ObjectId;
            }
            // release current object
            WriteHashes(this.currentHashers);
            // release current
            this.target.WriteHeader(this.current);

            var ids = this.target.Close();
            this.previous = this.previous.Append(ids.Self).ToArray();

            if (withParent)
            {
                this.InitializeLinking();
                this.InitializeCurrent();
                this.Release(false);
            }
            return ids;
        }
        private void WriteHashes(PayloadChecksumHasher[] hashers)
        {
            for (int i = 0; i < hashers.Length; i++)
            {
                hashers[i].ChecksumWriter(hashers[i].Hasher.Hash);
            }
        }

        private void InitializeLinking()
        {
            this.current = FromObject(this.current);
            this.current.Header.Split.Parent = this.parent.ObjectId;
            this.current.Header.Split.Children.AddRange(this.previous);
            this.current.Header.Split.SplitId = ByteString.CopyFrom(this.splitID.ToByteArray());
        }

        private void WriteChunk(byte[] chunk)
        {
            // statement is true if the previous write of bytes reached exactly the boundary.
            if (this.written > 0 && this.written % this.maxSize == 0)
            {
                if (this.written == this.maxSize)
                    this.PrepareFirstChild();

                // need to release current object
                this.Release(false);
                // initialize another object
                this.Initialize();
            }

            ulong len = (ulong)chunk.Length;
            var cut = len;
            var leftToEdge = this.maxSize - this.written % this.maxSize;

            if (len > leftToEdge)
                cut = leftToEdge;

            this.chunkWriter.Write(chunk[..(int)cut]);
            // increase written bytes counter
            this.written += cut;
            // if there are more bytes in buffer we call method again to start filling another object
            if (len > leftToEdge)
                this.WriteChunk(chunk[(int)cut..]);
        }

        private void PrepareFirstChild()
        {
            // initialize split header with split ID on first object in chain
            this.current.Header.Split = new Split(); // InitRelations
            this.current.Header.Split.SplitId = ByteString.CopyFrom(this.splitID.ToByteArray());

            // cut source attributes
            this.parAttrs = this.current.Header.Attributes.ToArray();
            this.current.Header.Attributes.Clear();

            // attributes will be added to parent in detachParent
        }

        private void DetachParent()
        {
            this.parent = this.current;
            this.current = FromObject(this.parent);
            this.parent.Header.Split = null; // reset relations
            this.parentHashers = this.currentHashers;

            this.parent.Header.Attributes.Clear();
            this.parent.Header.Attributes.AddRange(this.parAttrs);
        }
    }

    public delegate void ChecksumWriter(byte[] b);

    public class PayloadChecksumHasher
    {
        //private HashAlgorithm hasher;
        //private ChecksumWriter checksumWriter;

        public HashAlgorithm Hasher { get; set; }
        public ChecksumWriter ChecksumWriter { get; set; }
    }
}
