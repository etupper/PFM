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
				return StringType ();
			case "optstring":
				return OptStringType ();
			case "int":
				return IntType ();
			case "short":
				return ShortType ();
			case "float":
				return SingleType ();
			case "boolean":
				return BoolType ();
            case "list":
                return ListType();
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
            return string.Format("{0}:{1}", Name, TypeName);
        }
	}

	class StringType : FieldInfo {
		public StringType () {
			TypeName = "string";
			TypeCode = TypeCode.String;
		}
        public override FieldInstance CreateInstance() {
            return new StringField() {
                Name = this.Name,
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
                Name = this.Name,
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
                Name = this.Name,
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
                Name = this.Name,
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
                Name = this.Name,
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
                Name = this.Name,
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
            return new VarByteField(byteCount) {
                Name = this.Name
            };
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
        public List<FieldInstance> CreateContainedInstance() {
            List<FieldInstance> result = new List<FieldInstance>();
            containedInfos.ForEach(i => result.Add(i.CreateInstance()));
            return result;
        }
        
        public bool EncodeItemIndices {
            get {
                return false;
            }
            set {
                // ignore
            }
        }
        
        int itemIndexAt = -1;
        public int ItemIndexAt {
            get { return itemIndexAt >= Infos.Count ? -1 : itemIndexAt; }
            set { itemIndexAt = value; }
        }
        int nameAt = -1;
        public int NameAt {
            get {
                int result = nameAt >= Infos.Count ? -1 : nameAt;
                if (result == -1) {
                    // use the first string we find
                    for (int i = 0; i < Infos.Count; i++) {
                        if (Infos[i].TypeCode == System.TypeCode.String) {
                            result = i;
                            break;
                        }
                    }
                }
                return result;
            }
            set { nameAt = value; }
        }

        public override bool Equals(object other) {
            bool result = base.Equals(other);
            if (result) {
                ListType type = other as ListType;
                result &= type.containedInfos.Count == containedInfos.Count;
                if (result) {
                    for (int i = 0; i < containedInfos.Count; i++) {
                        result &= containedInfos[i].Equals(type.containedInfos[i]);
                    }
                }
            }
            return result;
        }
        
        public override int GetHashCode() {
            return 2*Name.GetHashCode() + 3*Infos.GetHashCode();
        }
        
//        public override string ToString() {
//            return string.Format("list ({0} fields)", Infos.Count);
//        }
    }
}