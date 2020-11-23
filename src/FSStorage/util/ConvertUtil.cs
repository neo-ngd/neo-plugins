using System;
using System.Numerics;

namespace Neo.Plugins.util
{
    public class ConvertUtil
    {
        private uint basePrecision;
        private uint targetPrecision;
        private BigInteger factor;

        public uint BasePrecision { get => basePrecision; set => basePrecision = value; }
        public uint TargetPrecision { get => targetPrecision; set => targetPrecision = value; }
        public BigInteger Factor { get => factor; set => factor = value; }

        public BigInteger Convert(BigInteger n, BigInteger factor, bool decreasePrecision)
        {
            if (decreasePrecision)
                return BigInteger.Divide(n, factor);
            return BigInteger.Multiply(n, factor);
        }

        public BigInteger ToBasePrecision(BigInteger n)
        {
            return Convert(n, Factor, BasePrecision < TargetPrecision);
        }

        public BigInteger ToTargetPrecision(BigInteger n)
        {
            return Convert(n, Factor, BasePrecision > TargetPrecision);
        }


        public BigInteger Convert(uint fromPrecision, uint toPrecision, BigInteger n)
        {
            bool decreasePrecision = false;
            var exp = (int)toPrecision - (int)fromPrecision;
            if (exp < 0)
            {
                decreasePrecision = true;
                exp = -exp;
            }
            Factor = new BigInteger(Math.Pow(10, exp));
            return Convert(n, Factor, decreasePrecision);
        }

    }

    public class Fixed8ConverterUtil
    {
        private const uint Fixed8Precision = 8;
        private ConvertUtil converter;

        public Fixed8ConverterUtil()
        {
            converter = new ConvertUtil();
        }

        public Fixed8ConverterUtil(uint precision)
        {
            converter = new ConvertUtil();
            SetBalancePrecision(precision);
        }

        public long ToFixed8(long n)
        {
            return (long)converter.ToBasePrecision(new BigInteger(n));
        }

        public long ToBalancePrecision(long n)
        {
            return (long)converter.ToTargetPrecision(new BigInteger(n));
        }

        public void SetBalancePrecision(uint precision)
        {
            var exp = (int)precision - Fixed8Precision;
            if (exp < 0)
                exp = -exp;
            converter.BasePrecision = Fixed8Precision;
            converter.TargetPrecision = precision;
            converter.Factor = new BigInteger(Math.Pow(10, exp));
        }
    }
}
