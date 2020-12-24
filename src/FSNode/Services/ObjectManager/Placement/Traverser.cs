using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.FSNode.Services.ObjectManager.Placement
{
    public class Traverser
    {
        public bool TrackCopies;
        public int FlatSuccess;
        public int Rem;
        public Address Address;
        public PlacementPolicy Policy;
        public IBuilder Builder;
        private List<Node[]> vectors;
        private int[] rem;

        public Traverser()
        {
            if (Builder == null)
                throw new InvalidOperationException("placement builder is null");
            else if (Policy == null)
                throw new InvalidOperationException("placement policy is null");

            var ns = Builder.BuildPlacement(Address, Policy);
            var rs = Policy.Replicas;
            var rem = Array.Empty<int>();

            foreach (var r in rs)
            {
                var cnt = Rem;
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
            if (this.vectors.Count == 0)
                return null;
            else if (this.vectors[0].Length < this.rem[0])
                return null;

            var count = this.rem[0];
            if (count < 0)
                count = this.vectors[0].Length;

            var addrs = Array.Empty<Network.Address>();

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
            for (int i = 0; i < vectors.Count; i++)
            {
                if (vectors[i].Length == 0 && rem[i] <= 0 || rem[0] == 0)
                {
                    vectors.Remove(vectors[i]);
                    rem = rem[..i].Concat(rem[(i + 1)..]).ToArray();
                    i--;
                }
                else
                    break;
            }
        }

        public void SubmitSuccess()
        {
            if (rem.Length > 0)
                rem[0]--;
        }

        public bool Success()
        {
            for (int i = 0; i < rem.Length; i++)
            {
                if (rem[i] > 0)
                    return false;
            }
            return true;
        }
    }


}
