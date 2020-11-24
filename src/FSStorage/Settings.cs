using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Neo.Plugins.FSStorage
{
    public class Settings
    {
        private string url;
        private string walletPath;
        private string password;
        private string path;

        private UInt160 netmapContractHash;
        private UInt160 fsContractHash;
        private UInt160 fsIdContractHash;
        private UInt160 balanceContractHash;
        private UInt160 containerContractHash;
        private UInt160[] alphabetContractHash;

        private int netmapContractWorkersSize;
        private int fsContractWorkersSize;
        private int balanceContractWorkersSize;
        private int containerContractWorkersSize;
        private int alphabetContractWorkersSize;

        private long epochDuration;
        private long alphabetDuration;

        private int mintEmitCacheSize;
        private ulong mintEmitThreshold;
        private long mintEmitValue;
        private ulong storageEmission;

        private bool cleanupEnabled;
        private ulong cleanupThreshold;

        private bool isSender;

        public static Settings Default { get; private set; }
        public string WalletPath { get => walletPath; set => walletPath = value; }
        public string Password { get => password; set => password = value; }
        public string Path { get => path; set => path = value; }
        public UInt160 NetmapContractHash { get => netmapContractHash; set => netmapContractHash = value; }
        public UInt160 FsContractHash { get => fsContractHash; set => fsContractHash = value; }
        public UInt160 BalanceContractHash { get => balanceContractHash; set => balanceContractHash = value; }
        public UInt160 ContainerContractHash { get => containerContractHash; set => containerContractHash = value; }
        public UInt160[] AlphabetContractHash { get => alphabetContractHash; set => alphabetContractHash = value; }
        public UInt160 FsIdContractHash { get => fsIdContractHash; set => fsIdContractHash = value; }
        public int NetmapContractWorkersSize { get => netmapContractWorkersSize; set => netmapContractWorkersSize = value; }
        public int FsContractWorkersSize { get => fsContractWorkersSize; set => fsContractWorkersSize = value; }
        public int BalanceContractWorkersSize { get => balanceContractWorkersSize; set => balanceContractWorkersSize = value; }
        public int ContainerContractWorkersSize { get => containerContractWorkersSize; set => containerContractWorkersSize = value; }
        public int AlphabetContractWorkersSize { get => alphabetContractWorkersSize; set => alphabetContractWorkersSize = value; }

        public string Url { get => url; set => url = value; }
        public long EpochDuration { get => epochDuration; set => epochDuration = value; }
        public long AlphabetDuration { get => alphabetDuration; set => alphabetDuration = value; }
        public int MintEmitCacheSize { get => mintEmitCacheSize; set => mintEmitCacheSize = value; }
        public ulong MintEmitThreshold { get => mintEmitThreshold; set => mintEmitThreshold = value; }
        public long MintEmitValue { get => mintEmitValue; set => mintEmitValue = value; }
        public ulong StorageEmission { get => storageEmission; set => storageEmission = value; }
        public bool CleanupEnabled { get => cleanupEnabled; set => cleanupEnabled = value; }
        public ulong CleanupThreshold { get => cleanupThreshold; set => cleanupThreshold = value; }
        public bool IsSender { get => isSender; set => isSender = value; }

        public List<UInt160> Contracts = new List<UInt160>();

        private Settings(IConfigurationSection section)
        {
            this.Path = string.Format(section.GetSection("Path").Value, ProtocolSettings.Default.Magic.ToString("X8"));
            this.Url = section.GetSection("URL").Value;
            this.WalletPath = section.GetSection("WalletPath").Value;
            this.Password = section.GetSection("Password").Value;

            IConfigurationSection contracts = section.GetSection("contracts");
            this.NetmapContractHash = UInt160.Parse(contracts.GetSection("netmap").Value);
            this.FsContractHash = UInt160.Parse(contracts.GetSection("neofs").Value);
            this.FsIdContractHash = UInt160.Parse(contracts.GetSection("neofsId").Value);
            this.BalanceContractHash = UInt160.Parse(contracts.GetSection("balance").Value);
            this.ContainerContractHash = UInt160.Parse(contracts.GetSection("container").Value);
            int alphabetContractCount = int.Parse(contracts.GetSection("alphabet").Value);
            UInt160[] hashes = new UInt160[alphabetContractCount];
            for (int i = 0; i < alphabetContractCount; i++)
            {
                hashes[i] = UInt160.Parse(contracts.GetSection("alphabet" + i).Value);
            }
            this.AlphabetContractHash = hashes;
            Contracts.Add(NetmapContractHash);
            Contracts.Add(FsContractHash);
            Contracts.Add(FsIdContractHash);
            Contracts.Add(BalanceContractHash);
            Contracts.Add(ContainerContractHash);
            Contracts.AddRange(AlphabetContractHash);

            IConfigurationSection workSizes = section.GetSection("workers");
            this.NetmapContractWorkersSize = int.Parse(workSizes.GetSection("netmap").Value);
            this.FsContractWorkersSize = int.Parse(workSizes.GetSection("neofs").Value);
            this.BalanceContractWorkersSize = int.Parse(workSizes.GetSection("balance").Value);
            this.ContainerContractWorkersSize = int.Parse(workSizes.GetSection("container").Value);
            this.AlphabetContractWorkersSize = int.Parse(workSizes.GetSection("alphabet").Value);

            IConfigurationSection timers = section.GetSection("timers");
            this.EpochDuration = long.Parse(timers.GetSection("epoch").Value);
            this.AlphabetDuration = long.Parse(timers.GetSection("emit").Value);

            IConfigurationSection emit = section.GetSection("emit");
            this.MintEmitCacheSize = int.Parse(emit.GetSection("mint").GetSection("cache_size").Value);
            this.MintEmitThreshold = ulong.Parse(emit.GetSection("mint").GetSection("threshold").Value);
            this.MintEmitValue = long.Parse(emit.GetSection("mint").GetSection("value").Value);
            this.StorageEmission = ulong.Parse(emit.GetSection("storage").GetSection("amount").Value);

            IConfigurationSection netmapCleaner = section.GetSection("netmap_cleaner");
            this.CleanupEnabled = bool.Parse(netmapCleaner.GetSection("enabled").Value);
            this.CleanupThreshold = ulong.Parse(netmapCleaner.GetSection("threshold").Value);

            this.IsSender = bool.Parse(section.GetSection("isSender").Value);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
