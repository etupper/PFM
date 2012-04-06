using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Common {
    public class DBTypeMap {
        public static readonly string SCHEMA_FILE_NAME = "schema.xml";
        public static readonly string SCHEMA_USER_FILE_NAME = "schema_user.xml";
        public SortedDictionary<string, List<FieldInfo>> typeMap = new SortedDictionary<string, List<FieldInfo>>();
        public static readonly DBTypeMap Instance = new DBTypeMap();

        private DBTypeMap() {
            // prevent instantiation
        }

        public void initializeTypeMap(string basePath) {
            string xmlFile = Path.Combine(basePath, SCHEMA_USER_FILE_NAME);
            if (!File.Exists(xmlFile)) {
                xmlFile = Path.Combine(basePath, SCHEMA_FILE_NAME);
            }
            initializeFromFile(xmlFile);
        }
        public void initializeFromFile(string filename) {
            typeMap = loadXmlSchema(filename);
        }

        public void loadFromXsd(string xsdFile) {
            //            typeMap = new XsdParser (xsdFile).loadXsd ();
        }

        public void saveToFile(string path) {
            string filename = Path.Combine(path, SCHEMA_USER_FILE_NAME);
            string backupName = filename + ".bak";
            if (File.Exists(filename)) {
                File.Copy(filename, backupName);
            }
            var stream = File.Create(filename);
            new XmlExporter(stream).export(typeMap);
            stream.Close();
            if (File.Exists(backupName)) {
                File.Delete(backupName);
            }
        }

        public void set(string key, List<FieldInfo> setTo) {
            typeMap[key] = setTo;
        }

        SortedDictionary<string, List<FieldInfo>> loadXmlSchema(string filename) {
            XmlImporter importer = null;
            using (Stream stream = File.OpenRead(filename)) {
                importer = new XmlImporter(stream);
                importer.import();
            }
            return importer.descriptions;
        }

        private static List<TypeInfo> retrieveOrAdd<T>(IDictionary<T, List<TypeInfo>> dict, T key) {
            List<TypeInfo> list;
            if (!dict.TryGetValue(key, out list)) {
                list = new List<TypeInfo>();
                dict.Add(key, list);
            }
            return list;
        }

        public List<string> DBFileTypes {
            get {
                List<string> result = new List<string>(typeMap.Keys);
                return result;
            }
        }

        public TypeInfo this[string key, int index, bool downgrade = true] {
            get {
                TypeInfo result = new TypeInfo(key);
                List<FieldInfo> list;
                if (typeMap.TryGetValue(key, out list)) {
                    foreach (FieldInfo d in list) {
                        if (d.StartVersion <= index && d.LastVersion >= index) {
                            result.fields.Add(d);
                        }
                    }
                }
                return result;
            }
        }
        public int MaxVersion(string type) {
            int result = 0;
            List<FieldInfo> list = null;
            if (typeMap.TryGetValue(type, out list)) {
                list.ForEach(delegate(FieldInfo d) { result = Math.Max(d.StartVersion, result); });
            }
            return result;
        }
        public Boolean IsSupported(string type) {
            return typeMap.ContainsKey(type);
        }
    }

    class TypeInfoComparer : Comparer<TypeInfo> {
        public override int Compare(TypeInfo x, TypeInfo y) {
            return x.name.CompareTo(y.name);
        }
    }
}
