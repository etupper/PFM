﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackFileTest.Mapping {

    public class UniqueTableGenerator {
        string xmlDirectory;
        string tableName;
        string guid;
        List<CaFieldInfo> generateFor;

        Dictionary<string, IValueGenerator> generators = new Dictionary<string, IValueGenerator>();

        public UniqueTableGenerator(string xmlDir, string table) {
            generateFor = CaFieldInfo.ReadInfo(xmlDir, table, out guid);
            xmlDirectory = xmlDir;
            tableName = table;

            IValueGenerator intGenerator = new IntGenerator();
            generators.Add("integer", intGenerator);
            generators.Add("longinteger", intGenerator);
            generators.Add("autonumber", intGenerator);

            IValueGenerator floatGenerator = new FloatGenerator();
            generators.Add("single", floatGenerator);
            generators.Add("double", floatGenerator);
            generators.Add("decimal", floatGenerator);

            IValueGenerator textGenerator = new TextGenerator();
            generators.Add("text", textGenerator);
            generators.Add("memo", textGenerator);

            IValueGenerator boolGenerator = new BoolGenerator();
            generators.Add("yesno", boolGenerator);
        }

        public void GenerateTable() {
            Console.WriteLine(tableName);
            using (var file = File.CreateText(Path.Combine(xmlDirectory, string.Format("{0}.xml", tableName)))) {
                file.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                file.WriteLine("<dataroot export_time=\"Mon 24. Oct 11:11:16 2011\" revision=\"34\" export_branch=\"C:/Official_Mod_Tools/binaries\" export_user=\"modder\">");
                file.Write("<edit_uuid>{0}</edit_uuid>", guid);
                for (int i = 0; i < 20; i++) {
                    file.WriteLine("<{0}>", tableName);
                    foreach (CaFieldInfo info in generateFor) {
                        string value;
                        if (info.Reference != null) {
                            value = string.Format("{0}_0", info.Reference.Field);
                        } else {
                            IValueGenerator generator = generators[info.FieldType];
                            value = generator.NextValue(info.Name);
                        }
                        file.WriteLine("<{0}>{1}</{0}>", info.Name, value);
                    }
                    file.WriteLine("</{0}>", tableName);
                }
                file.WriteLine("</dataroot>");
            }
        }
    }

    interface IValueGenerator {
        string NextValue(string fieldname);
    }

    class IntGenerator : IValueGenerator {
        int lastValue = 0;
        public string NextValue(string fieldname) {
            string result = lastValue.ToString();
            lastValue++;
            return result;
        }
    }

    class TextGenerator : IValueGenerator {
        int index = 0;
        public string NextValue(string fieldName) {
            return string.Format("{0}_{1}", fieldName, index++);
        }
    }

    class BoolGenerator : IValueGenerator {
        Random random = new Random();
        public string NextValue(string fieldName) {
            return random.Next(0, 2).ToString();
        }
    }

    class FloatGenerator : IValueGenerator {
        float lastValue = 0;
        public string NextValue(string fieldname) {
            string result = lastValue.ToString().Replace(',', '.');
            lastValue += 0.1f;
            return result;
        }
    }
}
