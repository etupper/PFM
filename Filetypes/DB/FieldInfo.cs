using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Common;

namespace Filetypes
{
	public class Types {
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
            case "list":
                return new ListType();
			}
			if (typeName.StartsWith ("blob")) {
				string lengthPart = typeName.Substring (4);
				int length = int.Parse (lengthPart);
				return new VarBytesType (length);
			}
			return null;
		}
        public static FieldInfo StringType() { return new StringType() { Name = "unknown" }; }
        public static FieldInfo IntType() { return new IntType() { Name = "unknown" }; }
        public static FieldInfo ShortType() { return new ShortType() { Name = "unknown" }; }
        public static FieldInfo BoolType() { return new BoolType() { Name = "unknown" }; }
        public static FieldInfo OptStringType() { return new OptStringType() { Name = "unknown" }; }
        public static FieldInfo SingleType() { return new SingleType() { Name = "unknown" }; }
        public static FieldInfo ByteType() { return new VarBytesType(1) { Name = "unknown" }; }
        public static FieldInfo ListType() { return new ListType() { Name = "unknown" }; }
    }
	
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

		public virtual string TypeName { get; set; }
		public TypeCode TypeCode { get; set; }

		// public abstract int Length(string str);
        
        public abstract FieldInstance CreateInstance();
//		public abstract void Encode(BinaryWriter writer, string val);
//		public abstract string Decode(BinaryReader reader);
		
        public override bool Equals(object other) {
            bool result = false;
            if (other is FieldInfo) {
                FieldInfo info = other as FieldInfo;
                result = Name.Equals(info.Name);
                result &= TypeName.Equals(info.TypeName);
            }
            return result;
        }
        
        public override int GetHashCode() {
            return 2*Name.GetHashCode() +
                3*TypeName.GetHashCode();
        }
        
        public override string ToString() {
            return TypeName;
        }
	}

	class StringType : FieldInfo {
		public StringType () {
			TypeName = "string";
			TypeCode = TypeCode.String;
		}
        public override FieldInstance CreateInstance() {
            return new StringField() {
                Value = ""
            };
        }
	}

	class IntType : FieldInfo {
		public IntType () {
			TypeName = "int";
			TypeCode = TypeCode.Int32;
		}
        public override FieldInstance CreateInstance() {
            return new IntField() {
                Value = "0"
            };
        }
	}

	class ShortType : FieldInfo {
		public ShortType () {
			TypeName = "short";
			TypeCode = TypeCode.Int16;
		}
        public override FieldInstance CreateInstance() {
            return new ShortField() {
                Value = "0"
            };
        }
	}

	class SingleType : FieldInfo {
		public SingleType () {
			TypeName = "float";
			TypeCode = TypeCode.Single;
		}
        public override FieldInstance CreateInstance() {
            return new SingleField() {
                Value = "0"
            };
        }
	}

	class BoolType : FieldInfo {
		public BoolType () {
			TypeName = "boolean";
			TypeCode = TypeCode.Boolean;
		}
        public override FieldInstance CreateInstance() {
            return new BoolField() {
                Value = "false"
            };
        }
	}

	class OptStringType : FieldInfo {
		public OptStringType () {
			TypeName = "optstring";
			TypeCode = TypeCode.String;
		}
        public override FieldInstance CreateInstance() {
            return new OptStringField() {
                Value = ""
            };
        }
	}

	public class VarBytesType : FieldInfo {
        int byteCount;
		public VarBytesType (int bytes) {
			TypeName = string.Format("blob{0}", byteCount);
			TypeCode = TypeCode.Empty;
            byteCount = bytes;
		}
        public override FieldInstance CreateInstance() {
            return new VarByteField(byteCount);
        }
	}
    
    public class ListType : FieldInfo {
        public ListType() {
            TypeName = "list";
            TypeCode = TypeCode.Object;
        }
        
        public override string TypeName {
            get {
                return "list";
            }
        }
        
        List<FieldInfo> containedInfos = new List<FieldInfo>();
        public List<FieldInfo> Infos {
            get {
                return containedInfos;
            }
            set {
                containedInfos.Clear();
                if (value != null) {
                    containedInfos.AddRange(value);
                }
            }
        }
        public override FieldInstance CreateInstance() {
            ListField field = new ListField(this);
            // containedInfos.ForEach(i => field.Contained.Add(i.CreateInstance()));
            return field;
        }
        
        public bool EncodeItemIndices {
            get {
                return false;
            }
            set {
                // ignore
            }
        }
        
//        public override string ToString() {
//            return string.Format("list ({0} fields)", Infos.Count);
//        }
    }
}