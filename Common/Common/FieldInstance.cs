using System.Diagnostics;
using System.Collections.Generic;

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

        public static FieldInstance createInstance(FieldInfo info)
        {
            FieldInstance result = null;
            switch (info.type)
            {
                case PackTypeCode.Empty:
                    List<string> list2 = new List<string>();
                    int num = 0;
                    while (num < info.length)
                    {
                        list2.Add("00");
                        num++;
                    }
                    result = new FieldInstance(info, string.Join(" ", list2.ToArray()));
                    break;

                case PackTypeCode.Object:
                case PackTypeCode.DBNull:
                case PackTypeCode.Char:
                case PackTypeCode.SByte:
                case PackTypeCode.Byte:
                case PackTypeCode.Decimal:
                case PackTypeCode.DateTime:
                case (PackTypeCode.DateTime | PackTypeCode.Object):
                    {
                        break;
                    }
                case PackTypeCode.Boolean:
                    {
                        result = new FieldInstance(info, "False");
                        break;
                    }
                case PackTypeCode.Int16:
                case PackTypeCode.UInt16:
                case PackTypeCode.Int32:
                case PackTypeCode.UInt32:
                case PackTypeCode.Int64:
                case PackTypeCode.UInt64:
                case PackTypeCode.Single:
                case PackTypeCode.Double:
                    {
                        result = new FieldInstance(info, "0");
                        break;
                    }
                case PackTypeCode.String:
                case PackTypeCode.StringContainer:
                    {
                        result = new FieldInstance(info, string.Empty);
                        break;
                    }
                default:
                    {
                        result = null;
                        break;
                    }
            }
            return result;
        }
    }
}

