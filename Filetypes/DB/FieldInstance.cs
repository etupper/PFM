using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Filetypes
{

    [DebuggerDisplay("{Value}:{Info}; ")]
    public class FieldInstance
    {
        public FieldInstance(FieldInfo fieldInfo, string value)
        {
            Info = fieldInfo;
            Value = value;
        }

        public FieldInfo Info { get; private set; }

        public string Value { get; set; }

		public override bool Equals(object o) {
			bool result = o is FieldInstance;
			if (result) {
				result = Value.Equals ((o as FieldInstance).Value);
			}
			return result;
		}
		public override int GetHashCode() {
			return 2 * Info.GetHashCode () + 3 * Value.GetHashCode ();
		}
    }
}

