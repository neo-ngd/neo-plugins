using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Neo.Plugins.FSStorage.innerring.invoke.ContractInvoker;
using static Neo.Plugins.FSStorage.morph.client.Tests.MorphClientTests;
using static Neo.Plugins.FSStorage.morph.invoke.MorphClient;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class ContractInvokerTests : TestKit
    {
        private string BalanceContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
        private string ContainerContractHash = "0x4a445a72c5dba72c0c4e4634cff86c48dfe2c396";
        private string FsContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
        private string NetMapContractHash = "0x3fafa517cd771afcbd744e9194065657804ef683";
        private string AlphabetContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";

        private MorphClient client;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            NeoSystem system = TestBlockchain.TheNeoSystem;
            string ConfigFilePath = "./FSStorage/config.json";
            IConfigurationSection config = new ConfigurationBuilder().AddJsonFile(ConfigFilePath, optional: true).Build().GetSection("PluginConfiguration");
            Settings.Load(config);
            wallet = new MyWallet("");
            wallet.CreateAccount();
            client = new MorphClient()
            {
                Wallet = wallet,
                Blockchain = system.ActorSystem.ActorOf(Props.Create(() => new BlockChainFakeActor()))
            };
            //Fake balance
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            UInt160 from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators);
            UInt160 to = accounts.ToArray()[0].ScriptHash;
            Signers signers = new Signers(from);
            byte[] script = NativeContract.GAS.Hash.MakeScript("transfer", from, to, 500_00000000);
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //Fake contract
            //FakeBalanceContract
            string balanceContractNefFilePath = "./contracts/balance/balance_contract.nef";
            string balanceContractManifestPath = "./contracts/balance/balance_contract_config.json";

            var balanceContractManifest = ContractManifest.Parse(File.ReadAllBytes(balanceContractManifestPath));

            NefFile balanceContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(balanceContractNefFilePath), Utility.StrictUTF8, false))
            {
                balanceContractNefFile = stream.ReadSerializable<NefFile>();
            }

            UInt160 balanceContractHash = balanceContractNefFile.Script.ToScriptHash();
            var balanceContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = balanceContractNefFile.Script.ToArray(),
                Manifest = balanceContractManifest
            };
            snapshot.Contracts.Add(balanceContractHash, balanceContract);
            //FakeNetMapContract
            string netMapContractNefFilePath = "./contracts/netmap/netmap_contract.nef";
            string netMapContractManifestPath = "./contracts/netmap/netmap_contract_config.json";

            var netMapContractManifest = ContractManifest.Parse(File.ReadAllBytes(netMapContractManifestPath));

            NefFile netMapContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(netMapContractNefFilePath), Utility.StrictUTF8, false))
            {
                netMapContractNefFile = stream.ReadSerializable<NefFile>();
            }

            UInt160 netMapContractHash = netMapContractNefFile.Script.ToScriptHash();
            var netMapContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = netMapContractNefFile.Script.ToArray(),
                Manifest = netMapContractManifest
            };
            snapshot.Contracts.Add(netMapContractHash, netMapContract);
            //FakeContainerContract
            string containerContractNefFilePath = "./contracts/container/container_contract.nef";
            string containerContractManifestPath = "./contracts/container/container_contract_config.json";

            var containerContractManifest = ContractManifest.Parse(File.ReadAllBytes(containerContractManifestPath));

            NefFile containerContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(containerContractNefFilePath), Utility.StrictUTF8, false))
            {
                containerContractNefFile = stream.ReadSerializable<NefFile>();
            }

            UInt160 containerContractHash = containerContractNefFile.Script.ToScriptHash();
            var containerContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = containerContractNefFile.Script.ToArray(),
                Manifest = containerContractManifest
            };
            snapshot.Contracts.Add(containerContractHash, containerContract);
            //FakeBalanceInit
            script = balanceContractHash.MakeScript("init", netMapContractHash.ToArray(), containerContractHash.ToArray());
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeNetMapInit
            script = MakeScript(netMapContractHash, "init", new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray() });
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            snapshot.Commit();
            script = netMapContractHash.MakeScript("innerRingList");
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);

            Settings.Default.BalanceContractHash = UInt160.Parse(BalanceContractHash);
            Settings.Default.NetmapContractHash = UInt160.Parse(NetMapContractHash);
            Settings.Default.ContainerContractHash = UInt160.Parse(ContainerContractHash);
        }

        private byte[] MakeScript(UInt160 scriptHash, string operation, byte[][] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                for (int i = 0; i < args.Length; i++)
                {
                    sb.EmitPush(args[i]);
                    sb.EmitPush(1);
                    sb.Emit(OpCode.PACK);
                }
                sb.EmitPush(args.Length);
                sb.Emit(OpCode.PACK);
                sb.EmitPush(operation);
                sb.EmitPush(scriptHash);
                sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
                return sb.ToArray();
            }
        }

        [TestMethod()]
        public void InvokeTransferBalanceXTest()
        {
            bool result = ContractInvoker.TransferBalanceX(client, new TransferXParams()
            {
                Sender = UInt160.Zero.ToArray(),
                Receiver = UInt160.Zero.ToArray(),
                Amount = 0,
                Comment = new byte[] { 0x01 }
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeMinerTest()
        {
            bool result = ContractInvoker.Mint(client, new MintBurnParams()
            {
                ScriptHash = Settings.Default.NetmapContractHash.ToArray(),
                Amount = 0,
                Comment = new byte[] { 0x01 }
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeBurnTest()
        {
            bool result = ContractInvoker.Burn(client, new MintBurnParams()
            {
                ScriptHash = Settings.Default.NetmapContractHash.ToArray(),
                Amount = 0,
                Comment = new byte[] { 0x01 }
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeLockAssetTest()
        {
            bool result = ContractInvoker.LockAsset(client, new LockParams()
            {
                ID = new byte[] { 0x01 },
                UserAccount = UInt160.Zero,
                LockAccount = UInt160.Zero,
                Amount = 0,
                Until = 100,
            });
            var tx = ExpectMsg<BlockChainFakeActor.OperationResult>().tx;
            Assert.AreEqual(result, true);
            Assert.IsNotNull(tx);
        }

        [TestMethod()]
        public void InvokeBalancePrecisionTest()
        {
            uint result = ContractInvoker.BalancePrecision(client);
            Assert.AreEqual(result, (uint)12);
        }
    }
}
