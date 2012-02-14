using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common {
    public class DBReferenceMap {
        public static readonly DBReferenceMap Instance = new DBReferenceMap();
        Dictionary<string, SortedSet<string>> valueCache = new Dictionary<string, SortedSet<string>>();
        PackFile lastPack = null;

        private DBReferenceMap() {
        }

        PackFile LastPack {
            get { return lastPack; }
            set {
                if ((value != null && lastPack != null) &&
                    (value.Filepath != lastPack.Filepath)) {
                    // clear cache when using another pack file
                    valueCache.Clear();
                }
                lastPack = value;
            }
        }

        /*
        public void validateReferences(string directory, PackFile pack) {
            LastPack = pack;
            // verify dependencies
            foreach (string fromMap in references.Keys) {
                foreach (TableReference reference in references[fromMap]) {
                    if (reference.fromMap == "ancillary_to_effects") {
                        Console.WriteLine("ok");
                    }
                    SortedSet<string> values = collectValues (reference.fromMap, pack);
                    SortedSet<string> allowed = collectValues (reference.toMap, pack);
                    if (values != null && allowed != null) {
                        foreach (string val in values) {
                            if (val != "" && !allowed.Contains (val)) {
                                Console.WriteLine("value '{0}' in {1}:{2} does not fulfil reference {3}:{4}",
                                    val, reference.fromMap, reference.fromIndex, reference.toMap, reference.toIndex);
                            }
                        }
                    }
                }
            }
        }
         * */

        public SortedSet<string> this[string key] {
            get {
                return valueCache[key];
            }
        }

        SortedSet<string> collectValues(string reference, PackFile pack) {
            if (pack != lastPack) {
                lastPack = pack;
            }
            SortedSet<string> result = new SortedSet<string>();
            if (valueCache.TryGetValue(reference, out result)) {
                return result;
            }
            string[] split = reference.Split('.');
            string dbFileName = split[0];
            string fieldName = split[1];
            string dbFullPath = Path.Combine("db", dbFileName);

            foreach (string packfileName in pack.FileList) {
                if (packfileName.StartsWith(dbFullPath)) {
                    string type = DBFile.typename(packfileName);
                    result = new SortedSet<string>();
                    PackedFile file = pack[packfileName];
                    DBFile dbFile = new PackedFileDbCodec().readDbFile(file);
                    int index = -1;
                    for (int i = 0; i < dbFile.CurrentType.fields.Count; i++) {
                        if (dbFile.CurrentType.fields[i].Name.Equals(fieldName)) {
                            index = i;
                            break;
                        }
                    }
                    if (index == -1) {
                        return null;
                    }
                    foreach (List<FieldInstance> entry in dbFile.Entries) {
                        string toAdd = entry[index].Value;
                        if (toAdd != null) {
                            result.Add(toAdd);
                        }
                    }
                }
            }
            valueCache.Add(reference, result);
            return result;
        }

        public SortedSet<string> resolveFromPackFile(string key, PackFile packFile) {
            LastPack = packFile;
            if (key.Length == 0) {
                return null;
            }
            return collectValues(key, packFile);
        }
    }
}
