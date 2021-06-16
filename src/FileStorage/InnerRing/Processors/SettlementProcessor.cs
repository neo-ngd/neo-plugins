using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Akka.Actor;
using Neo.FileStorage.API.Audit;
using Neo.FileStorage.API.Container;
using Neo.FileStorage.API.Netmap;
using Neo.FileStorage.API.Refs;
using Neo.FileStorage.Morph.Event;
using static Neo.FileStorage.InnerRing.Events.MorphEvent;
using static Neo.FileStorage.InnerRing.Processors.SettlementProcessor.IncomeSettlementContext;
using static Neo.FileStorage.Morph.Invoker.MorphContractInvoker;
using static Neo.FileStorage.Utils.WorkerPool;

namespace Neo.FileStorage.InnerRing.Processors
{
    public class SettlementProcessor : BaseProcessor
    {
        public override string Name => "SettlementProcessor";
        private readonly Dictionary<ulong, IncomeSettlementContext> incomeContexts = new();
        public BasicIncomeSettlementDeps basicIncome;
        public Calculator auditProc;

        public void Handle(ulong epoch)
        {
            Utility.Log(Name, LogLevel.Info, "process audit settlements");
            auditProc.Calculate(epoch);
            Utility.Log(Name, LogLevel.Info, "audit processing finished");
        }

