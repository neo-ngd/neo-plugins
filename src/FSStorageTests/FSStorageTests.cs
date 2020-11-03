using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC;
using Neo.Plugins.FSStorage.innerring.invoke;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using Moq;
using Neo.Network.RPC.Models;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.IO;
using Neo.VM;
using System.Linq;

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
