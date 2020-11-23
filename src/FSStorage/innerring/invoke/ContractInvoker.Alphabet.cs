using Neo.Plugins.FSStorage.morph.invoke;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public partial class ContractInvoker
    {
        private static UInt160[] AlphabetContractHash => Settings.Default.AlphabetContractHash;
        private const string EmitMethod = "emit";

        public static bool AlphabetEmit(Client client, int index)
        {
            return client.InvokeFunction(AlphabetContractHash[index], EmitMethod, 0);
        }
    }
}