using Neo.ConsoleService;
using System;
using System.IO;
using System.Text;

namespace Neo.Plugins
{
    public partial class FSClient : Plugin
    {
        const uint SingleForwardedTTL = 2;

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        protected override void OnPluginsLoaded()
        {

        }
    }
}
