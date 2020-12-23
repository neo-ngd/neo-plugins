using Google.Protobuf;
using V2Object = NeoFS.API.v2.Object.Object;
using NeoFS.API.v2.Refs;
using Neo.FSNode.Core.Object;
using System;

namespace Neo.FSNode.Services.Object.Put
{
    public abstract class ValidatingTarget : IPutTarget
    {
        protected FormatValidator objectValidator;
        protected Checksum checksum;
        protected bool initReceived;

        public ValidatingTarget(FormatValidator validator)
        {
            objectValidator = validator;
        }

        public virtual void PutInit(V2Object init)
        {
            checksum = init.Header.PayloadHash;
            if (!(checksum.Type == ChecksumType.Sha256 || checksum.Type == ChecksumType.Tz))
                throw new InvalidOperationException(nameof(PutInit) + " unsupported paylaod checksum type " + checksum.Type);
            if (!objectValidator.Validate(init))
                throw new FormatException(nameof(PutInit) + " invalid object");
            initReceived = true;
        }

        public virtual PutResult PutPayload(ByteString payload)
        {
            if (!initReceived) throw new InvalidOperationException(nameof(PutPayload) + " missing init");
            if (!checksum.Verify(payload)) throw new InvalidOperationException(nameof(PutPayload) + " invalid checksum");
            return null;
        }
    }
}
