using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common;

namespace DecodeTool {
    public class Types {
        public static TypeDescription StringType = new StringDescription();
        public static TypeDescription IntType = new IntDescription();
        public static TypeDescription ShortType = new ShortDescription();
        public static TypeDescription BoolType = new BoolDescription();
        public static TypeDescription SingleType = new SingleDescription();
        public static TypeDescription OptStringType = new OptStringDescription();
    }

    public abstract class TypeDescription {
        public string TypeName { get; set; }

        public abstract string Decode(BinaryReader reader);
        public abstract int GetLength(string val);
    }

    class StringDescription : TypeDescription {
        public StringDescription() { TypeName = "String"; }
        public override int GetLength(string str) { return 2*str.Length+2; }
        public override string Decode(BinaryReader reader) { return IOFunctions.readCAString(reader); }
    }
    abstract class FixedLengthTypeDescription : TypeDescription {
        protected int length;
        public override int GetLength(string str) { return length; }
    }
    class IntDescription : FixedLengthTypeDescription {
        public IntDescription() { TypeName = "UInt32"; length = 4; }
        public override string Decode(BinaryReader reader) { return reader.ReadUInt32().ToString(); }
    }
    class ShortDescription : FixedLengthTypeDescription {
        public ShortDescription() { TypeName = "Short"; length = 2; }
        public override string Decode(BinaryReader reader) { return reader.ReadUInt16().ToString(); }
    }
    class SingleDescription : FixedLengthTypeDescription {
        public SingleDescription() { TypeName = "Single"; length = 4; }
        public override string Decode(BinaryReader reader) { return reader.ReadSingle().ToString(); }
    }
    class BoolDescription : FixedLengthTypeDescription {
        public BoolDescription() { TypeName = "Boolean"; length = 1; }
        public override string Decode(BinaryReader reader) {
            byte b = reader.ReadByte();
            if (b == 0 || b == 1) {
                return Convert.ToBoolean(b).ToString();
            }
            return string.Format("- invalid - ({0:x2})", b);
        }
    }
    class OptStringDescription : TypeDescription {
        public OptStringDescription() { TypeName = "OptString"; }
        public override string Decode(BinaryReader reader) {
            string result = "";
            byte b = reader.ReadByte();
            if (b == 1) {
                result = IOFunctions.readCAString(reader);
            } else if (b != 0) {
                result = string.Format("- invalid - ({0:x2})", b);
            }
            return result;
        }
        public override int GetLength(string str) {
            return 2*(str.Length) + (str.Length == 0 ? 1 : 3);
        }
    }
    class VarBytesDescription : FixedLengthTypeDescription {
        public VarBytesDescription(int byteCount) { TypeName = "Unknown"; length = byteCount; }
        public override string Decode(BinaryReader reader) { return Util.formatHex(reader.ReadBytes(length)); }
    }
}
