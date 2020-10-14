using Neo.Plugins.FSStorage.morph.invoke;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins.FSStorage.morph.invoke
{
    public class StaticClient
    {
        private Client client;
        private UInt160 contractHash;
        private long fee;
        public Client Client { get => client; set => client = value; }
        public UInt160 ContractHash { get => contractHash; set => contractHash = value; }
        public long Fee { get => fee; set => fee = value; }

        public bool InvokeFunction(string method, object[] args = null)
        {
            return client.InvokeFunction(ContractHash, method, fee, args);
        }

        public InvokeResult InvokeLocalFunction(string method, object[] args = null)
        {
            return client.InvokeLocalFunction(ContractHash, method, args);
        }
    }
}
