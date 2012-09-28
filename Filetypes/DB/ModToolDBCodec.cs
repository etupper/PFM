using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;

using NameMapping = System.Tuple<string, string>;


namespace Filetypes {
    public class ModToolDBCodec : Codec<DBFile> {
        TableNameCorrespondencyManager nameManager;
        
        public ModToolDBCodec(TableNameCorrespondencyManager corMan) {
            nameManager = corMan;
        }
        
        public DBFile Decode(Stream stream) {
            throw new NotSupportedException();
        }
        
        // write given file to given stream in ca xml format
        public void Encode(Stream dbFile, DBFile file) {
            using (var writer = new StreamWriter(dbFile)) {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<dataroot>");
                if (!string.IsNullOrEmpty(file.Header.GUID)) {
                    writer.WriteLine(string.Format("<edit_uuid>{0}</edit_uuid>", file.Header.GUID));
                }
                foreach(List<FieldInstance> fields in file.Entries) {
                    writer.WriteLine(" <{0}>", file.CurrentType.Name);
                    foreach (FieldInstance field in fields) {
                        string fieldName;
                        try {
                            fieldName = nameManager.GetXmlFieldName(file.CurrentType.Name, field.Name);
                        } catch {
                            Console.Error.WriteLine("No xml field name for {0}:{1}", file.CurrentType.Name, field.Name);
                            fieldName = field.Name;
                        }
                        string toWrite = field.Value;
                        writer.WriteLine("  <{0}>{1}</{0}>", fieldName, toWrite);
                    }
                    writer.WriteLine(" </{0}>", file.CurrentType.Name);
                }
                writer.WriteLine("</dataroot>");
            }
        }
    }

    public class TableNameCorrespondencyManager {
        public const string DEFAULT_FILE_NAME = "correspondencies.xml";
        Dictionary<string, List<NameMapping>> tableMapping = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> TableMapping {
            get {
                return tableMapping;
            }
        }
        
        public TableNameCorrespondencyManager() {}
        
        // load name correspondencies from file
        public TableNameCorrespondencyManager(string filename = DEFAULT_FILE_NAME) {
            XmlDocument xmlFile = new XmlDocument();
            xmlFile.Load(filename);
            foreach(XmlNode tableNode in xmlFile.ChildNodes[0].ChildNodes) {
                string tableName = tableNode.Attributes["name"].Value;
                List<NameMapping> mappings = new List<NameMapping>();
                tableMapping.Add(tableName, mappings);
                foreach(XmlNode fieldNode in tableNode.ChildNodes) {
                    string packName = tableNode.Attributes["pack"].Value;
                    string xmlName = tableNode.Attributes["xml"].Value;
                    mappings.Add(new Tuple<string, string>(packName, xmlName));
                }
                
            }
        }
        
        // store to file
        public void SaveToFile(string filename = DEFAULT_FILE_NAME) {
            using (var file = File.CreateText(filename)) {
                file.WriteLine("<correspondencies>");
                foreach (string tableName in tableMapping.Keys) {
                    file.WriteLine(" <table name=\"{0}\">", tableName);
                    foreach (NameMapping fieldNames in tableMapping[tableName]) {
                        file.WriteLine(string.Format("  <field pack=\"{0}\" xml=\"{1}\"/>", fieldNames.Item1, fieldNames.Item2));
                    }
                    file.WriteLine(" </table>");
                }
                file.WriteLine("</correspondencies>");
            }
        }
  
        // retrieve ca xml tag for given db schema table/field combination
        public string GetXmlFieldName(string table, string field) {
            NameMapping result = null;
            List<NameMapping> mapping = tableMapping[table];
            foreach(NameMapping candidate in mapping) {
                if (candidate.Item1.Equals(field)) {
                    result = candidate;
                    break;
                }
            }
            return result.Item2;
        }
    }
}

