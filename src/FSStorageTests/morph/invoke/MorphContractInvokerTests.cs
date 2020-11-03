using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Neo.Plugins.FSStorage.morph.client.Tests.MorphClientTests;
using static Neo.Plugins.FSStorage.morph.invoke.MorphClient;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class MorphContractInvokerTests : TestKit
    {
        private string BalanceContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
        private string ContainerContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
        private string FsContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
        private string NetMapContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
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
            string nefFilePath = "./contracts/balance/balance_contract.nef";
            string manifestPath = "./contracts/balance/balance_contract_config.json";

            var manifest = ContractManifest.Parse(File.ReadAllBytes(manifestPath));

            NefFile nefFile;
            using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Utility.StrictUTF8, false))
            {
                nefFile = stream.ReadSerializable<NefFile>();
            }

            UInt160 balanceContractHash = nefFile.Script.ToScriptHash();
            var balanceContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = nefFile.Script.ToArray(),
                Manifest = manifest
            };
            snapshot.Contracts.Add(balanceContractHash, balanceContract);
            snapshot.Commit();
        }

        [TestMethod()]
        public void InvokeBalanceOfTest()
        {
            Settings.Default.BalanceContractHash = UInt160.Parse(BalanceContractHash);
            long result = MorphContractInvoker.InvokeBalanceOf(client, UInt160.Zero.ToArray());
            Assert.AreEqual(result, 0);
        }

        [TestMethod()]
        public void InvokeDecimalsTest()
        {
            Settings.Default.BalanceContractHash = UInt160.Parse(BalanceContractHash);
            long result = MorphContractInvoker.InvokeDecimals(client);
            Assert.AreEqual(result, 12);
        }
    }
}
