using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.IO;
using Neo.VM;
using System.Linq;
using Neo.Wallets.NEP6;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class FSStorageTests
    {
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            wallet = new MyWallet("test");
            wallet.CreateAccount("2931fe84623e29817503fd9529bb10472cbb02b4e2de404a8edbfdc669262e16".HexToBytes());
        }

        [TestMethod()]
        public void GetNotifyEventArgsFromJsonTest()
        {
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0,
                Nonce = 0,
                Script = new byte[] { 0x01 },
                Signers = new Signer[] { new Signer() { Account = wallet.GetAccounts().ToArray()[0].ScriptHash } },
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();

            JArray obj = new JArray();
            obj.Add(tx.ToArray().ToHexString());
            obj.Add(UInt160.Zero.ToString());
            obj.Add("test");
            obj.Add(new JArray(new VM.Types.Boolean(true).ToJson()));

            NotifyEventArgs notify = FSStorage.GetNotifyEventArgsFromJson(obj);
            Assert.IsNotNull(notify);
        }

        [TestMethod()]
        public void InitTest()
        {
            var sys = TestBlockchain.TheNeoSystem;
            NEP6Wallet temp = TestBlockchain.wallet;
            Console.WriteLine(temp.GetAccounts().ToArray()[0].GetKey().PublicKey.EncodePoint(true).ToHexString());
        }
    }
}
