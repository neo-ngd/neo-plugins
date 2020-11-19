using Microsoft.Extensions.Configuration;

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

        private Settings(IConfigurationSection section)
        {
            this.Path = string.Format(section.GetSection("Path").Value, ProtocolSettings.Default.Magic.ToString("X8"));
            this.Url = section.GetSection("URL").Value;
            this.WalletPath = section.GetSection("WalletPath").Value;
            this.Password = section.GetSection("Password").Value;
            this.NetmapContractHash = UInt160.Parse(section.GetSection("contracts.netmap").Value);
            this.FsContractHash = UInt160.Parse(section.GetSection("contracts.neofs").Value);
            this.FsIdContractHash = UInt160.Parse(section.GetSection("contracts.neofsId").Value);
            this.BalanceContractHash = UInt160.Parse(section.GetSection("contracts.balance").Value);
            this.ContainerContractHash = UInt160.Parse(section.GetSection("contracts.container").Value);
            int alphabetContractCount = int.Parse(section.GetSection("contracts.alphabet").Value);
            UInt160[] hashes = new UInt160[alphabetContractCount];
            for (int i = 0; i < alphabetContractCount; i++)
            {
                hashes[i] = UInt160.Parse(section.GetSection("contracts.alphabet" + i).Value);
            }
            this.AlphabetContractHash = hashes;
            this.NetmapContractWorkersSize = int.Parse(section.GetSection("workers.netmap").Value);
            this.FsContractWorkersSize = int.Parse(section.GetSection("workers.neofs").Value);
            this.BalanceContractWorkersSize = int.Parse(section.GetSection("workers.balance").Value);
            this.ContainerContractWorkersSize = int.Parse(section.GetSection("workers.container").Value);
            this.AlphabetContractWorkersSize = int.Parse(section.GetSection("workers.alphabet").Value);
            this.EpochDuration = long.Parse(section.GetSection("timers.epoch").Value);
            this.AlphabetDuration = long.Parse(section.GetSection("timers.emit").Value);

            this.MintEmitCacheSize = int.Parse(section.GetSection("emit.mint.cache_size").Value);
            this.MintEmitThreshold = ulong.Parse(section.GetSection("emit.mint.threshold").Value);
            this.MintEmitValue = long.Parse(section.GetSection("emit.mint.value").Value);
            this.StorageEmission = ulong.Parse(section.GetSection("emit.storage.amount").Value);

            this.CleanupEnabled = bool.Parse(section.GetSection("netmap_cleaner.enabled").Value);
            this.CleanupThreshold = ulong.Parse(section.GetSection("netmap_cleaner.threshold").Value);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
