using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Filetypes {
    public class TypeVersionTuple {
        public string Type { get; set; }
        public int MaxVersion { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TypeInfo : IComparable<TypeInfo> {
		public string Name {
            get; set;
        }
        int version = 0;
        public int Version {
            get {
                int result = version;
                if (result == -1) {
                    fields.ForEach(f => {
                        result = Math.Max(result, f.StartVersion);
                        if (f.LastVersion != int.MaxValue) {
                            result = Math.Max(result, f.LastVersion+1);
                        }
                    });
                }
                return result;
            }
            set {
                version = value;
            }
        }
		List<FieldInfo> fields = new List<FieldInfo> ();
        public List<FieldInfo> Fields {
            get {
                return fields;
            }
        }
        List<string> applicableGuids = new List<string>();
        public List<string> ApplicableGuids {
            get {
                return applicableGuids;
            }
        }
        public FieldInfo this[string name] {
            get {
                FieldInfo result = null;
                foreach(FieldInfo field in fields) {
                    if (field.Name.Equals(name)) {
                        result = field;
                        break;
                    }
                }
                return result;
            }
        }
        public List<FieldInfo> ForVersion(int version) {
            List<FieldInfo> infos = new List<FieldInfo>();
            fields.ForEach(f => {
                if (f.StartVersion <= version && f.LastVersion >= version) {
                    infos.Add(f);
                }
            });
            return infos;
        }
  
        #region Constructors
		public TypeInfo () {
		}

		public TypeInfo (List<FieldInfo> addFields) {
			Fields.AddRange(addFields);
		}

		public TypeInfo (TypeInfo toCopy) {
			Name = toCopy.Name;
			Fields.AddRange (toCopy.Fields);
		}
        #endregion
  
        public int CompareTo(TypeInfo other) {
            int result = Name.CompareTo(other.Name);
            if (result == 0) {
                result = Version - other.Version;
            }
            return result;
        }
    }
}

