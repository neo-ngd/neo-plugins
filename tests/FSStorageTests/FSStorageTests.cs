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
using Neo.Ledger;
using static Neo.Plugins.FSStorage.morph.invoke.MorphClient;
using System.Collections.Generic;
using NeoFS.API.v2.Netmap;
using Google.Protobuf;
using NeoFS.API.v2.Refs;
using NeoFS.API.v2.Container;
using Neo.Cryptography;

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

            NEP6Wallet wallet1 = new NEP6Wallet("./wallet1.json");
            wallet1.Unlock("123456");
            wallet1.Import("L3aqUNaGHJrq7cmV96j8dkS7GQHbx83id4HRXzXtE3PaANCR7bUN");
            NEP6Wallet wallet2 = new NEP6Wallet("./wallet2.json");
            wallet2.Unlock("123456");
            wallet2.Import("L5a9ZV3SyhDLJpjYFZnBgVXop9BnypdJh4EVkw7vQmhcfdX6wkLy");
            NEP6Wallet wallet3 = new NEP6Wallet("./wallet3.json");
            wallet3.Unlock("123456");
            wallet3.Import("L4ueF8CqVjddt1NmmwfP8u79TxfahFmZ1TwgYhLHXuFv8pAqMUGN");
            wallet1.Save();
            wallet2.Save();
            wallet3.Save();

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
            obj.Add(UInt160.Zero.ToArray().ToHexString());
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

        [TestMethod()]
        public void FsContractInit()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var FsContractHash = UInt160.Parse("0xbbf24e35a65a9102443206921e0a2479af7b8f9c");
            var script = MakeScript(FsContractHash, "init", new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray(), accounts.ToArray()[1].GetKey().PublicKey.ToArray(), accounts.ToArray()[2].GetKey().PublicKey.ToArray() });
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                SystemFee = 1000000000,
                ValidUntilBlock = 1000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:");
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void FsContractInnerRingUpdate()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var FsContractHash = UInt160.Parse("0xbbf24e35a65a9102443206921e0a2479af7b8f9c");
            var script = MakeScript(FsContractHash, "innerRingUpdate",new byte[] { 0x02}, new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray(), accounts.ToArray()[1].GetKey().PublicKey.ToArray(), accounts.ToArray()[2].GetKey().PublicKey.ToArray() });
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1246,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash ,Scopes=WitnessScope.Global} },
                SystemFee = 1000000000,
                ValidUntilBlock = 4000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, new FakeSigners(accounts.ToArray()[0].ScriptHash), null, 0, tx.SystemFee);
            engine = ApplicationEngine.Run(script, snapshot, new FakeSigners(accounts.ToArray()[1].ScriptHash), null, 0, tx.SystemFee);
            engine = ApplicationEngine.Run(script, snapshot, new FakeSigners(accounts.ToArray()[2].ScriptHash), null, 0, tx.SystemFee);
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:"+engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void BalanceContractBalanceOf()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var BalanceContractHash = UInt160.Parse("0x08953affe65148d7ec4c8db5a0a6977c32ddf54c");
            var script = BalanceContractHash.MakeScript("balanceOf", accounts.ToArray()[0].ScriptHash);
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, null, null, 0, 20000000000);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(script.ToArray()));

            Console.WriteLine(script.ToArray().ToHexString());
            //Console.WriteLine(Convert.ToBase64String(script));
        }


        [TestMethod()]
        public void FsContractBind()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var FsContractHash = UInt160.Parse("0xbbf24e35a65a9102443206921e0a2479af7b8f9c");
            var txId = UInt256.Parse("0xce9524e8215d2a6c26271dcccfec58cae563cfdf9ef9287b473a38fcbeef6847");
            var script = MakeScript(FsContractHash,"bind", accounts.ToArray()[0].GetKey().PublicKey.ToArray(), new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray()});
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 3052,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash ,Scopes=WitnessScope.Global} },
                SystemFee = 2000000000,
                ValidUntilBlock = 4500,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:"+engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void FsContractDeposite()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var FsContractHash = UInt160.Parse("0xbbf24e35a65a9102443206921e0a2479af7b8f9c");
            var script = FsContractHash.MakeScript("deposit", accounts.ToArray()[0].ScriptHash.ToArray(), 50, new byte[0]);
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 3088,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                SystemFee = 1000000000,
                ValidUntilBlock = 8000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void FsContractWithDraw()
        {
            var newwallet = new MyWallet("test");
            newwallet.Import("L2NpJUsXCm3ajA98bzFWFztjTNrXcfYU9xWzHZgUasvTSA6rnRrR");
            IEnumerable<WalletAccount> accountstemp = newwallet.GetAccounts();
            KeyPair key = accountstemp.ToArray()[0].GetKey();
            Console.WriteLine(key.PublicKeyHash.ToAddress());
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var FsContractHash = Settings.Default.FsContractHash;
            var script = FsContractHash.MakeScript("withdraw", accountstemp.ToArray()[0].ScriptHash.ToArray(), 20);
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce =480,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accountstemp.ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                SystemFee = 1000000000,
                ValidUntilBlock = 3000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            newwallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void FsContractDeposit()
        {
            var newwallet = new MyWallet("test");
            newwallet.Import("L2NpJUsXCm3ajA98bzFWFztjTNrXcfYU9xWzHZgUasvTSA6rnRrR");
            IEnumerable<WalletAccount> accountstemp = newwallet.GetAccounts();
            KeyPair key = accountstemp.ToArray()[0].GetKey();
            Console.WriteLine(key.PublicKeyHash.ToAddress());
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var FsContractHash = Settings.Default.FsContractHash;
            var script = FsContractHash.MakeScript("deposit", accountstemp.ToArray()[0].ScriptHash.ToArray(), 50,new byte[0]);
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 5000,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accountstemp.ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                SystemFee = 2000000000,
                ValidUntilBlock = 5000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            newwallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        private static byte[] MakeScript(UInt160 scriptHash, string operation,byte[] arg0, byte[][] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                for (int i = args.Length - 1; i >= 0; i--)
                {
                    sb.EmitPush(args[i]);
                }
                sb.EmitPush(args.Length);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(arg0);
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(operation);
                sb.EmitPush(scriptHash);
                sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
                return sb.ToArray();
            }
        }

        private static byte[] MakeScript(UInt160 scriptHash, string operation, byte[][] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                for (int i = args.Length - 1; i >= 0; i--)
                {
                    sb.EmitPush(args[i]);
                }
                sb.EmitPush(args.Length);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(1);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(operation);
                sb.EmitPush(scriptHash);
                sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
                return sb.ToArray();
            }
        }

        [TestMethod()]
        public void BalanceContractInit()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var balanceContractHash = Settings.Default.BalanceContractHash;
            var containerContractHash = Settings.Default.ContainerContractHash;
            var netmapContractHash = Settings.Default.NetmapContractHash;
            var script = balanceContractHash.MakeScript("init", netmapContractHash.ToArray(), containerContractHash.ToArray());
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                SystemFee = 1000000000,
                ValidUntilBlock = 1000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:");
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void NetMapContractHashContractInit()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var netmapContractHash = Settings.Default.NetmapContractHash;
            var script = MakeScript(netmapContractHash, "init", new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray(), accounts.ToArray()[1].GetKey().PublicKey.ToArray(), accounts.ToArray()[2].GetKey().PublicKey.ToArray() });
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                SystemFee = 1000000000,
                ValidUntilBlock = 1000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:");
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void NetMapContractHashContractInitConfig()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var netmapContractHash = Settings.Default.NetmapContractHash;
            var script = MakeScript(netmapContractHash, "initConfig", new byte[][] { Utility.StrictUTF8.GetBytes("ContainerFee"), BitConverter.GetBytes(0) });
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                SystemFee = 1000000000,
                ValidUntilBlock = 5000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:");
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void ContainerContractHashContractInit()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var containerContractHash = Settings.Default.ContainerContractHash;
            var netMapContractHash = Settings.Default.NetmapContractHash;
            var balanceContractHash = Settings.Default.BalanceContractHash;
            var neofsIdContractHash = Settings.Default.FsIdContractHash;
            var script = containerContractHash.MakeScript("init", netMapContractHash.ToArray(), balanceContractHash.ToArray(), neofsIdContractHash.ToArray());
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                SystemFee = 1000000000,
                ValidUntilBlock = 1000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:");
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void FsIdContractHashContractInit()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var containerContractHash = Settings.Default.ContainerContractHash;
            var netMapContractHash = Settings.Default.NetmapContractHash;
            var neofsIdContractHash = Settings.Default.FsIdContractHash;
            var script = neofsIdContractHash.MakeScript("init", netMapContractHash.ToArray(), containerContractHash.ToArray());

            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 200000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                SystemFee = 1000000000,
                ValidUntilBlock = 1000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:");
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void AlphabetContractHashContractInit()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            for (int i = 0; i < Settings.Default.AlphabetContractHash.Length; i++) {
                var alphabetContractHash = Settings.Default.AlphabetContractHash[i];
                var netMapContractHash = Settings.Default.NetmapContractHash;
                var script = alphabetContractHash.MakeScript("init", netMapContractHash.ToArray());

                var tx = new Transaction()
                {
                    Attributes = Array.Empty<TransactionAttribute>(),
                    NetworkFee = 200000000,
                    Nonce = 1220,
                    Script = script,
                    Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[0].ScriptHash } },
                    SystemFee = 1000000000,
                    ValidUntilBlock = 1000,
                    Version = 0,
                };
                var data = new ContractParametersContext(tx);
                wallet1.Sign(data);
                tx.Witnesses = data.GetWitnesses();
                var snapshot = Blockchain.Singleton.GetSnapshot();
                //var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
                Console.WriteLine("tx:");
                Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
            }
        }

        [TestMethod()]
        public void NetMapEpoch()
        {
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var netMapContractHash = Settings.Default.NetmapContractHash;
            var script = netMapContractHash.MakeScript("newEpoch", 3);

            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accounts.ToArray()[2].ScriptHash ,Scopes=WitnessScope.Global} },
                SystemFee = 2000000000,
                ValidUntilBlock = 5000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            wallet1.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:"+engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void NetMapContractAddPeer()
        {
            var newwallet = new MyWallet("test");
            newwallet.Import("L2NpJUsXCm3ajA98bzFWFztjTNrXcfYU9xWzHZgUasvTSA6rnRrR");
            IEnumerable<WalletAccount> accountstemp = newwallet.GetAccounts();
            KeyPair key = accountstemp.ToArray()[0].GetKey();
            Console.WriteLine(key.PublicKeyHash.ToAddress());
            var nodeInfo = new NodeInfo()
            {
                PublicKey = Google.Protobuf.ByteString.CopyFrom(key.PublicKey.ToArray()),
                Address = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToAddress(key.PublicKey.ToArray()),
                State = NodeInfo.Types.State.Online
            };

            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var netmapContractHash = Settings.Default.NetmapContractHash;
            var script = netmapContractHash.MakeScript("addPeer", nodeInfo.ToByteArray());
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 1220,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accountstemp.ToArray()[0].ScriptHash ,Scopes=WitnessScope.Global} },
                SystemFee = 10000000000,
                ValidUntilBlock = 8000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            newwallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:"+engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void NetMapContractUpdateState()
        {
            var newwallet = new MyWallet("test");
            newwallet.Import("L2NpJUsXCm3ajA98bzFWFztjTNrXcfYU9xWzHZgUasvTSA6rnRrR");
            IEnumerable<WalletAccount> accountstemp = newwallet.GetAccounts();
            KeyPair key = accountstemp.ToArray()[0].GetKey();
            Console.WriteLine(key.PublicKeyHash.ToAddress());
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            var netmapContractHash = Settings.Default.NetmapContractHash;
            var script = netmapContractHash.MakeScript("updateState", (int)NodeInfo.Types.State.Offline, key.PublicKey.ToArray());
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 1244,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accountstemp.ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                SystemFee = 10000000000,
                ValidUntilBlock = 14000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            newwallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void ContainerContractPut()
        {
            var newwallet = new MyWallet("test");
            newwallet.Import("L2NpJUsXCm3ajA98bzFWFztjTNrXcfYU9xWzHZgUasvTSA6rnRrR");
            IEnumerable<WalletAccount> accountstemp = newwallet.GetAccounts();
            KeyPair key = accountstemp.ToArray()[0].GetKey();
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            OwnerID ownerId = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new NeoFS.API.v2.Refs.Version()
                {
                    Major = 1,
                    Minor = 1,
                },
                BasicAcl = 0,
                Nonce = Google.Protobuf.ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new NeoFS.API.v2.Netmap.PlacementPolicy()
            };

            byte[] sig = Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            var containerContractHash = Settings.Default.ContainerContractHash;
            var script = containerContractHash.MakeScript("put", container.ToByteArray(), sig, key.PublicKey.EncodePoint(true));

            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 1244,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accountstemp.ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                SystemFee = 2000000000,
                ValidUntilBlock = 5000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            newwallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }

        [TestMethod()]
        public void ContainerContractDelete()
        {
            var newwallet = new MyWallet("test");
            newwallet.Import("L2NpJUsXCm3ajA98bzFWFztjTNrXcfYU9xWzHZgUasvTSA6rnRrR");
            IEnumerable<WalletAccount> accountstemp = newwallet.GetAccounts();
            KeyPair key = accountstemp.ToArray()[0].GetKey();
            var wallet1 = TestBlockchain.wallet;
            var accounts = TestBlockchain.wallet.GetAccounts();
            OwnerID ownerId = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new NeoFS.API.v2.Refs.Version()
                {
                    Major = 1,
                    Minor = 1,
                },
                BasicAcl = 0,
                Nonce = Google.Protobuf.ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new NeoFS.API.v2.Netmap.PlacementPolicy()
            };

            byte[] sig = Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            var containerContractHash = Settings.Default.ContainerContractHash;
            var containerId = container.CalCulateAndGetID.Value.ToByteArray();
            sig = Crypto.Sign(containerId, key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            var script = containerContractHash.MakeScript("delete", containerId, sig);

            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 2000000000,
                Nonce = 1244,
                Script = script,
                Signers = new Signer[] { new Signer() { Account = accountstemp.ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } },
                SystemFee = 10000000000,
                ValidUntilBlock = 5000,
                Version = 0,
            };
            var data = new ContractParametersContext(tx);
            newwallet.Sign(data);
            tx.Witnesses = data.GetWitnesses();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = ApplicationEngine.Run(script, snapshot, tx, null, 0, tx.SystemFee);
            Console.WriteLine("tx:" + engine.State);
            Console.WriteLine(Convert.ToBase64String(tx.ToArray()));
        }
    }
}