        public void HandleAuditEvent(IContractEvent morphEvent)
        {
            Console.WriteLine("执行HandleAuditEvent");
            AuditStartEvent auditEvent = (AuditStartEvent)morphEvent;
            var epoch = auditEvent.epoch;
            Utility.Log(Name, LogLevel.Info, string.Format("new audit settlement event,epoch:{0}", epoch));
            if (epoch == 0)
            {
                Utility.Log(Name, LogLevel.Info, "ignore genesis epoch");
                return;
            }
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => Handle(epoch)) });
            Utility.Log(Name, LogLevel.Info, "AuditEvent handling successfully scheduled");
        }

        public void HandleIncomeCollectionEvent(IContractEvent morphEvent)
        {
            BasicIncomeCollectEvent basicIncomeCollectEvent = (BasicIncomeCollectEvent)morphEvent;
            var epoch = basicIncomeCollectEvent.epoch;
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore income collection event");
                return;
            }
            Utility.Log(Name, LogLevel.Info, string.Format("start basic income collection,epoch:{0}", epoch));
            if (incomeContexts.TryGetValue(epoch, out _))
            {
                Utility.Log(Name, LogLevel.Error, string.Format("income context already exists,epoch:{0}", epoch));
                return;
            }
            Console.WriteLine("构建IncomeSettlementContext上下文");
            IncomeSettlementContext incomeCtx = new IncomeSettlementContext() { settlementDeps = basicIncome, epoch = epoch };
            incomeContexts[epoch] = incomeCtx;
            Console.WriteLine("发送basic income collection任务");
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => incomeCtx.Collect()) });
        }

        public void HandleIncomeDistributionEvent(IContractEvent morphEvent)
        {
            BasicIncomeDistributeEvent basicIncomeDistributeEvent = (BasicIncomeDistributeEvent)morphEvent;
            var epoch = basicIncomeDistributeEvent.epoch;
            if (!State.IsAlphabet())
            {
                Utility.Log(Name, LogLevel.Info, "non alphabet mode, ignore income distribution event");
                return;
            }
            Utility.Log(Name, LogLevel.Info, string.Format("start basic income distribution,epoch:{0}", epoch));
            var flag = incomeContexts.TryGetValue(epoch, out var incomeCtx);
            incomeContexts.Remove(epoch);
            if (!flag)
            {
                Utility.Log(Name, LogLevel.Info, string.Format("income context distribution does not exists,epoch:{0}", epoch));
                return;
            }
            WorkPool.Tell(new NewTask() { Process = Name, Task = new Task(() => incomeCtx.Distribute()) });
        }

        public class IncomeSettlementContext
        {
            private object lockObject = new();
            public BasicIncomeSettlementDeps settlementDeps;
            public ulong epoch;
            public OwnerID bankOwner;
            public NodeSizeTable distributeTable;

            public OwnerID BankOwnerID()
            {
                return OwnerID.FromBase58String(new UInt160(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }).ToString());
            }

            public void Collect()
            {
                lock (lockObject)
                {
                    Console.WriteLine("执行Collect");
                    var cachedRate = settlementDeps.BasicRate;
                    var cnrEstimations = settlementDeps.Estimations(epoch);
                    var txTable = new TransferTable();
                    Console.WriteLine("创建TransferTable");
                    foreach (var item in cnrEstimations)
                    {
                        OwnerID owner = settlementDeps.ContainerInfo(item.ContainerID).OwnerId;
                        NodeInfo[] cnrNodes = settlementDeps.ContainerNodes(epoch, item.ContainerID);
                        ulong avg = AvgEstimation(item);
                        BigInteger total = CalculateBasicSum(avg, cachedRate, cnrNodes.Length);
                        foreach (var node in cnrNodes)
                            distributeTable.Put(node.PublicKey(), avg);
                        Console.WriteLine("创建交易缓存");
                        txTable.Transfer(new TransferTable.TransferTx() { from = owner, to = BankOwnerID(), amount = total });
                    }
                }
            }

            public ulong AvgEstimation(Estimations e)
            {
                ulong avg = 0;
                if (e.AllEstimation.Count == 0) return avg;
                foreach (var estimation in e.AllEstimation)
                    avg += estimation.Size;
                return avg / (ulong)e.AllEstimation.Count;
            }

            public BigInteger CalculateBasicSum(ulong size, ulong rate, int ln)
            {
                BigInteger bigRate = rate;
                ulong total = size * (ulong)ln;
                BigInteger price = new BigInteger(total);
                price = BigInteger.Multiply(price, bigRate);
                price = BigInteger.Divide(price, bigGB);
                if (price.CompareTo(bigZero) == 0)
                    price = BigInteger.Add(price, bigOne);
                return price;
            }

            public void Distribute()
            {
                lock (lockObject)
                {
                    var txTable = new TransferTable();
                    BigInteger bankBalance = settlementDeps.Balance(bankOwner);
                    BigInteger total = distributeTable.Total();
                    distributeTable.Iterate((byte[] key, BigInteger n) =>
                    {
                        var nodeOwner = settlementDeps.ResolveKey(new BasicNodeInfoWrapper(key));
                        txTable.Transfer(new TransferTable.TransferTx() { from = bankOwner, to = nodeOwner, amount = NormalizedValue(n, total, bankBalance) });
                    });
                    TransferTable.TransferAssets(settlementDeps, txTable);
                }
            }

            public BigInteger NormalizedValue(BigInteger n, BigInteger total, BigInteger limit)
            {
                if (limit.CompareTo(bigZero) == 0) return 0;
                n = BigInteger.Multiply(n, limit);
                return BigInteger.Divide(n, total);
            }

            public class TransferTable
            {
                private Dictionary<string, Dictionary<string, TransferTx>> txs = new();

                public void Transfer(TransferTx tx)
                {
                    var from = tx.from.ToBase58String();
                    var to = tx.to.ToBase58String();
                    if (from == to) return;
                    if (!txs.TryGetValue(from, out var m))
                    {
                        if (!txs.TryGetValue(to, out m))
                        {
                            to = from;
                        }
                        else
                        {
                            m = new Dictionary<string, TransferTx>();
                            txs[from] = m;
                        }
                    }
                    if (!m.TryGetValue(to, out var tgt))
                    {
                        m[to] = tx;
                        return;
                    }
                    tgt.amount += tx.amount;
                }

                public void Iterate(Action<TransferTx> f)
                {
                    foreach (var m in txs)
                        foreach (var tx in m.Value)
                            f(tx.Value);
                }

                public static void TransferAssets(SettlementDeps e, TransferTable t)
                {
                    t.Iterate((TransferTx tx) =>
                    {
                        var sign = tx.amount.Sign;
                        if (sign == 0) return;
                        if (sign < 0)
                        {
                            OwnerID temp = tx.from;
                            tx.from = tx.to;
                            tx.to = temp;
                            tx.amount = BigInteger.Abs(tx.amount);
                        }
                        e.Transfer(tx.from, tx.to, tx.amount);
                    });
                }

                public class TransferTx
                {
                    public OwnerID from;
                    public OwnerID to;
                    public BigInteger amount;
                }
            }

            public class NodeSizeTable
            {
                private Dictionary<string, ulong> price = new();
                private ulong total;

                public void Put(byte[] id, ulong avg)
                {
                    price[System.Text.Encoding.UTF8.GetString(id)] += avg;
                    total += avg;
                }

                public BigInteger Total() => total;

                public void Iterate(Action<byte[], BigInteger> f)
                {
                    foreach (var item in price)
                        f(System.Text.Encoding.UTF8.GetBytes(item.Key), item.Value);
                }
            }
        }

        public class Calculator
        {
            private SettlementDeps settlementDeps;

            public Calculator(SettlementDeps settlementDeps)
            {
                this.settlementDeps = settlementDeps;
            }

            public void Calculate(ulong epoch)
            {
                Utility.Log("Calculator", LogLevel.Info, string.Format("current epoch,{0}", epoch));
                if (epoch == 0)
                {
                    Utility.Log("Calculator", LogLevel.Info, "settlements are ignored for zero epoch");
                    return;
                }
                Utility.Log("Calculator", LogLevel.Info, "calculate audit settlements");
                Utility.Log("Calculator", LogLevel.Debug, "getting results for the previous epoch");
                List<DataAuditResult> auditResults;
                try
                {
                    auditResults = settlementDeps.AuditResultsForEpoch(epoch - 1);
                }
                catch
                {
                    Utility.Log("Calculator", LogLevel.Debug, "could not collect audit results");
                    return;
                }
                if (auditResults.Count == 0)
                {
                    Utility.Log("Calculator", LogLevel.Debug, "no audit results in previous epoch");
                    return;
                }
                Utility.Log("Calculator", LogLevel.Debug, string.Format("processing audit results,number:{0}", auditResults.Count));
                var table = new TransferTable();
                foreach (var auditResult in auditResults)
                {
                    ProcessResult(new SingleResultCtx()
                    {
                        auditResult = auditResult,
                        txTable = table
                    });
                }
                Utility.Log("Calculator", LogLevel.Debug, "processing transfers");
                TransferTable.TransferAssets(settlementDeps, table);
            }

            public void ProcessResult(SingleResultCtx ctx)
            {
                Utility.Log("Calculator", LogLevel.Debug, string.Format("cid:{0},audit epoch:{1}", ctx.cid.ToBase58String(), ctx.auditResult.AuditEpoch));
                Utility.Log("Calculator", LogLevel.Debug, "reading information about the container");
                if (!ReadContainerInfo(ctx)) return;
                Utility.Log("Calculator", LogLevel.Debug, "building placement");
                if (!BuildPlacement(ctx)) return;
                Utility.Log("Calculator", LogLevel.Debug, "collecting passed nodes");
                if (!CollectPassNodes(ctx)) return;
                Utility.Log("Calculator", LogLevel.Debug, "calculating sum of the sizes of all storage groups");
                if (!SumSGSizes(ctx)) return;
                Utility.Log("Calculator", LogLevel.Debug, "filling transfer table");
                if (!FillTransferTable(ctx)) return;
            }

            public bool ReadContainerInfo(SingleResultCtx ctx)
            {
                try
                {
                    ctx.cnrInfo = settlementDeps.ContainerInfo(ctx.auditResult.ContainerId);
                }
                catch (Exception e)
                {
                    Utility.Log("Calculator", LogLevel.Error, string.Format("could not get container info,error:{0}", e.Message));
                    return false;
                }
                return true;
            }

            public bool BuildPlacement(SingleResultCtx ctx)
            {
                try
                {
                    settlementDeps.ContainerNodes(ctx.eAudit, ctx.auditResult.ContainerId);
                    var empty = ctx.cnrNodes.Length == 0;
                    Utility.Log("Calculator", LogLevel.Debug, "empty list of container nodes");
                    return !empty;
                }
                catch (Exception e)
                {
                    Utility.Log("Calculator", LogLevel.Error, string.Format("could not get container nodes,error:{0}", e.Message));
                    return false;
                }
            }

            public bool CollectPassNodes(SingleResultCtx ctx)
            {
                ctx.passNodes = new Dictionary<string, NodeInfo>();
                bool loopflag = false;
                foreach (var cnrNode in ctx.cnrNodes)
                {
                    foreach (var passNode in ctx.auditResult.PassNodes)
                    {
                        if (!cnrNode.PublicKey().SequenceEqual(passNode.ToByteArray())) continue;
                        foreach (var failNode in ctx.auditResult.FailNodes)
                        {
                            if (cnrNode.PublicKey().SequenceEqual(failNode.ToByteArray()))
                            {
                                loopflag = true;
                                break;
                            }
                        }
                        if (loopflag) break;
                        ctx.passNodes[passNode.ToByteArray().ToHexString()] = cnrNode;
                    }
                }
                if (ctx.passNodes.Count == 0)
                {
                    Utility.Log("Calculator", LogLevel.Error, "none of the container nodes passed the audit");
                    return false;
                }
                return true;

            }

            public bool SumSGSizes(SingleResultCtx ctx)
            {
                var passedSG = ctx.auditResult.PassSg;
                if (passedSG.Count == 0)
                {
                    Utility.Log("Calculator", LogLevel.Debug, "empty list of passed SG");
                    return false;
                }
                ulong sumPassSGSize = 0;
                API.Refs.Address address = new API.Refs.Address();
                address.ContainerId = ctx.cid;
                foreach (var sgID in ctx.auditResult.PassSg)
                {
                    address.ObjectId = sgID;
                    var sgInfo = settlementDeps.SGInfo(address);
                    sumPassSGSize += sgInfo.ValidationDataSize;
                }
                if (sumPassSGSize == 0)
                {
                    Utility.Log("Calculator", LogLevel.Debug, "zero sum SG size");
                    return false;
                }
                ctx.sumSGSize = sumPassSGSize;
                return true;
            }

            public bool FillTransferTable(SingleResultCtx ctx)
            {
                var cnrOwner = ctx.cnrInfo.OwnerId;
                foreach (var item in ctx.passNodes)
                {
                    OwnerID ownerID;
                    try
                    {
                        ownerID = settlementDeps.ResolveKey(item.Value);
                    }
                    catch (Exception e)
                    {
                        Utility.Log("Calculator", LogLevel.Error, string.Format("could not resolve public key of the storage node,key:{0},error:{1}", item.Key, e.Message));
                        return false;
                    }
                    var price = item.Value.Price();
                    Utility.Log("Calculator", LogLevel.Debug, string.Format("calculating storage node salary for audit (GASe-12),sum SG size:{0},price:{1}", ctx.sumSGSize, price));
                    var fee = BigInteger.Multiply(price, ctx.sumSGSize);
                    fee = BigInteger.Divide(fee, BigInteger.One);
                    if (fee.CompareTo(BigInteger.Zero) == 0) fee = BigInteger.Add(fee, BigInteger.One);
                    ctx.txTable.Transfer(new TransferTable.TransferTx() { from = cnrOwner, to = ownerID, amount = fee });
                }
                var auditIR = OwnerID.Parser.ParseFrom(ctx.auditResult.PublicKey);
                ctx.txTable.Transfer(new TransferTable.TransferTx() { from = cnrOwner, to = auditIR, amount = ctx.auditFee });
                return false;
            }

            public class SingleResultCtx
            {
                public ulong eAudit;
                public DataAuditResult auditResult;
                public ContainerID cid;
                public TransferTable txTable;
                public Container cnrInfo;
                public NodeInfo[] cnrNodes;
                public Dictionary<string, NodeInfo> passNodes = new();
                public BigInteger sumSGSize;
                public BigInteger auditFee;
            }
        }
    }

    public interface NodeInfo
    {
        public BigInteger Price();
        public byte[] PublicKey();
    }
    public class BasicNodeInfoWrapper : NodeInfo
    {
        private byte[] n;

        public BasicNodeInfoWrapper(byte[] n)
        {
            this.n = n;
        }

        BigInteger NodeInfo.Price()
        {
            throw new Exception("should not be used");
        }

        byte[] NodeInfo.PublicKey()
        {
            return n;
        }
    }
    public class NormalNodeInfoWrapper : NodeInfo
    {
        private Node ni;

        public NormalNodeInfoWrapper(Node ni)
        {
            this.ni = ni;
        }

        public BigInteger Price()
        {
            return ni.Price;
        }

        public byte[] PublicKey()
        {
            return ni.PublicKey;
        }
    }
}
