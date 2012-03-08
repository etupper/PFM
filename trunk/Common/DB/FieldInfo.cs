﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Common
{
    /*
     * Utility class for db types.
     */
	public class Types {
        // return FieldInfo corresponding to the given type name
		public static FieldInfo FromTypeName(string typeName) {
			switch (typeName) {
			case "string":
				return new StringType ();
			case "optstring":
				return new OptStringType ();
			case "int":
				return new IntType ();
			case "short":
				return new ShortType ();
			case "float":
				return new SingleType ();
			case "boolean":
				return new BoolType ();
			}
			if (typeName.StartsWith ("blob")) {
				string lengthPart = typeName.Substring (4);
				int length = int.Parse (lengthPart);
				return new VarBytesType (length);
			}
			return null;
		}
        #region factory methods
        public static FieldInfo StringType() { return new StringType() { Name = "unknown" }; }
        public static FieldInfo IntType() { return new IntType() { Name = "unknown" }; }
        public static FieldInfo BoolType() { return new BoolType() { Name = "unknown" }; }
        public static FieldInfo OptStringType() { return new OptStringType() { Name = "unknown" };}
        public static FieldInfo SingleType() { return new SingleType() { Name = "unknown" }; }
        #endregion
    }

    /*
     * Base class for field information description item.
     */
	[System.Diagnostics.DebuggerDisplay("{Name} - {TypeName}; {Optional}")]
    public abstract class FieldInfo {
		public string Name {
			get;
			set;
		}
        public bool PrimaryKey { get; set; }
        public bool Optional { get; set; }

        public int StartVersion { get; set; }
        int lastVersion = int.MaxValue;

        public int LastVersion {
            set {
                lastVersion = value;
            }
            get {
                return lastVersion;
            }
        }

        string referenceString = "";
        public string ForeignReference {
            get {
                return referenceString;
            }
            set {
                referenceString = value;
            }
        }

		public string TypeName { get; set; }
		public TypeCode TypeCode { get; set; }

		public abstract int Length(string str);
		public abstract void Encode(BinaryWriter writer, string val);
		public abstract string Decode(BinaryReader reader);
		
		public string DefaultValue {
			get; set;
		}
	}

    /*
     * Type with variable length, containing lenght-encoded Unicode string.
     */
	class StringType : FieldInfo {
		public StringType () {
			TypeName = "string";
			DefaultValue = "";
			TypeCode = TypeCode.String;
		}
		public override int Length(string str) {
			return 2 * str.Length + 2;
		}
		public override string Decode(BinaryReader reader) {
			return IOFunctions.readCAString (reader);
		}
		public override void Encode(BinaryWriter writer, string val) {
			IOFunctions.writeCAString (writer, val);
		}
	}

    /*
     * Base class for fields with constant encoded length.
     */
	abstract class FixedLengthFieldInfo : FieldInfo {
		protected int length;

		public override int Length(string str) {
			return length;
		}
	}

    /*
     * Signed 4 byte integer.
     */
	class IntType : FixedLengthFieldInfo {
		public IntType () {
			TypeName = "int";
			length = 4;
			DefaultValue = "0";
			TypeCode = TypeCode.Int32;
		}

		public override string Decode(BinaryReader reader) {
			return reader.ReadInt32 ().ToString ();
		}
		
		public override void Encode(BinaryWriter writer, string val) {
			writer.Write (int.Parse (val));
		}
	}

    /*
     * Signed 2 byte integer.
     */
	class ShortType : FixedLengthFieldInfo {
		public ShortType () {
			TypeName = "short";
			length = 2;
			DefaultValue = "0";
			TypeCode = TypeCode.Int16;
		}

		public override string Decode(BinaryReader reader) {
			return reader.ReadInt16 ().ToString ();
		}
		public override void Encode(BinaryWriter writer, string val) {
			writer.Write (short.Parse (val));
		}
	}

    /*
     * 4 byte floating point number.
     */
	class SingleType : FixedLengthFieldInfo {
		public SingleType () {
			TypeName = "float";
			length = 4;
			DefaultValue = "0";
			TypeCode = TypeCode.Single;
		}

		public override string Decode(BinaryReader reader) {
			return reader.ReadSingle ().ToString ();
		}
		public override void Encode(BinaryWriter writer, string val) {
			writer.Write (float.Parse (val));
		}
	}

    /*
     * 1 byte boolean (0 = false, 1 = true).
     */
	class BoolType : FixedLengthFieldInfo {
		public BoolType () {
			TypeName = "boolean";
			length = 1;
			DefaultValue = false.ToString ();
			TypeCode = TypeCode.Boolean;
		}

		public override string Decode(BinaryReader reader) {
			byte b = reader.ReadByte ();
			if (b == 0 || b == 1) {
				return Convert.ToBoolean (b).ToString ();
			}
			return string.Format ("- invalid - ({0:x2})", b);
		}
		public override void Encode(BinaryWriter writer, string val) {
			writer.Write (bool.Parse(val));
		}
	}

    /*
     * Nullable string.
     * Basically a string preceeded with a boolean determining whether
     * there is a string at all.
     */
	class OptStringType : FieldInfo {
		public OptStringType () {
			TypeName = "optstring";
			DefaultValue = "";
			TypeCode = TypeCode.String;
		}

		public override string Decode(BinaryReader reader) {
			string result = "";
			byte b = reader.ReadByte ();
			if (b == 1) {
				result = IOFunctions.readCAString (reader);
			} else if (b != 0) {
				result = string.Format ("- invalid - ({0:x2})", b);
			}
			return result;
		}

		public override int Length(string str) {
			return 2 * (str.Length) + (str.Length == 0 ? 1 : 3);
		}
		public override void Encode(BinaryWriter writer, string val) {
			writer.Write (val.Length > 0);
			if (val.Length > 0) {
				IOFunctions.writeCAString (writer, val);
			}
		}
	}

    /*
     * A number of bytes, uninterpreted.
     */
	class VarBytesType : FixedLengthFieldInfo {
		public VarBytesType (int byteCount) {
			TypeName = "unknown";
			length = byteCount;
			TypeCode = TypeCode.Empty;
		}

		public override string Decode(BinaryReader reader) {
			byte[] bytes = reader.ReadBytes (length);
			if (bytes.Length == 0)
				return "";
			StringBuilder result = new StringBuilder (3 * bytes.Length);
			result.Append (string.Format ("{0:x2}", bytes [0]));
			for (int i = 1; i < bytes.Length; i++) {
				result.Append (string.Format (" {0:x2}", bytes [i]));
			}
			return result.ToString ();
		}
		public override void Encode(BinaryWriter writer, string val) {
			string[] split = val.Split (' ');
			foreach (string s in split) {
				writer.Write (byte.Parse(s, System.Globalization.NumberStyles.HexNumber));
			}
		}
	}
}
