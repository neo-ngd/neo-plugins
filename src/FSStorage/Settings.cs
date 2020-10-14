using Microsoft.Extensions.Configuration;
using System;

namespace Neo.Plugins
{
    internal class Settings
    {
        public string PrivateKey { get; }
        public string Path { get; }

        public UInt160 NetmapContractHash { get; }
        public UInt160 FsContractHash { get; }
        public UInt160 BalanceContractHash { get; }
        public UInt160 ContainerContractHash { get; }

        public int NetmapContractWorkersSize { get; }
        public int FsContractWorkersSize { get; }
        public int BalanceContractWorkersSize { get; }
        public int ContainerContractWorkersSize { get; }

        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section)
        {
            this.Path = string.Format(section.GetSection("Path").Value, ProtocolSettings.Default.Magic.ToString("X8"));
            this.PrivateKey = section.GetSection("PrivateKey").Value;
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
