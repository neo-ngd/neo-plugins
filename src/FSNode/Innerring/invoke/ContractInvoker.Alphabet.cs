using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Plugins.FSStorage.morph.invoke;
using System.Linq;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160[] AlphabetContractHash => Settings.Default.AlphabetContractHash;
        private const string EmitMethod = "emit";
        private const string VoteMethod = "vote";

        public static bool AlphabetEmit(IClient client, int index)
        {
            return client.InvokeFunction(AlphabetContractHash[index], EmitMethod, 0);
        }
        public static bool AlphabetVote(IClient client, int index, ulong epoch, ECPoint[] publicKeys)
        {
            return client.InvokeFunction(AlphabetContractHash[index], VoteMethod, FeeOneGas, epoch, publicKeys.Select(p => p.ToArray()).ToArray());
        }
    }
}
