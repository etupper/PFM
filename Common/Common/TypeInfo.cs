using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Common
{
    [StructLayout(LayoutKind.Sequential)]
    public class TypeInfo
    {
        public string name;
        public List<FieldInfo> fields = new List<FieldInfo>();
        public TypeInfo()
        {
        }
        public TypeInfo(string n)
        {
            name = n;
        }
        public TypeInfo(string name, string fields)
        {
            this.name = name;
            string[] strArray = fields.Split(";".ToCharArray());
            foreach (string str in strArray)
            {
                if (string.IsNullOrEmpty(str)) continue;
                string[] strArray2 = str.Split(",".ToCharArray());
                if (strArray2.Length == 2)
                {
                    this.fields.Add(new FieldInfo(strArray2[0], strArray2[1]));
                }
                else
                {
                    if (strArray2.Length != 3)
                    {
                        throw new InvalidDataException("wrong number of FieldInfo tokens");
                    }
                    this.fields.Add(new FieldInfo(strArray2[0], strArray2[1], strArray2[2]));
                }
            }
        }
    }
}

