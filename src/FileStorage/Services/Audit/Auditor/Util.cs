using System;

namespace Neo.FileStorage.Services.Audit.Auditor
{
    public static class Util
    {
        public static ulong RandomUInt64(ulong max = ulong.MaxValue)
        {
            var random = new Random();
            var buffer = new byte[sizeof(ulong)];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer) % max;
        }
    }
}
