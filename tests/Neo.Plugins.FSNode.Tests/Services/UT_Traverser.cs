using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Fs.Services.ObjectManager.Placement;
using NeoFS.API.v2.Container;
using NeoFS.API.v2.Netmap;
using NeoFS.API.v2.Refs;
using System;
using System.Linq;

namespace Neo.Fs.Tests
{
    public class TestBuilder : IBuilder
    {
        private Node[][] vectors;

        public TestBuilder(Node[][] vs)
        {
            this.vectors = vs;
        }

        public Node[][] BuildPlacement(Address addr, PlacementPolicy pp)
        {
            return this.vectors;
        }
    }

    [TestClass]
    public class UT_Traverser
    {
        private (Node[][], Container) PreparePlacement(int[] ss, int[] rs)
        {
            var nodes = new Node[0][];
            var replicas = new Replica[0];
            uint num = 0;

            for (int i = 0; i < ss.Length; i++)
            {
                var ns = new NodeInfo[0];
                for (int j = 0; j < ss[i]; j++)
                {
                    ns = ns.Append(PrepareNode(num)).ToArray();
                    num++;
                }
                nodes = nodes.Append(NodesFromInfo(ns)).ToArray();

                var r = new Replica() { Count = (uint)rs[i] };
                replicas = replicas.Append(r).ToArray();
            }

            var policy = new PlacementPolicy(0, replicas, null, null);

            return (nodes, new Container() { PlacementPolicy = policy });
        }

        private NodeInfo PrepareNode(uint v)
        {
            return new NodeInfo() { Address = "/ip4/0.0.0.0/tcp/" + v.ToString() };
        }

        private Node[] NodesFromInfo(NodeInfo[] infos)
        {
            var nodes = new Node[infos.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                nodes[i] = new Node(i, infos[i]);
            }
            return nodes;
        }

        private Node[][] CopyVectors(Node[][] v)
        {
            var vc = new Node[0][];
            for (int i = 0; i < v.Length; i++)
            {
                var ns = new Node[v[i].Length];
                Array.Copy(v[i], ns, v[i].Length);
                vc = vc.Append(ns).ToArray();
            }
            return vc;
        }

        [TestMethod]
        public void TestTraverserSearch()
        {
            var selectors = new int[] { 2, 3 };
            var replicas = new int[] { 1, 2 };

            Node[][] nodes;
            Container ctn;
            (nodes, ctn) = PreparePlacement(selectors, replicas);

            var nodesCopy = CopyVectors(nodes);

            var tr = new Traverser(new Option[]
            {
                Cfg.ForContainer(ctn),
                Cfg.UseBuilder(new TestBuilder(nodesCopy)),
                Cfg.WithoutSuccessTracking()
            });

            for (int i = 0; i < selectors.Length; i++)
            {
                var addrs = tr.Next();
                Assert.AreEqual(nodes[i].Length, addrs.Length);

                for (int j = 0; j < nodes[i].Length; j++)
                {
                    Assert.AreEqual(addrs[j].String(), nodes[i][j].NetworkAddress);
                }
            }

            Assert.IsTrue(tr.Success());
        }

        private delegate void Fn(int curVector);

        [TestMethod]
        public void TestTraverserRead()
        {
            var selectors = new int[] { 5, 3 };
            var replicas = new int[] { 2, 2 };

            Node[][] nodes;
            Container ctn;
            (nodes, ctn) = PreparePlacement(selectors, replicas);

            var nodesCopy = CopyVectors(nodes);

            var tr = new Traverser(new Option[]
            {
                Cfg.ForContainer(ctn),
                Cfg.UseBuilder(new TestBuilder(nodesCopy)),
                Cfg.SuccessAfter(1)
            });

            Fn fn = (cv) =>
            {
                for (int i = 0; i < selectors[cv]; i++)
                {
                    var addrs = tr.Next();
                    Assert.AreEqual(1, addrs.Length);
                    Assert.AreEqual(nodes[cv][i].NetworkAddress, addrs[0].String());
                }

                Assert.IsFalse(tr.Success());
                tr.SubmitSuccess();
            };

            for (int i = 0; i < selectors.Length; i++)
            {
                fn(i);

                if (i < selectors.Length - 1)
                    Assert.IsFalse(tr.Success());
                else
                    Assert.IsTrue(tr.Success());
            }
        }

        [TestMethod]
        public void TestTraverserPut()
        {
            var selectors = new int[] { 5, 3 };
            var replicas = new int[] { 2, 2 };

            Node[][] nodes;
            Container ctn;
            (nodes, ctn) = PreparePlacement(selectors, replicas);

            var nodesCopy = CopyVectors(nodes);

            var tr = new Traverser(new Option[]
            {
                Cfg.ForContainer(ctn),
                Cfg.UseBuilder(new TestBuilder(nodesCopy))
            });

            Fn fn = (cv) =>
            {
                for (int i = 0; i + replicas[cv] < selectors[cv]; i += replicas[cv])
                {
                    var addrs = tr.Next();
                    Assert.AreEqual(replicas[cv], addrs.Length);
                    for (int j = 0; j < addrs.Length; j++)
                    {
                        Assert.AreEqual(nodes[cv][i + j].NetworkAddress, addrs[j].String());
                    }
                }

                Assert.IsFalse(tr.Success());
                for (int i = 0; i < replicas[cv]; i++)
                {
                    tr.SubmitSuccess();
                }
            };

            for (int i = 0; i < selectors.Length; i++)
            {
                fn(i);

                if (i < selectors.Length - 1)
                    Assert.IsFalse(tr.Success());
                else
                    Assert.IsTrue(tr.Success());
            }
        }
    }
}
