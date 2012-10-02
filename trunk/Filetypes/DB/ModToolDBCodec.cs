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
        public const string DEFAULT_INCOMPATIBLE_FILE_NAME = "incompatible_tables.xml";
        Dictionary<string, List<NameMapping>> tableMapping = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> TableMapping {
            get {
                return tableMapping;
            }
        }
        Dictionary<string, List<NameMapping>> partialTableMapping = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> PartialTableMapping {
            get {
                return partialTableMapping;
            }
        }
        Dictionary<string, List<NameMapping>> incompatibleTables = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> IncompatibleTables {
            get {
                return incompatibleTables;
            }
        }
        
        public void Clear() {
            tableMapping.Clear();
            partialTableMapping.Clear();
            incompatibleTables.Clear();
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
            LoadFromFile(DEFAULT_FILE_NAME, tableMapping);
            LoadFromFile(DEFAULT_PARTIAL_FILE_NAME, partialTableMapping);
            LoadFromFile(DEFAULT_INCOMPATIBLE_FILE_NAME, incompatibleTables);
        }
  
        // I'm sure this could be done via Serialization
        public void Save() {
            SaveToFile(DEFAULT_FILE_NAME, tableMapping);
            SaveToFile(DEFAULT_PARTIAL_FILE_NAME, partialTableMapping);
            SaveToFile(DEFAULT_INCOMPATIBLE_FILE_NAME, incompatibleTables);
        }
        
        public void LoadFromFile(string filename, Dictionary<string, List<NameMapping>> table) {
            try {
                XmlDocument xmlFile = new XmlDocument();
                xmlFile.Load(filename);
                foreach(XmlNode tableNode in xmlFile.ChildNodes[0].ChildNodes) {
                    string tableName = tableNode.Attributes["name"].Value;
                    List<NameMapping> mappings = new List<NameMapping>();
                    table.Add(tableName, mappings);
                    foreach(XmlNode fieldNode in tableNode.ChildNodes) {
                        string packName = fieldNode.Attributes["pack"].Value;
                        string xmlName = fieldNode.Attributes["xml"].Value;
                        mappings.Add(new Tuple<string, string>(packName, xmlName));
                    }                
                }
            } catch (Exception ex) {
                Console.Error.WriteLine("failed to load {0}: {1}", filename, ex.Message);
            }
        }
        
        // store to file
        public void SaveToFile(string filename, Dictionary<string, List<NameMapping>> tableMapping) {
            using (var file = File.CreateText(filename)) {
                file.WriteLine("<correspondencies>");
                foreach (string tableName in tableMapping.Keys) {
                    string guid = tableGuidMap.ContainsKey(tableName) ? tableGuidMap[tableName] : "unknown";
                    file.WriteLine(" <table name=\"{0}\" guid=\"{1}\">", tableName, guid);
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
            string result = GetXmlFieldName(table, field, tableMapping);
            if (result == null) {
                result = GetXmlFieldName(table, field, incompatibleTables);
            }
            if (result == null) {
                result = GetXmlFieldName(table, field, partialTableMapping);
            }
            return null;
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

