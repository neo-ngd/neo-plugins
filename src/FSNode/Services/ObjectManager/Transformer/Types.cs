using NeoFS.API.v2.Refs;
using V2Object = NeoFS.API.v2.Object.Object;

namespace Neo.Fs.Services.ObjectManager.Transformer
{
    public class AccessIdentifiers
    {
        private ObjectID par;
        private ObjectID self;

        public ObjectID Par { get => this.par; }
        public ObjectID Self { get => this.self; }

        public AccessIdentifiers(ObjectID s, ObjectID p)
        {
            this.self = s;
            this.par = p;
        }
    }

    public interface IObjectTarget 
    {
        void WriteHeader(V2Object obj);
        AccessIdentifiers Close();
    }

    public delegate IObjectTarget TargetInitializer();
}
