using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Filetypes {
	[StructLayout(LayoutKind.Sequential)]
    public class TypeInfo {
		public string Name {
            get; set;
        }
		List<FieldInfo> fields = new List<FieldInfo> ();
        public List<FieldInfo> Fields {
            get {
                return fields;
            }
        }

		public TypeInfo () {
		}

		public TypeInfo (List<FieldInfo> addFields) {
			Fields.AddRange(addFields);
		}

		public TypeInfo (TypeInfo toCopy) {
			Name = toCopy.Name;
			Fields.AddRange (toCopy.Fields);
		}
	}
}

