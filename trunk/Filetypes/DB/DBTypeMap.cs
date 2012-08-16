using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Filetypes {
    public class DBTypeMap : IEnumerable<TypeInfo> {
        public static readonly string SCHEMA_FILE_NAME = "schema.xml";
        public static readonly string MASTER_SCHEMA_FILE_NAME = "master_schema.xml";
        public static readonly string SCHEMA_USER_FILE_NAME = "schema_user.xml";

        SortedDictionary<string, List<FieldInfo>> typeMap = new SortedDictionary<string, List<FieldInfo>>();
        SortedDictionary<GuidTypeInfo, List<FieldInfo>> guidMap = new SortedDictionary<GuidTypeInfo, List<FieldInfo>>();

        static readonly DBTypeMap instance = new DBTypeMap();        
        public static DBTypeMap Instance {
            get {
                if (!instance.Initialized) {
                    instance.InitializeTypeMap(Directory.GetCurrentDirectory());
                }
                return instance;
            }
        }

        public static readonly string[] SCHEMA_FILENAMES = {
            SCHEMA_USER_FILE_NAME, MASTER_SCHEMA_FILE_NAME, SCHEMA_FILE_NAME
        };

        private DBTypeMap() {
            // prevent instantiation
        }
        
        #region Type Maps
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
        public bool Initialized {
            get {
                return typeMap.Count != 0 || guidMap.Count != 0;
            }
        }
        #endregion

        public TypeInfo GetVersionedInfo(string guid, string key, int version) {
            TypeInfo result = new TypeInfo {
                Name = key
            };
            GuidTypeInfo info = new GuidTypeInfo(guid, key, version);
            if (!string.IsNullOrEmpty(guid) && guidMap.ContainsKey(info)) {
                result.Fields.AddRange(guidMap[info]);
            } else {
                List<FieldInfo> list;
                if (typeMap.TryGetValue(key, out list)) {
                    result.Fields.AddRange(FilterForVersion(list, version));
                }
            }
            return result;
        }

        #region Initialization / IO
        public void InitializeTypeMap(string basePath) {
            foreach(string file in SCHEMA_FILENAMES) {
                string xmlFile = Path.Combine(basePath, file);
                if (File.Exists(xmlFile)) {
                    initializeFromFile(xmlFile);
                    break;
                }
            }
        }
        public void initializeFromFile(string filename) {
            XmlImporter importer = null;
            using (Stream stream = File.OpenRead(filename)) {
                importer = new XmlImporter(stream);
                importer.Import();
            }
            typeMap = importer.Descriptions;
            guidMap = importer.GuidToDescriptions;
        }

        public void loadFromXsd(string xsdFile) {
            //            typeMap = new XsdParser (xsdFile).loadXsd ();
        }

        public void saveToFile(string path, string suffix) {
            string filename = Path.Combine(path, GetUserFilename(suffix));
            string backupName = filename + ".bak";
            if (File.Exists(filename)) {
                File.Copy(filename, backupName);
            }
            var stream = File.Create(filename);
            new XmlExporter(stream).Export(typeMap, guidMap);
            stream.Close();
            if (File.Exists(backupName)) {
                File.Delete(backupName);
            }
        }
        #endregion
  
        #region Setting Changed Definitions
        public void SetByName(string key, List<FieldInfo> setTo) {
            typeMap[key] = setTo;
        }
        public void SetByGuid(string guid, string tableName, int version, List<FieldInfo> setTo) {
            GuidTypeInfo info = new GuidTypeInfo(guid, tableName, version);
            guidMap[info] = setTo;
        }
        #endregion

        #region Utilities
        /*
         * Create a list containing only the items valid for the given version.
         */
        public static List<FieldInfo> FilterForVersion(List<FieldInfo> list, int version) {
            List<FieldInfo> result = new List<FieldInfo>();
            foreach (FieldInfo d in list) {
                if (d.StartVersion <= version && d.LastVersion >= version) {
                    result.Add(d);
                }
            }
            return result;
        }
        public string GetUserFilename(string suffix) {
            return string.Format(string.Format("schema_{0}.xml", suffix));
        }
        #endregion

        #region Supported Type/Version Queries
        /*
         * Retrieve all supported Type Names.
         */
        public List<string> DBFileTypes {
            get {
                SortedSet<string> result = new SortedSet<string>(typeMap.Keys);
                foreach (GuidTypeInfo info in guidMap.Keys) {
                    result.Add(info.TypeName);
                }
                return new List<string>(result);
            }
        }
  
        /*
         * Retrieve the highest version for the given type.
         */
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
        /*
         * Query if the given type is supported at all.
         */
        public bool IsSupported(string type) {
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
        #endregion
        
        /*
         * Note:
         * The names of the TypeInfos iterated here cannot be changed using the
         * enumeration; the FieldInfo lists and contained FieldInfos can.
         */
        public IEnumerator<TypeInfo> GetEnumerator() {
            return new TypeInfoEnumerator(GuidMap.Keys, TypeMap.Keys);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
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
        public string Guid { get; set; }
        public string TypeName { get; set; }
        public int Version { get; set; }
        public string EncodeVersionedType() {
            return string.Format("{0}{1}{2}", TypeName, SEPARATOR, Version);
        }
        public int CompareTo(GuidTypeInfo other) {
            int result = TypeName.CompareTo(other.TypeName);
            if (result == 0) {
                result = Version - other.Version;
            }
            if (result == 0) {
                result = Guid.CompareTo(other.Guid);
            }
            return result;
        }
        public override bool Equals(object obj) {
            bool result = obj is GuidTypeInfo;
            if (result) {
                if (string.IsNullOrEmpty(Guid)) {
                    result = (obj as GuidTypeInfo).TypeName.Equals(TypeName);
                } else {
                    result = (obj as GuidTypeInfo).Guid.Equals(Guid);
                }
            }
            return result;
        }
        public override int GetHashCode() {
            return Guid.GetHashCode();
        }
        public override string ToString() {
            return string.Format("{1}/{2} # {0}", Guid, TypeName, Version);
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
    
    public class TypeInfoEnumerator : IEnumerator<TypeInfo> {
        IEnumerator<GuidTypeInfo> guidEnumerator;
        IEnumerator<string> typeNameEnumerator;

        bool usingTypes = false;

        public TypeInfoEnumerator(IEnumerable<GuidTypeInfo> guids, IEnumerable<string> types) {
            guidEnumerator = guids.GetEnumerator();
            typeNameEnumerator = types.GetEnumerator();
        }
        
        public bool UsingTypes {
            get {
                return usingTypes;
            }
        }
        public TypeInfo Current {
            get {
                string typeName;
                List<FieldInfo> result;
                if (UsingTypes) {
                    typeName = typeNameEnumerator.Current;
                    result = DBTypeMap.Instance.TypeMap[typeName];
                } else {
                    typeName = guidEnumerator.Current.TypeName;
                    result = DBTypeMap.Instance.GuidMap[guidEnumerator.Current];
                }
                return new TypeInfo(result) {
                    Name = typeName
                };
            }
        }
        object System.Collections.IEnumerator.Current {
            get {
                return Current;
            }
        }
        public void Reset() {
            if (UsingTypes) {
                usingTypes = false;
                typeNameEnumerator.Reset();
            }
            guidEnumerator.Reset();
        }
        public bool MoveNext() {
            bool result;
            if (usingTypes) {
                result = typeNameEnumerator.MoveNext();
            } else {
                result = guidEnumerator.MoveNext();
                if (!result) {
                    usingTypes = true;
                    result = typeNameEnumerator.MoveNext();
                }
            }
            return result;
        }

        public void Dispose() {
            guidEnumerator.Dispose();
            typeNameEnumerator.Dispose();
        }
    }
}
