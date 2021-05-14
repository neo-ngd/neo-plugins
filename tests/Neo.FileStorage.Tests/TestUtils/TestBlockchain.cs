using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Netmap;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using static Neo.FileStorage.Morph.Invoker.MorphClient;

namespace Neo.FileStorage.Tests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static NEP6Wallet wallet;
        public static Settings settings;

        static TestBlockchain()
        {
            TheNeoSystem = new NeoSystem(ProtocolSettings.Default);
            Console.WriteLine("initialize NeoSystem");
            InitializeMockNeoSystem();
        }

        public static void InitializeMockNeoSystem()
        {
            NeoSystem system = TheNeoSystem;
            string ConfigFilePath = "./Config/config.json";
            IConfigurationSection config = new ConfigurationBuilder().AddJsonFile(ConfigFilePath, optional: true).Build().GetSection("PluginConfiguration");
            Settings.Load(config);
            settings = Settings.Default;
            wallet = new NEP6Wallet(Settings.Default.WalletPath, TheNeoSystem.Settings);
            wallet.Unlock(Settings.Default.Password);
            if (!File.Exists(Settings.Default.WalletPath))
            {
                for (int i = 0; i < 7; i++)
                {
                    wallet.CreateAccount();
                }
                wallet.Save();
            }
            //Fake balance
            IEnumerable<WalletAccount> accounts = wallet.GetAccounts();
            UInt160 from = Contract.GetBFTAddress(TheNeoSystem.Settings.StandbyValidators);
            UInt160 to = accounts.ToArray()[0].ScriptHash;
            FakeSigners signers = new FakeSigners(from);
            byte[] script = NativeContract.GAS.Hash.MakeScript("transfer", from, to, 500_00000000, null);
            using var snapshot = TheNeoSystem.GetSnapshot();
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, null, TheNeoSystem.Settings, 0, 2000000000);
            //Fake deploy contract
            string DeployContractsCasesPath = "./Config/Contracts/DeployContractCases.json";
            IEnumerable<IConfigurationSection> deployContracts = new ConfigurationBuilder().AddJsonFile(DeployContractsCasesPath, optional: true).Build().GetSection("DeployContracts").GetChildren();
            var balanceSender = UInt160.Zero;
            var containerSender = UInt160.Zero;
            var fsSender = UInt160.Zero;
            var fsidSender = UInt160.Zero;
            var alphabetSender = UInt160.Zero;
            var netmapSender = UInt160.Zero;
            foreach (IConfigurationSection deployContract in deployContracts)
            {
                string contractName = deployContract.GetSection("Name").Value;
                string nefFilePath = deployContract.GetSection("NefFilePath").Value;
                string manifestPath = deployContract.GetSection("ManifestPath").Value;
                UInt160 sender = accounts.ToArray()[int.Parse(deployContract.GetSection("accountIndex").Value)].ScriptHash;
                DeployContract(snapshot, contractName, nefFilePath, manifestPath, sender, out UInt160 contractHash);
                switch (manifestPath)
                {
                    case "./Config/Contracts/balance/config.json":
                        settings.BalanceContractHash = contractHash;
                        balanceSender = sender;
                        break;
                    case "./Config/Contracts/alphabet/config.json":
                        settings.AlphabetContractHash = new UInt160[] { contractHash };
                        alphabetSender = sender;
                        break;
                    case "./Config/Contracts/audit/config.json":
                        settings.AuditContractHash = contractHash;
                        break;
                    case "./Config/Contracts/container/config.json":
                        settings.ContainerContractHash = contractHash;
                        containerSender = sender;
                        break;
                    case "./Config/Contracts/neofs/config.json":
                        settings.FsContractHash = contractHash;
                        fsidSender = sender;
                        break;
                    case "./Config/Contracts/neofsid/config.json":
                        settings.FsIdContractHash = contractHash;
                        fsidSender = sender;
                        break;
                    case "./Config/Contracts/netmap/config.json":
                        settings.NetmapContractHash = contractHash;
                        netmapSender = sender;
                        break;
                        //case "./Config/Contracts/reputation/config.json":
                        //    settings. = contractHash;
                        //    break;

                }
            }
            //Fake contract init
            //FakeBalanceInit
            script = settings.BalanceContractHash.MakeScript("init", false, balanceSender, settings.NetmapContractHash.ToArray(), settings.ContainerContractHash.ToArray());
            ExecuteScript(snapshot, "BalanceInit", script, from);
            //FakeNetMapInit
            //script = MakeScript(settings.NetmapContractHash, "init", accounts.Select(p => p.GetKey().PublicKey.ToArray()).ToArray());
            script = settings.NetmapContractHash.MakeScript("init", false, netmapSender, settings.BalanceContractHash.ToArray(), settings.ContainerContractHash.ToArray(), ToParameter(accounts.Select(p => p.GetKey().PublicKey.ToArray()).ToArray()));
            ExecuteScript(snapshot, "NetMapInit", script, from);
            //FakeNetMapConfigInit/ContainerFee
            //script = MakeScript(settings.NetmapContractHash, "initConfig", new byte[][] { Neo.Utility.StrictUTF8.GetBytes("ContainerFee"), BitConverter.GetBytes(0) });
            script = settings.NetmapContractHash.MakeScript("initConfig", ToParameter(new byte[][] { Neo.Utility.StrictUTF8.GetBytes("ContainerFee"), BitConverter.GetBytes(0) }));
            ExecuteScript(snapshot, "NetMapConfigInit/ContainerFee", script, from);
            //FakeContainerInit
            script = settings.ContainerContractHash.MakeScript("init", false, containerSender, settings.NetmapContractHash.ToArray(), settings.BalanceContractHash.ToArray(), settings.FsIdContractHash.ToArray());
            ExecuteScript(snapshot, "ContainerInit", script, from);
            //FakeFsIdInit
            script = settings.FsIdContractHash.MakeScript("init", false, fsidSender, settings.NetmapContractHash.ToArray(), settings.ContainerContractHash.ToArray());
            ExecuteScript(snapshot, "FsIdInit", script, from);
            //FakeFsInit
            script = settings.FsContractHash.MakeScript("init", false, fsSender, UInt160.Zero, ToParameter(accounts.Select(p => p.GetKey().PublicKey.ToArray()).ToArray()));
            ExecuteScript(snapshot, "FsInit", script, from);
            //FakeAlphabetInit
            for (int i = 0; i < settings.AlphabetContractHash.Length; i++)
            {
                script = settings.AlphabetContractHash[i].MakeScript("init", false, alphabetSender, settings.NetmapContractHash.ToArray(), UInt160.Zero, "Alphabet" + i, i, settings.AlphabetContractHash.Length);
                ExecuteScript(snapshot, "Alphabet" + i + "Init", script, from);
            }
            //Fake peer
            NodeInfo nodeInfo = new NodeInfo();
            nodeInfo.Address = Neo.FileStorage.API.Cryptography.KeyExtension.PublicKeyToAddress(accounts.ToArray()[0].GetKey().PublicKey.ToArray());
            nodeInfo.PublicKey = ByteString.CopyFrom(accounts.ToArray()[0].GetKey().PublicKey.ToArray());
            var rawNodeInfo = nodeInfo.ToByteArray();
            script = settings.NetmapContractHash.MakeScript("addPeer", rawNodeInfo);
            ExecuteScript(snapshot, "FakePeer", script, accounts.ToArray()[0].ScriptHash);

            //for (int i = 0; i < accounts.Count(); i++)
            //{
            //    ExecuteScript(snapshot, "FakePeer", script, accounts.ToArray()[0].ScriptHash);
            //}
            //Fake container
            KeyPair key = accounts.ToArray()[0].GetKey();
            Neo.FileStorage.API.Refs.OwnerID ownerId = Neo.FileStorage.API.Cryptography.KeyExtension.PublicKeyToOwnerID(key.PublicKey.ToArray());
            Container container = new Container()
            {
                Version = new Neo.FileStorage.API.Refs.Version(),
                BasicAcl = 0,
                Nonce = ByteString.CopyFrom(new byte[16], 0, 16),
                OwnerId = ownerId,
                PlacementPolicy = new PlacementPolicy()
            };
            byte[] sig = Neo.Cryptography.Crypto.Sign(container.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            script = settings.ContainerContractHash.MakeScript("put", container.ToByteArray(), sig, key.PublicKey.ToArray());
            var containerId = container.CalCulateAndGetId.Value.ToByteArray();
            var committees = NativeContract.NEO.GetCommittee(snapshot);
            var address = Contract.CreateMultiSigRedeemScript(committees.Length * 2 / 3 + 1, committees).ToScriptHash();
            for (int i = 0; i < accounts.Count(); i++)
            {

                ExecuteScript(snapshot, "FakeContainer", script, address);
            }
            Console.WriteLine("FakeContainerID:" + containerId.ToHexString());
            //Fake eacl
            Neo.FileStorage.API.Acl.EACLTable eACLTable = new Neo.FileStorage.API.Acl.EACLTable()
            {
                ContainerId = container.CalCulateAndGetId,
                Version = new Neo.FileStorage.API.Refs.Version(),
            };
            eACLTable.Records.Add(new Neo.FileStorage.API.Acl.EACLRecord());
            sig = Cryptography.Crypto.Sign(eACLTable.ToByteArray(), key.PrivateKey, key.PublicKey.EncodePoint(false)[1..]);
            script = settings.ContainerContractHash.MakeScript("setEACL", eACLTable.ToByteArray(), sig);
            ExecuteScript(snapshot, "FakeEacl", script, accounts.ToArray()[0].ScriptHash);
            //Fake Epoch
            for (int i = 0; i < accounts.Count(); i++)
            {
                script = settings.NetmapContractHash.MakeScript("newEpoch", 1);
                ExecuteScript(snapshot, "FakeEpoch", script, address);
            }
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


        private static ContractParameter ToParameter(byte[][] args)
        {
            var array = new ContractParameter(ContractParameterType.Array);
            var list = new List<ContractParameter>();
            foreach (var bytes in args)
            {
                list.Add(new ContractParameter(ContractParameterType.ByteArray) { Value = bytes });
            }

            array.Value = list;
            return array;
        }
        public static void ContractAdd(DataCache snapshot, ContractState contract)
        {
            var key = new KeyBuilder(-1, 8).Add(contract.Hash);
            snapshot.Add(key, new StorageItem(contract));
        }

        private static void DeployContract(DataCache snapshot, string contractName, string nefFilePath, string manifestFilePath, UInt160 sender, out UInt160 contractHash)
        {
            var manifest = ContractManifest.Parse(File.ReadAllBytes(manifestFilePath));
            NefFile nef;
            using (var stream = new BinaryReader(File.OpenRead(nefFilePath), Neo.Utility.StrictUTF8, false))
            {
                nef = stream.ReadSerializable<NefFile>();
            }
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nef.ToArray(), manifest.ToJson().ToString(), null);
            Random rand = new Random();
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = sb.ToArray(),
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + TheNeoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = new Signer[] { new Signer() { Account = sender, Scopes = WitnessScope.Global } },
                Attributes = Array.Empty<TransactionAttribute>(),
            };
            ApplicationEngine engine = ApplicationEngine.Run(sb.ToArray(), snapshot, tx, null, TheNeoSystem.Settings, 0, 2000000000);

            if (engine.State != VMState.HALT)
            {
                contractHash = UInt160.Zero;
                Console.WriteLine($"Deploy {contractName} contract fault.Contract Hash:{contractHash}");
            }
            else
            {
                contractHash = new UInt160(((VM.Types.Array)engine.ResultStack.Peek())[2].GetSpan());
                Console.WriteLine($"Deploy {contractName} contract success.Contract Hash:{contractHash}");
            }
        }

        private static void ExecuteScript(DataCache snapshot, string functionName, byte[] script, UInt160 sender)
        {
            FakeSigners signers = new FakeSigners(sender);
            ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, container: signers, null, TheNeoSystem.Settings, 0, 2000000000);
            var faultMessage = engine.FaultException?.ToString();
            var innerMessage = engine.FaultException?.InnerException?.ToString();
            var errorMessage = $"\r\nException:{faultMessage}";
            if (!string.IsNullOrWhiteSpace(innerMessage))
            {
                errorMessage += "\r\n\tInner:" + innerMessage;
            }

            if (engine.State != VMState.HALT)
                Console.WriteLine($"{functionName} execute fault.{errorMessage}");
            else
                Console.WriteLine($"{functionName} execute success.");
        }
    }

    public class MyWallet : Wallet
    {
        public string path;

        public override string Name => "MyWallet";

        public override Version Version => Version.Parse("0.0.1");

        Dictionary<UInt160, WalletAccount> accounts = new Dictionary<UInt160, WalletAccount>();

        public MyWallet(string path) : base(path, ProtocolSettings.Default)
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
            Contract contract = new Contract
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
            : base(scriptHash, ProtocolSettings.Default)
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
