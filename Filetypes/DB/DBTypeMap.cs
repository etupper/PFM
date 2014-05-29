using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Filetypes {
    /*
     * Class mapping db type names to type infos able to decode them.
     * Contains methods querying type infos for several situations.
     * A type info can be determined by type name and version number (ETW) or
     * by a GUID, type name and version number (NTW and later).
     * Not all DB files contain all information relevant to determining the exact info
     * they are encoded as, therefore often several type infos may be applicable for a given file
     * without reading the whole table.
     */
    public class DBTypeMap : IEnumerable<TypeInfo> {
        public static readonly string MASTER_SCHEMA_FILE_NAME = "master_schema.xml";
        public static readonly string SCHEMA_USER_FILE_NAME = "schema_user.xml";
        public static readonly string MODEL_SCHEMA_FILE_NAME = "schema_models.xml";
  
        List<TypeInfo> typeInfos = new List<TypeInfo>();
        public List<TypeInfo> AllInfos {
            get {
                return typeInfos;
            }
        }

        /*
         * Singleton access.
         */
        static readonly DBTypeMap instance = new DBTypeMap();        
        public static DBTypeMap Instance {
            get {
                return instance;
            }
        }
        private DBTypeMap() {
            // prevent instantiation
        }
        
        /*
         * Query if any schema has been loaded.
         */
        public bool Initialized {
            get {
                return typeInfos.Count != 0;
            }
        }

        /*
         * A list of schema files that may be present to contain schema data.
         * The user file is intended to store the data for a user after he has edited a field name or suchlike
         * so he is able to send it in for us to integrate; but since now schema files are split up per game,
         * that is not really viable anymore because it would only work for the game he currently saved as.
         */
        public static readonly string[] SCHEMA_FILENAMES = {
            SCHEMA_USER_FILE_NAME, MASTER_SCHEMA_FILE_NAME
        };

        /*
         * Retrieve all infos currently loaded for the given table,
         * either in the table/version format or from the GUID list.
         */
        public List<TypeInfo> GetAllInfos(string table) {
            List<TypeInfo> result = new List<TypeInfo>();
            typeInfos.ForEach(t => { if (t.Name.Equals(table)) { 
                    result.Add (t);
                }});
            return result;
        }
        /*
         * Get all infos matching the given table and version.
         * There may be more than one because sometimes, there are several GUIDs with
         * the same type/version but different structures.
         */
        public List<TypeInfo> GetVersionedInfos(string key, int version) {
            List<TypeInfo> result = new List<TypeInfo>();
            foreach(TypeInfo info in GetAllInfos(key)) {
                List<FieldInfo> fields = info.ForVersion(version);
                if (fields.Count > 0) {
                    TypeInfo newInfo = new TypeInfo(fields) {
                        Name = key, Version = info.Version
                    };
                    if (fields.Count == info.Fields.Count) {
                        newInfo.ApplicableGuids.AddRange(info.ApplicableGuids);
                    }
                    AddOrMerge(result, newInfo);
                }
            }
            result.Sort(new BestVersionComparer { TargetVersion = version });
#if DEBUG
            Console.WriteLine("Returning {0} infos for {1}/{2}", result.Count, key, version);
#endif
            return result;
        }

        /*
         * Add the given type info to the given list, or add its guid to an
         * existing entry if one with the same structure is found.
         */
        void AddOrMerge(List<TypeInfo> list, TypeInfo toAdd) {
            foreach(TypeInfo info in list) {
                if (Enumerable.SequenceEqual(info.Fields, toAdd.Fields)) {
                    // merge into existing entry
                    info.ApplicableGuids.AddRange(toAdd.ApplicableGuids);
                    return;
                }
            }
            list.Add(toAdd);
        }

        #region Initialization / IO
        /*
         * Read schema from given directory, in the order of the SCHEMA_FILENAMES.
         */
        public void InitializeTypeMap(string basePath) {
            foreach(string file in SCHEMA_FILENAMES) {
                string xmlFile = Path.Combine(basePath, file);
                if (File.Exists(xmlFile)) {
                    initializeFromFile(xmlFile);
                    break;
                }
            }
        }
        /*
         * Load the given schema xml file.
         */
        public void initializeFromFile(string filename) {
            XmlImporter importer = null;
            using (Stream stream = File.OpenRead(filename)) {
                importer = new XmlImporter(stream);
                importer.Import();
            }
            typeInfos = importer.Imported;
            if (File.Exists(MODEL_SCHEMA_FILE_NAME)) {
                importer = null;
                using (Stream stream = File.OpenRead(MODEL_SCHEMA_FILE_NAME)) {
                    importer = new XmlImporter(stream);
                    importer.Import();
                }
            }
        }
        /*
         * Stores the whole schema to a file at the given directory with the given suffix.
         */
        public void SaveToFile(string path, string suffix) {
            string filename = Path.Combine(path, GetUserFilename(suffix));
            string backupName = filename + ".bak";
            if (File.Exists(filename)) {
                File.Copy(filename, backupName);
            }
#if DEBUG
            Console.WriteLine("saving schema file {0}", filename);
#endif
            var stream = File.Create(filename);
            new XmlExporter(stream).Export();
            stream.Close();
            if (File.Exists(backupName)) {
                File.Delete(backupName);
            }
        }
        #endregion
  
        #region Setting Changed Definitions
        public void SetByName(string key, List<FieldInfo> setTo) {
            typeInfos.Add(new TypeInfo(setTo) {
                Name = key
            });
        }
        public void SetByGuid(string guid, string tableName, int version, List<FieldInfo> setTo) {
            bool found = false;
            foreach(TypeInfo info in typeInfos) {
                if (info.Fields.SequenceEqual(setTo)) {
                    info.ApplicableGuids.Add(guid);
                    found = true;
                    break;
                } else if (info.ApplicableGuids.Contains(guid)) {
                    info.ApplicableGuids.Remove(guid);
                }
            }
            if (!found) {
                typeInfos.Add(new TypeInfo(setTo) {
                    Name = tableName,
                    Version = version
                });
            }
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
                SortedSet<string> result = new SortedSet<string>();
                typeInfos.ForEach(t => { if (!result.Contains(t.Name)) { 
                    result.Add(t.Name);
                }});  
                return new List<string>(result);
            }
        }
  
        /*
         * Retrieve the highest version for the given type.
         */
        public int MaxVersion(string type) {
            int result = 0;
            typeInfos.ForEach(t => result = Math.Max(t.Version, result));
            return result;
        }
        /*
         * Query if the given type is supported at all.
         */
        public bool IsSupported(string type) {
            foreach(TypeInfo info in typeInfos) {
                if (info.Name.Equals(type)) {
                    return true;
                }
            }
            return false;
        }
        #endregion
        
        /*
         * Note:
         * The names of the TypeInfos iterated here cannot be changed using the
         * enumeration; the FieldInfo lists and contained FieldInfos can.
         */
        public IEnumerator<TypeInfo> GetEnumerator() {
            return typeInfos.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
    
    /*
     * Class defining a db type by GUID. They do still carry their type name
     * and a version number along; the name/version tuple may not be unique though.
     */
    public class GuidTypeInfo : IComparable<GuidTypeInfo> {
        public GuidTypeInfo(string guid) : this(guid, "", 0) {}
        public GuidTypeInfo(string guid, string type, int version) {
            Guid = guid;
            TypeName = type;
            Version = version;
        }
        public string Guid { get; set; }
        public string TypeName { get; set; }
        public int Version { get; set; }
        /*
         * Comparable (mostly to sort the master schema for easier version control).
         */
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
        #region Framework overrides
        public override bool Equals(object obj) {
            bool result = obj is GuidTypeInfo;
            if (result) {
                if (string.IsNullOrEmpty(Guid)) {
                    result = (obj as GuidTypeInfo).TypeName.Equals(TypeName);
                    result &= (obj as GuidTypeInfo).Version.Equals(Version);
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
        #endregion
    }

    /*
     * Comparer for two guid info instances.
     */
    class GuidInfoComparer : Comparer<GuidTypeInfo> {
        public override int Compare(GuidTypeInfo x, GuidTypeInfo y) {
            int result = x.TypeName.CompareTo(y.TypeName);
            if (result == 0) {
                result = y.Version - x.Version;
            }
            return result;
        }
    }

    /*
     * Compares two versioned infos to best match a version being looked for.
     */
    class BestVersionComparer : IComparer<TypeInfo> {
        public int TargetVersion { get; set; }
        public int Compare(TypeInfo info1, TypeInfo info2)
        {
            int difference1 = info1.Version - TargetVersion;
            int difference2 = info2.Version - TargetVersion;
            return difference2 - difference1;
        }
    }
}
