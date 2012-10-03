using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;

using NameMapping = System.Tuple<string, string>;


namespace Filetypes {
    /*
     * Decodes a DBFile to the format used by CA's Assembly Kit.
     */
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
                            throw new DBFileNotSupportedException(
                                string.Format("No xml field name for {0}:{1}", file.CurrentType.Name, field.Name));
                        }
                        string toWrite = Encode(field);
                        writer.WriteLine("  <{0}>{1}</{0}>", fieldName, toWrite);
                    }
                    writer.WriteLine(" </{0}>", file.CurrentType.Name);
                }
                writer.WriteLine("</dataroot>");
            }
        }
        
        string Encode(FieldInstance field) {
            string result = field.Value;
            if (field.Info.TypeCode == TypeCode.Boolean) {
                result = bool.Parse(field.Value) ? "1" : "0";
            }
            return result;
        }
    }
 
    /*
     * Provides mapping between column names of schema.xml (from the community mod tools)
     * to those used by CA.
     */
    public class TableNameCorrespondencyManager {
        public const string DEFAULT_FILE_NAME = "correspondencies.xml";
        public const string DEFAULT_PARTIAL_FILE_NAME = "partial_correspondencies.xml";

        static string UNMAPPED_PACK_FIELDS = "unmappedPackedFields";
        static string UNMAPPED_XML_FIELDS = "unmappedXmlFields";

        Dictionary<string, MappedTable> mappedTables = new Dictionary<string, MappedTable>();

        public List<NameMapping> GetMappedFieldsForTable(string table) {
            List<NameMapping> fields = new List<NameMapping>();
            if (mappedTables.ContainsKey(table)) {
                fields.AddRange(mappedTables[table].MappingAsTuples);
            }
            return fields;
        }
        public Dictionary<string, MappedTable> MappedTables {
            get { return mappedTables; }
        }
        List<NameMapping> GetMappedFields(Dictionary<string, List<NameMapping>> map, string table) {
            List<NameMapping> result = new List<NameMapping>();
            if (map.ContainsKey(table)) {
                result.AddRange(map[table]);
            }
            return result;
        }

        public void Clear() {
            mappedTables.Clear();
            tableGuidMap.Clear();
        }

        Dictionary<string, string> tableGuidMap = new Dictionary<string, string>();
        public Dictionary<string, string> TableGuidMap {
            get { return tableGuidMap; }
        }
        

        private static TableNameCorrespondencyManager instance;
        public static TableNameCorrespondencyManager Instance {
            get {
                if (instance == null) {
                    instance = new TableNameCorrespondencyManager();
                }
                return instance;
            }
        }
        
        // load name correspondencies from file
        private TableNameCorrespondencyManager() {
            LoadFromFile(DEFAULT_FILE_NAME, mappedTables);
            LoadFromFile(DEFAULT_PARTIAL_FILE_NAME, mappedTables);
        }
  
        // I'm sure this could be done via Serialization
        public void Save() {
            StreamWriter fullyMappedWriter = File.CreateText(DEFAULT_FILE_NAME);
            StreamWriter partiallyMappedWriter = File.CreateText(DEFAULT_PARTIAL_FILE_NAME);
            fullyMappedWriter.WriteLine("<correspondencies>");
            partiallyMappedWriter.WriteLine("<correspondencies>");

            foreach (MappedTable table in mappedTables.Values) {
                StreamWriter writeTo;
                if (table.IsFullyMapped) {
                    writeTo = fullyMappedWriter;
                } else {
                    writeTo = partiallyMappedWriter;
                }
                SaveToFile(writeTo, table);
            }

            fullyMappedWriter.WriteLine("</correspondencies>");
            partiallyMappedWriter.WriteLine("</correspondencies>");
            fullyMappedWriter.Dispose();
            partiallyMappedWriter.Dispose();
        }
        
        public static void LoadFromFile(string filename, Dictionary<string, MappedTable> tables) {
            try {
                XmlDocument xmlFile = new XmlDocument();
                xmlFile.Load(filename);
                char[] separator = new char[] { ',' };
                foreach(XmlNode tableNode in xmlFile.ChildNodes[0].ChildNodes) {
                    string tableName = tableNode.Attributes["name"].Value;
                    MappedInfoTable table = new MappedInfoTable(tableName);
                    table.Guid = tableNode.Attributes["guid"].Value;
                    tables.Add(tableName, table);
                    foreach(XmlNode fieldNode in tableNode.ChildNodes) {
                        if (fieldNode.Name.Equals("field")) {
                            string packName = fieldNode.Attributes["pack"].Value;
                            if (fieldNode.Attributes["xml"] != null) {
                                string xmlName = fieldNode.Attributes["xml"].Value;
                                table.AddMapping(packName, xmlName);
                            } else if (fieldNode.Attributes["constant"] != null) {
                                table.AddConstantValue(packName, fieldNode.Attributes["constant"].Value);
                            }
                        } else if (fieldNode.Name.Equals(UNMAPPED_PACK_FIELDS)) {
                            string[] list = fieldNode.InnerText.Split(separator);
                            foreach (string field in list) {
                                table.PackDataFields.Add(field);
                            }
                        } else if (fieldNode.Name.Equals(UNMAPPED_XML_FIELDS)) {
                            string[] list = fieldNode.InnerText.Split(separator);
                            foreach (string field in list) {
                                table.XmlDataFields.Add(field);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Console.Error.WriteLine("failed to load {0}: {1}", filename, ex.Message);
            }
        }

        public static Dictionary<string, List<NameMapping>> LoadFromFile(string filename) {
            Dictionary<string, List<NameMapping>> tableToMapping = new Dictionary<string,List<NameMapping>>();
            try {
                XmlDocument xmlFile = new XmlDocument();
                xmlFile.Load(filename);
                char[] separator = new char[] { ',' };
                foreach (XmlNode tableNode in xmlFile.ChildNodes[0].ChildNodes) {
                    List<NameMapping> mappings = new List<NameMapping>();
                    string tableName = tableNode.Attributes["name"].Value;
                    foreach (XmlNode fieldNode in tableNode.ChildNodes) {
                        if (fieldNode.Name.Equals("field")) {
                            string packName = fieldNode.Attributes["pack"].Value;
                            string xmlName = fieldNode.Attributes["xml"].Value;
                            mappings.Add(new NameMapping(packName, xmlName));
                        }
                    }
                    tableToMapping.Add(tableName, mappings);
                }
            } catch (Exception ex) {
                Console.Error.WriteLine("failed to load {0}: {1}", filename, ex.Message);
            }
            return tableToMapping;
        }
        
        // store to file
        public void SaveToFile(StreamWriter file, MappedTable table) {
            string guid = table.Guid;
            file.WriteLine(" <table name=\"{0}\" guid=\"{1}\">", table.TableName, guid);
            foreach (NameMapping fieldNames in table.MappingAsTuples) {
                file.WriteLine(string.Format("  <field pack=\"{0}\" xml=\"{1}\"/>", fieldNames.Item1, fieldNames.Item2));
            }
            foreach (string constantField in table.ConstantValues.Keys) {
                file.WriteLine(string.Format("  <field pack=\"{0}\" constant=\"{1}\"/>", constantField, table.ConstantValues[constantField]));
            }
            WriteList(file, UNMAPPED_PACK_FIELDS, table.UnmappedPackFieldNames);
            WriteList(file, UNMAPPED_XML_FIELDS, table.UnmappedXmlFieldNames);
            file.WriteLine(" </table>");
        }

        static void WriteList(StreamWriter writer, string tag, List<string> list) {
            if (list.Count != 0) {
                writer.WriteLine(string.Format("  <{1}>{0}</{1}>", string.Join(",", list), tag));
            }
        }
        
        // retrieve ca xml tag for given db schema table/field combination
        public string GetXmlFieldName(string table, string field) {
            string result = null;
            if (mappedTables.ContainsKey(table)) {
                result = mappedTables[table].GetMappedXml(field);
            }
            return result;
        }

        static string GetXmlFieldName(string table, string field, Dictionary<string, List<NameMapping>> mapping) {
            if (!mapping.ContainsKey(table)) {
                return null;
            }
            foreach(NameMapping candidate in mapping[table]) {
                if (candidate.Item1.Equals(field)) {
                        return candidate.Item2;
                }
            }
            return null;
        }
    }
}

