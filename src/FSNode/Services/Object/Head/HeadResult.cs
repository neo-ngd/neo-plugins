using V2Object = NeoFS.API.v2.Object;

namespace Neo.Fs.Services.Object.Head
{
    public class HeadResult
    {
        public V2Object.Object Header;
        public V2Object.Object RightChild;
    }
}
