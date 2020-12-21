using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using NeoFS.API.v2.Container;
using NeoFS.API.v2.Netmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Neo.Plugins.FSStorage.morph.invoke.MorphClient;

namespace Neo.Plugins.FSStorage
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static NEP6Wallet wallet;

        public static readonly string BalanceContractHash = "0x08953affe65148d7ec4c8db5a0a6977c32ddf54c";
        public static readonly string ContainerContractHash = "0x4a445a72c5dba72c0c4e4634cff86c48dfe2c396";
        public static readonly string FsContractHash = "0xbbf24e35a65a9102443206921e0a2479af7b8f9c";
        public static readonly string FsIdContractHash = "0xd376487192d0ff8f03b5878be657337e8308709b";
        public static readonly string NetMapContractHash = "0x3fafa517cd771afcbd744e9194065657804ef683";
        public static readonly string AlphabetAZContractHash0 = "0x496d839a9ccdd364066323dd7bde210dfa7267e2";

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem();
            // Ensure that blockchain is loaded
            var _ = Blockchain.Singleton;
            InitializeMockNeoSystem();
        }

        public static void InitializeMockNeoSystem()
        {
            NeoSystem system = TheNeoSystem;
            string ConfigFilePath = "./FSStorage/config.json";
            IConfigurationSection config = new ConfigurationBuilder().AddJsonFile(ConfigFilePath, optional: true).Build().GetSection("PluginConfiguration");
            Settings.Load(config);
            wallet = new NEP6Wallet(Settings.Default.WalletPath);
            wallet.Unlock(Settings.Default.Password);
            //Fake balance
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            UInt160 from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators);
            UInt160 to = accounts.ToArray()[0].ScriptHash;
            FakeSigners signers = new FakeSigners(from);
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
            //FakeFsContract
            string neofsContractNefFilePath = "./contracts/neofs/neofs_contract.nef";
            string neofsContractManifestPath = "./contracts/neofs/neofs_contract_config.json";
            var neofsContractManifest = ContractManifest.Parse(File.ReadAllBytes(neofsContractManifestPath));
            NefFile neofsContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(neofsContractNefFilePath), Utility.StrictUTF8, false))
            {
                neofsContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 neofsContractHash = neofsContractNefFile.Script.ToScriptHash();
            var neofsContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = neofsContractNefFile.Script.ToArray(),
                Manifest = neofsContractManifest
            };
            snapshot.Contracts.Add(neofsContractHash, neofsContract);
            //FakeFsIdContract
            string neofsIdContractNefFilePath = "./contracts/neofsid/neofsid_contract.nef";
            string neofsIdContractManifestPath = "./contracts/neofsid/neofsid_contract_config.json";
            var neofsIdContractManifest = ContractManifest.Parse(File.ReadAllBytes(neofsIdContractManifestPath));
            NefFile neofsIdContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(neofsIdContractNefFilePath), Utility.StrictUTF8, false))
            {
                neofsIdContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 neofsIdContractHash = neofsIdContractNefFile.Script.ToScriptHash();
            var neofsIdContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = neofsIdContractNefFile.Script.ToArray(),
                Manifest = neofsIdContractManifest
            };
            snapshot.Contracts.Add(neofsIdContractHash, neofsIdContract);
            //AlphabetAzContract
            string alphabetAzContractNefFilePath = "./contracts/alphabet/az/az_contract.nef";
            string alphabetAzContractManifestPath = "./contracts/alphabet/az/az_contract_config.json";
            var alphabetAzContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetAzContractManifestPath));
            NefFile alphabetAzContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetAzContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetAzContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetAzContractHash = alphabetAzContractNefFile.Script.ToScriptHash();
            var alphabetAzContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetAzContractNefFile.Script.ToArray(),
                Manifest = alphabetAzContractManifest
            };
            snapshot.Contracts.Add(alphabetAzContractHash, alphabetAzContract);
            //AlphabetBukyContract
            string alphabetBukyContractNefFilePath = "./contracts/alphabet/buky/buky_contract.nef";
            string alphabetBukyContractManifestPath = "./contracts/alphabet/buky/buky_contract_config.json";
            var alphabetBukyContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetBukyContractManifestPath));
            NefFile alphabetBukyContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetBukyContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetBukyContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetBukyContractHash = alphabetBukyContractNefFile.Script.ToScriptHash();
            var alphabetBukyContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetBukyContractNefFile.Script.ToArray(),
                Manifest = alphabetBukyContractManifest
            };
            snapshot.Contracts.Add(alphabetBukyContractHash, alphabetBukyContract);
            //AlphabetDobroContract
            string alphabetDobroContractNefFilePath = "./contracts/alphabet/dobro/dobro_contract.nef";
            string alphabetDobroContractManifestPath = "./contracts/alphabet/dobro/dobro_contract_config.json";
            var alphabetDobroContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetDobroContractManifestPath));
            NefFile alphabetDobroContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetDobroContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetDobroContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetDobroContractHash = alphabetDobroContractNefFile.Script.ToScriptHash();
            var alphabetDobroContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetDobroContractNefFile.Script.ToArray(),
                Manifest = alphabetDobroContractManifest
            };
            snapshot.Contracts.Add(alphabetDobroContractHash, alphabetDobroContract);
            //AlphabetGlagoliContract
            string alphabetGlagoliContractNefFilePath = "./contracts/alphabet/glagoli/glagoli_contract.nef";
            string alphabetGlagoliContractManifestPath = "./contracts/alphabet/glagoli/glagoli_contract_config.json";
            var alphabetGlagoliContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetGlagoliContractManifestPath));
            NefFile alphabetGlagoliContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetGlagoliContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetGlagoliContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetGlagoliContractHash = alphabetGlagoliContractNefFile.Script.ToScriptHash();
            var alphabetGlagoliContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetGlagoliContractNefFile.Script.ToArray(),
                Manifest = alphabetGlagoliContractManifest
            };
            snapshot.Contracts.Add(alphabetGlagoliContractHash, alphabetGlagoliContract);
            //AlphabetJestContract
            string alphabetJestContractNefFilePath = "./contracts/alphabet/jest/jest_contract.nef";
            string alphabetJestContractManifestPath = "./contracts/alphabet/jest/jest_contract_config.json";
            var alphabetJestContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetJestContractManifestPath));
            NefFile alphabetJestContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetJestContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetJestContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetJestContractHash = alphabetJestContractNefFile.Script.ToScriptHash();
            var alphabetJestContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetJestContractNefFile.Script.ToArray(),
                Manifest = alphabetJestContractManifest
            };
            snapshot.Contracts.Add(alphabetJestContractHash, alphabetJestContract);
            //AlphabetVediContract
            string alphabetVediContractNefFilePath = "./contracts/alphabet/vedi/vedi_contract.nef";
            string alphabetVediContractManifestPath = "./contracts/alphabet/vedi/vedi_contract_config.json";
            var alphabetVediContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetVediContractManifestPath));
            NefFile alphabetVediContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetVediContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetVediContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetVediContractHash = alphabetVediContractNefFile.Script.ToScriptHash();
            var alphabetVediContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetVediContractNefFile.Script.ToArray(),
                Manifest = alphabetVediContractManifest
            };
            snapshot.Contracts.Add(alphabetVediContractHash, alphabetVediContract);
            //AlphabetZhiveteContract
            string alphabetZhiveteContractNefFilePath = "./contracts/alphabet/zhivete/zhivete_contract.nef";
            string alphabetZhiveteContractManifestPath = "./contracts/alphabet/zhivete/zhivete_contract_config.json";
            var alphabetZhiveteContractManifest = ContractManifest.Parse(File.ReadAllBytes(alphabetZhiveteContractManifestPath));
            NefFile alphabetZhiveteContractNefFile;
            using (var stream = new BinaryReader(File.OpenRead(alphabetZhiveteContractNefFilePath), Utility.StrictUTF8, false))
            {
                alphabetZhiveteContractNefFile = stream.ReadSerializable<NefFile>();
            }
            UInt160 alphabetZhiveteContractHash = alphabetZhiveteContractNefFile.Script.ToScriptHash();
            var alphabetZhiveteContract = new ContractState
            {
                Id = snapshot.ContractId.GetAndChange().NextId++,
                Script = alphabetZhiveteContractNefFile.Script.ToArray(),
                Manifest = alphabetZhiveteContractManifest
            };
            snapshot.Contracts.Add(alphabetZhiveteContractHash, alphabetZhiveteContract);
            //FakeBalanceInit
            script = balanceContractHash.MakeScript("init", netMapContractHash.ToArray(), containerContractHash.ToArray());
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeNetMapInit
            script = MakeScript(netMapContractHash, "init", new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray()});
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeNetMapConfigInit/ContainerFee
            script = MakeScript(netMapContractHash, "initConfig", new byte[][] { Utility.StrictUTF8.GetBytes("ContainerFee"), BitConverter.GetBytes(0) });
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeContainerInit
            script = containerContractHash.MakeScript("init", netMapContractHash.ToArray(), balanceContractHash.ToArray(), neofsIdContractHash.ToArray());
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeFsIdInit
            script = neofsIdContractHash.MakeScript("init", netMapContractHash.ToArray(), containerContractHash.ToArray());
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeFsInit
            script = MakeScript(neofsContractHash, "init", new byte[][] { accounts.ToArray()[0].GetKey().PublicKey.ToArray(), accounts.ToArray()[1].GetKey().PublicKey.ToArray(), accounts.ToArray()[2].GetKey().PublicKey.ToArray() });
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //FakeAZInit
            script = alphabetAzContractHash.MakeScript("init", netMapContractHash.ToArray());
            engine = ApplicationEngine.Run(script, snapshot, container: signers, null, 0, 2000000000);
            //Fake peer
            NodeInfo nodeInfo = new NodeInfo();
            nodeInfo.Address = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToAddress(accounts.ToArray()[0].GetKey().PublicKey.ToArray());
            nodeInfo.PublicKey = Google.Protobuf.ByteString.CopyFrom(accounts.ToArray()[0].GetKey().PublicKey.ToArray());
            var rawNodeInfo = nodeInfo.ToByteArray();
            script = netMapContractHash.MakeScript("addPeer", rawNodeInfo);
            engine = ApplicationEngine.Run(script, snapshot, container: new FakeSigners(to), null, 0, 2000000000);
            //Fake container
            KeyPair key = accounts.ToArray()[0].GetKey();
            NeoFS.API.v2.Refs.OwnerID ownerId = NeoFS.API.v2.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new NeoFS.API.v2.Refs.Version(),
                BasicAcl = 0,
                Nonce = Google.Protobuf.ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new NeoFS.API.v2.Netmap.PlacementPolicy()
            };
            byte[] sig = Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            script = containerContractHash.MakeScript("put", container.ToByteArray(),sig, key.PublicKey.ToArray());
            engine = ApplicationEngine.Run(script, snapshot, container: new FakeSigners(to), null, 0, 2000000000);
            //Fake eacl
            NeoFS.API.v2.Acl.EACLTable eACLTable = new NeoFS.API.v2.Acl.EACLTable()
            {
                ContainerId = container.CalCulateAndGetID,
                Version = new NeoFS.API.v2.Refs.Version(),
            };
            eACLTable.Records.Add(new NeoFS.API.v2.Acl.EACLRecord());
            sig = Crypto.Sign(eACLTable.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            script = containerContractHash.MakeScript("setEACL", eACLTable.ToByteArray(), sig);
            engine = ApplicationEngine.Run(script, snapshot, container: new FakeSigners(to), null, 0, 2000000000);
            //Fake Epoch
            script = netMapContractHash.MakeScript("newEpoch",2);
            engine = ApplicationEngine.Run(script, snapshot, new FakeSigners(to), null, 0, 2000000000);
            script = netMapContractHash.MakeScript("newEpoch", 2);
            engine = ApplicationEngine.Run(script, snapshot, new FakeSigners(accounts.ToArray()[1].ScriptHash), null, 0, 2000000000);
            script = netMapContractHash.MakeScript("newEpoch",2);
            engine = ApplicationEngine.Run(script, snapshot, container: new FakeSigners(accounts.ToArray()[2].ScriptHash), null, 0, 2000000000);
            snapshot.Commit();
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
