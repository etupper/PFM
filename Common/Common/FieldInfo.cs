namespace Common
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("Name = {Name}; Type = {Type}; Modifier = {Mod}")]
    public class FieldInfo
    {
        public int length;
        public Modifier modifier;
        public string name;
        public PackTypeCode type;

        public FieldInfo(string name, string type)
        {
            this.name = name;
            this.type = PackTypeCode.Empty;
            this.length = 0;
            this.modifier = Modifier.None;
            switch (type.ToLowerInvariant())
            {
                case "string":
                    this.type = PackTypeCode.String;
                    break;

                case "stringcontainer":
                    this.type = PackTypeCode.StringContainer;
                    break;

                case "boolean":
                    this.type = PackTypeCode.Boolean;
                    break;

                case "uint8":
                    this.type = PackTypeCode.Byte;
                    break;

                case "short":
                case "uint16":
                    this.type = PackTypeCode.UInt16;
                    break;

                case "uint32":
                    this.type = PackTypeCode.UInt32;
                    break;

                case "uint64":
                    this.type = PackTypeCode.UInt64;
                    break;

                case "int8":
                    this.type = PackTypeCode.SByte;
                    break;

                case "int16":
                    this.type = PackTypeCode.Int16;
                    break;

                case "int32":
                case "int":
                    this.type = PackTypeCode.Int32;
                    break;

                case "int64":
                    this.type = PackTypeCode.Int64;
                    break;

                case "float":
                case "single":
                    this.type = PackTypeCode.Single;
                    break;

                case "double":
                    this.type = PackTypeCode.Double;
                    break;

                default:
                    if (!int.TryParse(type, out this.length))
                    {
                        throw new ArgumentException("unknown type: " + type);
                    }
                    break;
            }
        }

        public FieldInfo(string name, string type, string modifier)
        {
            this.name = name;
            this.type = PackTypeCode.Empty;
            this.length = 0;
            this.modifier = Modifier.None;
            switch (type)
            {
                case "Boolean":
                    this.type = PackTypeCode.Boolean;
                    break;

                case "UInt16":
                    this.type = PackTypeCode.UInt16;
                    break;

                default:
                    throw new ArgumentException("unknown type or type doesn't support a modifier");
            }
            if (modifier == "*")
            {
                this.modifier = Modifier.NextFieldRepeats;
            }
            else
            {
                if (!int.TryParse(modifier, out this.length))
                {
                    throw new ArgumentException("invalid FieldInfo modifier");
                }
                this.modifier = Modifier.NextFieldIsConditional;
            }
        }

        public string GetConditionString(bool condition)
        {
            if (this.modifier != Modifier.NextFieldIsConditional)
            {
                throw new InvalidOperationException("field is not a conditional");
            }
            PackTypeCode type = this.type;
            if (type != PackTypeCode.Boolean)
            {
                if (type != PackTypeCode.UInt16)
                {
                    throw new InvalidOperationException("invalid type for conditional test");
                }
            }
            else
            {
                return (condition ? "True" : "False");
            }
            return (condition ? "1" : "0");
        }

        public bool TestConditionalValue(string value)
        {
            if (this.modifier != Modifier.NextFieldIsConditional)
            {
                throw new InvalidOperationException("field is not a conditional");
            }
            PackTypeCode type = this.type;
            if (type != PackTypeCode.Boolean)
            {
                if (type != PackTypeCode.UInt16)
                {
                    throw new InvalidOperationException("invalid type for conditional test");
                }
            }
            else
            {
                return Convert.ToBoolean(value);
            }
            return (Convert.ToUInt16(value) > 0);
        }

        public override string ToString()
        {
            return string.Format("Name = {0}; Type = {1}; Modifier = {2}", this.name, this.type, this.modifier);
        }

        public int Length
        {
            get
            {
                return this.length;
            }
        }

        public Modifier Mod
        {
            get
            {
                return this.modifier;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public PackTypeCode Type
        {
            get
            {
                return this.type;
            }
        }

        public enum DBType
        {
            Boolean = 3,
            OptionalString = 0x13,
            RequiredString = 0x12,
            Single = 13,
            UInt16 = 8,
            UInt32 = 10
        }

        public enum Modifier
        {
            None,
            NextFieldIsConditional,
            NextFieldRepeats
        }
    }
}

