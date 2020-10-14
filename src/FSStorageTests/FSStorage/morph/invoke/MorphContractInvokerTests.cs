using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.FSStorage.morph.client;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Text;
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
            Wallet wallet = new MyWallet("");
            wallet.CreateAccount();
            client = new MorphClient(wallet, null, 0);
        }

        [TestMethod()]
        public void InvokeBalanceOfTest()
        {
            /*            MorphContractInvoker invoker = new MorphContractInvoker();
                        invoker.BalanceContractHash = NativeContract.GAS.Hash;
                        long result=invoker.InvokeBalanceOf(client,UInt160.Zero.toArray());
                        Assert.AreEqual(result, 0);*/
        }

        [TestMethod()]
        public void InvokeDecimalsTest()
        {
            MorphContractInvoker invoker = new MorphContractInvoker();
            invoker.BalanceContractHash = NativeContract.GAS.Hash;
            long result = invoker.InvokeDecimals(client);
            Assert.AreEqual(result, 8);
        }
    }
}
