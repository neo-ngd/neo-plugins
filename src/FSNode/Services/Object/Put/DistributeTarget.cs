using Google.Protobuf;
using V2Object = NeoFS.API.v2.Object.Object;
using Neo.Fs.Core.Object;
using System;

namespace Neo.Fs.Services.Object.Put
{
    public class DistributeTarget : ValidatingTarget
    {
        //travers options
        private V2Object obj;


        public DistributeTarget(FormatValidator validator) : base(validator) { }

        public override void PutInit(V2Object init)
        {
            base.PutInit(init);
            obj = init;
        }

        public override PutResult PutPayload(ByteString payload)
        {
            base.PutPayload(payload);
            //TODO: travers init
            if (!objectValidator.ValidateContent(obj.Header.ObjectType, payload))
                throw new InvalidOperationException(nameof(PutPayload) + " invalid content");
            obj.Payload = payload;
            //TODO: travers and workpool add
            return new PutResult();
        }
    }
}
