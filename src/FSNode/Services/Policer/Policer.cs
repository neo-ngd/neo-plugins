using Neo.Fs.Core.Container;
using Neo.Fs.Network;
using Neo.Fs.Services.ObjectManager.Placement;
using System;
using System.Threading.Tasks;

namespace Neo.Fs.Services.Policer
{
    public class Policer
    {
        public Cfg Cfg { get; set; }
        public PrevTask PrevTask { get; set; }

        public Policer(Option[] opts)
        {
            var c = new Cfg();
            foreach (var opt in opts)
            {
                opt(c);
            }
            this.Cfg = c;
            this.PrevTask = new PrevTask() { Cancel = () => { }, Wait = new Task[0] };
        }
    }

    public delegate void Option(Cfg cfg);

    public class Cfg
    {
        public TimeSpan HeadTimeout { get; set; }
        // trigger

        public JobQueue JobQueue { get; set; }
        public ISource CnrSrc { get; set; }
        public IBuilder PlacementBuilder { get; set; }
        // remoteHeader
        public ILocalAddressSource LocalAddrSrc { get; set; }
        public Replicator.Replicator Replicator { get; set; }
    }

    public class WorkScope
    {
        public int Val { get; set; }
        public int ExpRate { get; set; } // in %
    }

    public class PrevTask
    {
        public int Undone { get; set; }
        public CancelFunc Cancel { get; set; }
        public Task[] Wait { get; set; } // WaitGroup
    }

    public delegate void CancelFunc();
}
