using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using Neo.Wallets;
using static Neo.Plugins.FSStorage.morph.client.Tests.MorphClientTests;

namespace Neo.Plugins.FSStorage.morph.invoke.Tests
{
    [TestClass()]
    public class MorphContractInvokerTests
    {
        private MorphClient client;
        private Wallet wallet;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            string ConfigFile = "./FSStorage/config.json";
            IConfigurationSection config = new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true).Build().GetSection("PluginConfiguration");
            Settings.Load(config);
            wallet = new MyWallet("");
            wallet.CreateAccount();
            client = new MorphClient()
            {
                Wallet = wallet,
            };
        }

        [TestMethod()]
        public void InvokeBalanceOfTest()
        {
            /*          MorphContractInvoker invoker = new MorphContractInvoker();
                        invoker.BalanceContractHash = NativeContract.GAS.Hash;
                        long result=invoker.InvokeBalanceOf(client,UInt160.Zero.toArray());
                        Assert.AreEqual(result, 0);*/
        }

        [TestMethod()]
        public void InvokeDecimalsTest()
        {
            Settings.Default.BalanceContractHash = NativeContract.GAS.Hash;
            long result = MorphContractInvoker.InvokeDecimals(client);
            Assert.AreEqual(result, 8);
        }
    }
}
