using System.Diagnostics;
namespace Common
{

    [DebuggerDisplay("FieldInfo = {Info}; Value = {Value}")]
    public class FieldInstance
    {
        public FieldInstance(FieldInfo fieldInfo, string value)
        {
            Info = fieldInfo;
            Value = value;
        }

        public FieldInfo Info { get; private set; }

        public string Value { get;  set; }
    }
}

