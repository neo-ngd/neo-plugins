namespace Neo.Plugins.FSStorage
{
    public class Utils
    {
        public class ScriptHashWithType
        {
            private UInt160 scriptHashValue;
            private string type;

            public UInt160 ScriptHashValue { get => scriptHashValue; set => scriptHashValue = value; }
            public string Type { get => type; set => type = value; }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                ScriptHashWithType oth = (ScriptHashWithType)obj;
                if (this == oth) return true;
                if (oth.Type is null && oth.ScriptHashValue is null) return false;
                if (this.Type is null && oth.Type is null) return this.ScriptHashValue.Equals(oth.ScriptHashValue);
                if (this.ScriptHashValue is null && oth.ScriptHashValue is null) return this.Type.Equals(oth.Type);
                return this.Type.Equals(oth.Type) && this.ScriptHashValue.Equals(oth.ScriptHashValue);
            }

            public override int GetHashCode()
            {
                return scriptHashValue.GetHashCode() + type.GetHashCode();
            }

            public override string ToString()
            {
                return base.ToString();
            }
        }
    }
}
