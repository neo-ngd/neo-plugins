using Neo.Fs.Core.Netmap;
using NeoFS.API.v2.Refs;
using NeoFS.API.v2.Session;
using System.Security.Cryptography;
using V2Object = NeoFS.API.v2.Object.Object;

namespace Neo.Fs.Services.ObjectManager.Transformer
{
    public class Formatter : IObjectTarget
    {
        private FormatterParams prm;
        private V2Object obj;
        private ulong sz;

        public Formatter(FormatterParams p)
        {
            this.prm = p;
        }

        public void WriteHeader(V2Object obj)
        {
            this.obj = obj;
        }

        public AccessIdentifiers Close()
        {
            var curEpoch = this.prm.NetworkState.CurrentEpoch();

            this.obj.Header.Version = Version.SDKVersion();
            this.obj.Header.PayloadLength = this.sz;
            this.obj.Header.SessionToken = this.prm.SessionToken;
            this.obj.Header.CreationEpoch = curEpoch;

            ObjectID parId = null;
            V2Object par;
            if (this.obj.Header.Split.Parent != null)
            {
                par = new V2Object()
                {
                    ObjectId = this.obj.Header.Split.Parent,
                    Signature = this.obj.Header.Split.ParentSignature,
                    Header = this.obj.Header.Split.ParentHeader
                };
                var rawPar = new V2Object(par);
                rawPar.Header.SessionToken = this.prm.SessionToken;
                rawPar.Header.CreationEpoch = curEpoch;

                var sig = rawPar.CalculateIDSignature(this.prm.Key); // TBD, 
                rawPar.Signature = sig;
                parId = rawPar.ObjectId;
                this.obj.Header.Split.Parent = parId;
            }

            var signature = this.obj.CalculateIDSignature(this.prm.Key);
            this.obj.Signature = signature;

            this.prm.NextTarget.WriteHeader(this.obj);
            this.prm.NextTarget.Close();

            return new AccessIdentifiers(this.obj.ObjectId, parId);
        }
    }

    public class FormatterParams
    {
        public ECDsa Key { get; set; } // private key
        public IObjectTarget NextTarget { get; set; }
        public SessionToken SessionToken { get; set; }
        public IState NetworkState { get; set; }
    }
}
