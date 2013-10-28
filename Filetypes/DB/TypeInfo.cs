using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Filetypes {
	[StructLayout(LayoutKind.Sequential)]
    public class TypeInfo {
		public string Name {
            get; set;
        }
        public int Version {
            get; set;
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
	}
}

