using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.FSStorage.morph.invoke;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;

namespace Neo.Plugins.FSStorage.innerring.invoke
{
    public class MainClient : Client
    {
        private string url;
        private Wallets.Wallet wallet;

        public Wallet Wallet { get => wallet; set => wallet = value; }

        public bool InvokeFunction(UInt160 contractHash, string method, long fee, params object[] args)
        {
            return true;
        }


        /*
        {
          "jsonrpc": "2.0",
          "method": "invokefunction",
          "params": [
            "af7c7328eee5a275a3bcaee2bf0cf662b5e739be",
            "balanceOf",
            [
              {
                "type": "Hash160",
                "value": "91b83e96f2a7c4fdf0c1688441ec61986c7cae26"
              }
            ]
          ],
          "id": 3
        }
        */
        public InvokeResult InvokeLocalFunction(UInt160 contractHash, string method, params object[] args)
        {
            byte[] script = contractHash.MakeScript(method, args);
            IEnumerable<WalletAccount> accounts = Wallet.GetAccounts();
            Signer[] signers = new Signer[] { new Signer() { Account = Wallet.GetAccounts().ToArray()[0].ScriptHash, Scopes = WitnessScope.Global } };
            List<JObject> parameters = new List<JObject> { script.ToHexString() };
            parameters.Add(signers.Select(p => p.ToJson()).ToArray());

            var json = new JObject();
            json["id"] = 1;
            json["jsonrpc"] = "2.0";
            json["method"] = "invokescript";
            json["params"] = new JArray(parameters);

            var result = MakeHttpRequest(json.ToString());
            var resultObj = JObject.Parse(result);

            var invokeResult = new InvokeResult();
            invokeResult.Script = resultObj["script"].AsString().HexToBytes();
            invokeResult.State = resultObj["state"].TryGetEnum<VM.VMState>();
            invokeResult.GasConsumed = (long)BigInteger.Parse(resultObj["gasconsumed"].AsString());
            try
            {
                invokeResult.ResultStack = ((JArray)json["stack"]).Select(p => StackItemFromJson(p)).ToArray();
            }
            catch { }
            return invokeResult;

        }

        public StackItem StackItemFromJson(JObject json)
        {
            StackItemType type = json["type"].TryGetEnum<StackItemType>();
            switch (type)
            {
                case StackItemType.Boolean:
                    return new VM.Types.Boolean(json["value"].AsBoolean());
                case StackItemType.Buffer:
                    return new VM.Types.Buffer(Convert.FromBase64String(json["value"].AsString()));
                case StackItemType.ByteString:
                    return new ByteString(Convert.FromBase64String(json["value"].AsString()));
                case StackItemType.Integer:
                    return new Integer(new System.Numerics.BigInteger(json["value"].AsNumber()));
                case StackItemType.Array:
                    VM.Types.Array array = new VM.Types.Array();
                    foreach (var item in (JArray)json["value"])
                        array.Add(StackItemFromJson(item));
                    return array;
                case StackItemType.Struct:
                    Struct @struct = new Struct();
                    foreach (var item in (JArray)json["value"])
                        @struct.Add(StackItemFromJson(item));
                    return @struct;
                case StackItemType.Map:
                    Map map = new Map();
                    foreach (var item in (JArray)json["value"])
                    {
                        PrimitiveType key = (PrimitiveType)StackItemFromJson(item["key"]);
                        map[key] = StackItemFromJson(item["value"]);
                    }
                    return map;
                case StackItemType.Pointer:
                    return new Pointer(null, (int)json["value"].AsNumber());
                case StackItemType.InteropInterface:
                    return new InteropInterface(new object()); // See https://github.com/neo-project/neo/blob/master/src/neo/VM/Helper.cs#L194
            }
            return null;
        }

        public string MakeHttpRequest(String jsonParemter)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://url");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonParemter);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }

        }
    }
}
