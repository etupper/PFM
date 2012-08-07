using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Common {
    public class DBTypeMap {
        public static readonly string SCHEMA_FILE_NAME = "schema.xml";
        public static readonly string SCHEMA_USER_FILE_NAME = "schema_user.xml";
        SortedDictionary<string, List<FieldInfo>> typeMap = new SortedDictionary<string, List<FieldInfo>>();
        SortedDictionary<GuidTypeInfo, List<FieldInfo>> guidMap = new SortedDictionary<GuidTypeInfo, List<FieldInfo>>();
        public static readonly DBTypeMap Instance = new DBTypeMap();

        private DBTypeMap() {
            // prevent instantiation
        }
        public SortedDictionary<string, List<FieldInfo>> TypeMap {
            get {
                return new SortedDictionary<string, List<FieldInfo>>(typeMap);
            }
        }
        public SortedDictionary<GuidTypeInfo, List<FieldInfo>> GuidMap {
            get {
                return new SortedDictionary<GuidTypeInfo, List<FieldInfo>>(guidMap);
            }
        }

        public void initializeTypeMap(string basePath) {
            string xmlFile = Path.Combine(basePath, SCHEMA_USER_FILE_NAME);
            if (!File.Exists(xmlFile)) {
                xmlFile = Path.Combine(basePath, SCHEMA_FILE_NAME);
            }
            initializeFromFile(xmlFile);
        }
        public void initializeFromFile(string filename) {
            XmlImporter importer = null;
            using (Stream stream = File.OpenRead(filename)) {
                importer = new XmlImporter(stream);
                importer.import();
            }
            typeMap = importer.descriptions;
            guidMap = importer.guidToDescriptions;
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
            new XmlExporter(stream).export();
            stream.Close();
            if (File.Exists(backupName)) {
                File.Delete(backupName);
            }
        }

        public void SetByName(string key, List<FieldInfo> setTo) {
            typeMap[key] = setTo;
        }
        public void SetByGuid(string guid, string tableName, int version, List<FieldInfo> setTo) {
            GuidTypeInfo info = new GuidTypeInfo(guid, tableName, version);
            guidMap[info] = setTo;
        }
        public string GetTableForGuid(string guid) {
            string result = "";
            foreach(GuidTypeInfo info in guidMap.Keys) {
                if (info.Guid.Equals(guid)) {
                    result = info.TypeName;
                    break;
                }
            }
            return result;
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

        public TypeInfo GetVersionedInfo(string guid, string key, int version) {
            TypeInfo result = new TypeInfo(key);
            GuidTypeInfo info = new GuidTypeInfo(guid, key, version);
            if (!string.IsNullOrEmpty(guid) && guidMap.ContainsKey(info)) {
                result.fields.AddRange(guidMap[info]);
            } else {
                List<FieldInfo> list;
                if (typeMap.TryGetValue(key, out list)) {
                    foreach (FieldInfo d in list) {
                        if (d.StartVersion <= version && d.LastVersion >= version) {
                            result.fields.Add(d);
                        }
                    }
                }
            }
            return result;
        }

        public List<string> GetGuidsForInfo(string type, int version) {
            List<string> result = new List<string>();
            foreach(GuidTypeInfo info in guidMap.Keys) {
                if (info.Version == version && info.TypeName.Equals(type)) {
                    result.Add(info.Guid);
                }
            }
            return result;
        }

        public int MaxVersion(string type) {
            int result = 0;
            bool found = false;
            // look in the guid tables first
            foreach(GuidTypeInfo info in guidMap.Keys) {
                if (info.TypeName.Equals(type)) {
                    result = Math.Max(result, info.Version);
                    found = true;
                }
            }
            if (!found) {
                List<FieldInfo> list = null;
                if (typeMap.TryGetValue(type, out list)) {
                    list.ForEach(delegate(FieldInfo d) { result = Math.Max(d.StartVersion, result); });
                }
            }
            return result;
        }
        public Boolean IsSupported(string type) {
            bool result = typeMap.ContainsKey(type);
            if (!result) {
                foreach(GuidTypeInfo info in guidMap.Keys) {
                    if (info.TypeName.Equals(type)) {
                        result = true; break;
                    }
                }
            }
            return result;
        }
    }
    
    public class GuidTypeInfo : IComparable<GuidTypeInfo> {
        static char[] SEPARATOR = { '/' };
        public GuidTypeInfo(string guid) : this(guid, "", 0) {}
        public GuidTypeInfo(string guid, string type, int version) {
            Guid = guid;
            TypeName = type;
            Version = version;
        }
        public GuidTypeInfo(string guid, string encoded) {
            Guid = guid;
            DecodeVersionedType(encoded);
        }
        public string Guid { get; set; }
        public string TypeName { get; set; }
        public int Version { get; set; }
        public void DecodeVersionedType(string encoded) {
            string[] split = encoded.Split(SEPARATOR);
            TypeName = split[0];
            Version = int.Parse(split[1]);
        }
        public string EncodeVersionedType() {
            return string.Format("{0}{1}{2}", TypeName, SEPARATOR, Version);
        }
        public int CompareTo(GuidTypeInfo other) {
            int result = TypeName.CompareTo(other.TypeName);
            if (result == 0) {
                result = Version - other.Version;
            }
            return result;
        }
    }

    class GuidInfoComparer : Comparer<GuidTypeInfo> {
        public override int Compare(GuidTypeInfo x, GuidTypeInfo y) {
            int result = x.TypeName.CompareTo(y.TypeName);
            if (result == 0) {
                result = y.Version - x.Version;
            }
            return result;
        }
    }
}
