using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins.innerring.processors
{
    public interface IIndexer
    {
        public int Index();
        public void SetIndexer(int index);
    }
}
