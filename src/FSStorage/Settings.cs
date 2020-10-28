using Microsoft.Extensions.Configuration;
using System;

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
        private UInt160 balanceContractHash;
        private UInt160 containerContractHash;

        private int netmapContractWorkersSize;
        private int fsContractWorkersSize;
        private int balanceContractWorkersSize;
        private int containerContractWorkersSize;

        public static Settings Default { get; private set; }
        public string WalletPath { get => walletPath; set => walletPath = value; }
        public string Password { get => password; set => password = value; }
        public string Path { get => path; set => path = value; }
        public UInt160 NetmapContractHash { get => netmapContractHash; set => netmapContractHash = value; }
        public UInt160 FsContractHash { get => fsContractHash; set => fsContractHash = value; }
        public UInt160 BalanceContractHash { get => balanceContractHash; set => balanceContractHash = value; }
        public UInt160 ContainerContractHash { get => containerContractHash; set => containerContractHash = value; }
        public int NetmapContractWorkersSize { get => netmapContractWorkersSize; set => netmapContractWorkersSize = value; }
        public int FsContractWorkersSize { get => fsContractWorkersSize; set => fsContractWorkersSize = value; }
        public int BalanceContractWorkersSize { get => balanceContractWorkersSize; set => balanceContractWorkersSize = value; }
        public int ContainerContractWorkersSize { get => containerContractWorkersSize; set => containerContractWorkersSize = value; }
        public string Url { get => url; set => url = value; }

        private Settings(IConfigurationSection section)
        {
            this.Path = string.Format(section.GetSection("Path").Value, ProtocolSettings.Default.Magic.ToString("X8"));
            this.Url = section.GetSection("URL").Value;
            this.WalletPath = section.GetSection("WalletPath").Value;
            this.Password = section.GetSection("Password").Value;
            this.NetmapContractHash = UInt160.Parse(section.GetSection("contracts.netmap").Value);
            this.FsContractHash = UInt160.Parse(section.GetSection("contracts.neofs").Value);
            this.BalanceContractHash = UInt160.Parse(section.GetSection("contracts.balance").Value);
            this.ContainerContractHash = UInt160.Parse(section.GetSection("contracts.container").Value);

            this.NetmapContractWorkersSize = int.Parse(section.GetSection("workers.netmap").Value);
            this.FsContractWorkersSize = int.Parse(section.GetSection("workers.neofs").Value);
            this.BalanceContractWorkersSize = int.Parse(section.GetSection("workers.balance").Value);
            this.ContainerContractWorkersSize = int.Parse(section.GetSection("workers.container").Value);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
