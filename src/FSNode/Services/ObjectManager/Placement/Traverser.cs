using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System;
using System.Linq;
using V2 = NeoFS.API.v2.Container;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public interface IBuilder
    {
        Node[][] BuildPlacement(Address address, PlacementPolicy pp);
    }

    public delegate void Option(Cfg cfg);

    public class Cfg
    {
        public static Cfg DefaultCfg = new Cfg() { Addr = new Address() };

        public int Rem { get; set; }
        public Address Addr { get; set; }
        public PlacementPolicy Policy { get; set; }
        public IBuilder Builder { get; set; }

        public static Option UseBuilder(IBuilder b)
        {
            return (cfg) => { cfg.Builder = b; };
        }

        public static Option ForContainer(V2.Container ctn)
        {
            return (cfg) =>
            {
                cfg.Policy = ctn.PlacementPolicy;
                cfg.Addr.ContainerId = ctn.CalCulateAndGetID;
            };
        }

        public static Option ForObject(ObjectID id)
        {
            return (cfg) =>
            {
                cfg.Addr.ObjectId = id;
            };
        }

        public static Option SuccessAfter(int v)
        {
            return (cfg) =>
            {
                if (v > 0)
                    cfg.Rem = v;
            };
        }

        public static Option WithoutSuccessTracking()
        {
            return (cfg) =>
            {
                cfg.Rem = -1;
            };
        }
    }

    public class Traverser
    {
        private Node[][] vectors;
        private int[] rem;

        public Traverser(Option[] opts)
        {
            var cfg = Cfg.DefaultCfg;

            foreach (var opt in opts)
            {
                if (opt != null)
                    opt(cfg);
            }

            if (cfg.Builder == null)
                throw new InvalidOperationException("placement builder is null");
            else if (cfg.Policy == null)
                throw new InvalidOperationException("placement policy is null");

            var ns = cfg.Builder.BuildPlacement(cfg.Addr, cfg.Policy);
            var rs = cfg.Policy.Replicas;
            var rem = new int[0];

            foreach (var r in rs)
            {
                var cnt = cfg.Rem;
                if (cnt == 0)
                    cnt = (int)r.Count;
                rem = rem.Append(cnt).ToArray();
            }

            this.rem = rem;
            this.vectors = ns;
        }

        public Network.Address[] Next()
        {
            this.SkipEmptyVectors();
            if (this.vectors.Length == 0)
                return null;
            else if (this.vectors[0].Length < this.rem[0])
                return null;

            var count = this.rem[0];
            if (count < 0)
                count = this.vectors[0].Length;

            var addrs = new Network.Address[0];

            for (int i = 0; i < count; i++)
            {
                var addr = Network.Address.AddressFromString(this.vectors[0][i].NetworkAddress);
                addrs = addrs.Append(addr).ToArray();
            }

            this.vectors[0] = this.vectors[0][count..];
            return addrs;
        }

        private void SkipEmptyVectors()
        {
            for (int i = 0; i < this.vectors.Length; i++)
            {
                if (this.vectors[i].Length == 0 && this.rem[i] <= 0 || this.rem[0] == 0)
                {
                    this.vectors = this.vectors[..i].Concat(this.vectors[(i + 1)..]).ToArray();
                    this.rem = this.rem[..i].Concat(this.rem[(i + 1)..]).ToArray();
                    i--;
                }
                else
                    break;
            }
        }

        public void SubmitSuccess()
        {
            if (this.rem.Length > 0)
                this.rem[0]--;
        }

        public bool Success()
        {
            for (int i = 0; i < this.rem.Length; i++)
            {
                if (this.rem[i] > 0)
                    return false;
            }
            return true;
        }
    }


}
