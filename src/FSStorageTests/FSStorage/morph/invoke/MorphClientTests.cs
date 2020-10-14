using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage.morph.client.Tests
{
    [TestClass()]
    public class MorphClientTests
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
        public void InvokeLocalFunctionTest()
        {
            InvokeResult result = client.InvokeLocalFunction(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);
            Assert.AreEqual(result.State, VM.VMState.HALT);
            Assert.AreEqual(result.GasConsumed, 2007750);
            Assert.AreEqual(result.ResultStack[0].GetInteger(), 0);
        }

        [TestMethod()]
        public void InvokeFunctionTest()
        {
            //Assert.Fail();
        }

        public class MyWallet : Wallet
        {
            public string path;

            public override string Name => "MyWallet";

            public override Version Version => Version.Parse("0.0.1");

            Dictionary<UInt160, WalletAccount> accounts = new Dictionary<UInt160, WalletAccount>();

            public MyWallet(string path) : base(path)
            {
            }

            public override bool ChangePassword(string oldPassword, string newPassword)
            {
                throw new NotImplementedException();
            }

            public override bool Contains(UInt160 scriptHash)
            {
                return accounts.ContainsKey(scriptHash);
            }

            public void AddAccount(WalletAccount account)
            {
                accounts.Add(account.ScriptHash, account);
            }

            public override WalletAccount CreateAccount(byte[] privateKey)
            {
                KeyPair key = new KeyPair(privateKey);
                Neo.Wallets.SQLite.VerificationContract contract = new Neo.Wallets.SQLite.VerificationContract
                {
                    Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                    ParameterList = new[] { ContractParameterType.Signature }
                };
                MyWalletAccount account = new MyWalletAccount(contract.ScriptHash);
                account.SetKey(key);
                account.Contract = contract;
                AddAccount(account);
                return account;
            }

            public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
            {
                MyWalletAccount account = new MyWalletAccount(contract.ScriptHash)
                {
                    Contract = contract
                };
                account.SetKey(key);
                AddAccount(account);
                return account;
            }

            public override WalletAccount CreateAccount(UInt160 scriptHash)
            {
                MyWalletAccount account = new MyWalletAccount(scriptHash);
                AddAccount(account);
                return account;
            }

            public override bool DeleteAccount(UInt160 scriptHash)
            {
                return accounts.Remove(scriptHash);
            }

            public override WalletAccount GetAccount(UInt160 scriptHash)
            {
                accounts.TryGetValue(scriptHash, out WalletAccount account);
                return account;
            }

            public override IEnumerable<WalletAccount> GetAccounts()
            {
                return accounts.Values;
            }

            public override bool VerifyPassword(string password)
            {
                return true;
            }
        }

        public class MyWalletAccount : WalletAccount
        {
            private KeyPair key = null;
            public override bool HasKey => key != null;

            public MyWalletAccount(UInt160 scriptHash)
                : base(scriptHash)
            {
            }

            public override KeyPair GetKey()
            {
                return key;
            }

            public void SetKey(KeyPair inputKey)
            {
                key = inputKey;
            }
        }

    }
}
