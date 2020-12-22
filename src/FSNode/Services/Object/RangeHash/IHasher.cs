
namespace Neo.Fs.Services.Object.RangeHash
{
    public interface IHasher
    {
        void Add(byte[] data);
        byte[] Sum();
    }
}
