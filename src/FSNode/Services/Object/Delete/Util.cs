using Google.Protobuf;
using Google.Protobuf.Collections;
using NeoFS.API.v2.Refs;
using System.Collections.Generic;
using System.IO;

namespace Neo.Fs.Services.Object.Delete
{
    public static class Util
    {
        public static RepeatedField<Address> ToRepeatedField(this IEnumerable<Address> list)
        {
            var repeated = new RepeatedField<Address>();
            repeated.AddRange(list);
            return repeated;
        }

        public static byte[] ToByteArray(this RepeatedField<Address> list)
        {
            using MemoryStream ms = new MemoryStream();
            CodedOutputStream output = new CodedOutputStream(ms);
            list.WriteTo(output, FieldCodec.ForMessage(10, Address.Parser));
            output.Flush();
            return ms.ToArray();
        }
    }
}
