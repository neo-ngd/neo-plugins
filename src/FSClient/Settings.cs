using Microsoft.Extensions.Configuration;
using System;

namespace Neo.Plugins
{
    internal class Settings
    {
        public string PrivateKey { get; }
        public string Host { get; }
        public string DefaultFilePath { get; }
        public int DefaultFilePermission { get; }
        public static Settings Default { get; private set; }

        private Settings(IConfigurationSection section)
        {
            this.PrivateKey = GetValueOrDefault(section.GetSection("PrivateKey"), "", p => p);
            this.Host = GetValueOrDefault(section.GetSection("Host"), "localhost:8080", p => p);
            this.DefaultFilePath = GetValueOrDefault(section.GetSection("DefaultFilePath"), ".", p => p);
            this.DefaultFilePermission = GetValueOrDefault(section.GetSection("DefaultFilePermission"), 600, p => int.Parse(p));
        }

        public T GetValueOrDefault<T>(IConfigurationSection section, T defaultValue, Func<string, T> selector)
        {
            if (section.Value == null) return defaultValue;
            return selector(section.Value);
        }

        public static void Load(IConfigurationSection section)
        {
            Default = new Settings(section);
        }
    }
}
