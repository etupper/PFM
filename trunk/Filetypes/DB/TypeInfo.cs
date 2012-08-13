using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Filetypes {
	[StructLayout(LayoutKind.Sequential)]
    public class TypeInfo {
		public string name;
		public List<FieldInfo> fields = new List<FieldInfo> ();

		public TypeInfo () {
		}

		public TypeInfo (string n) {
			name = n;
		}

		public TypeInfo (TypeInfo toCopy) {
			name = toCopy.name;
			fields.AddRange (toCopy.fields);
		}
	}
}

