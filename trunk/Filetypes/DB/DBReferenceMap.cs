using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common {
    public class DBReferenceMap {
        public static readonly DBReferenceMap Instance = new DBReferenceMap();
        Dictionary<string, SortedSet<string>> valueCache = new Dictionary<string, SortedSet<string>>();
        Dictionary<string, SortedSet<string>> gamePackCache = new Dictionary<string, SortedSet<string>>();
        Dictionary<string, List<PackedFile>> typeToPackedCache = new Dictionary<string, List<PackedFile>>();
        PackFile lastPack = null;

        private DBReferenceMap() {
        }

        public PackFile CurrentPack {
            get { return lastPack; }
            set {
                if ((value != null && lastPack != null) &&
                    (value.Filepath != lastPack.Filepath)) {
                    // clear cache when using another pack file
                    valueCache.Clear();
                    typeToPackedCache.Clear();
                }
                lastPack = value;
            }
        }
        List<string> gamePacks = new List<string>();
        public List<string> GamePacks {
            get { return gamePacks; }
            set {
                gamePacks = value != null ? value : new List<string>();
                gamePackCache.Clear();
            }
        }

        public SortedSet<string> this[string key] {
            get {
                return valueCache[key];
            }
        }

        SortedSet<string> collectValues(string tableName, string fieldName, IEnumerable<PackedFile> packedFiles) {
            SortedSet<string> result = null;
#if DEBUG
            Console.WriteLine("Looking for {0}:{1} in {2}", tableName, fieldName, packedFiles);
#endif
            // enable load from multiple files
            bool found = false;
            foreach (PackedFile packed in packedFiles) {
                string currentTable = DBFile.typename(packed.FullPath);
                if (currentTable.Equals(tableName)) {
                    found = true;
                    if (result == null) {
                        result = new SortedSet<string>();
                    }
#if DEBUG
                    Console.WriteLine("Found {0}:{1} in {2}", tableName, fieldName, packedFiles);
#endif
                    try {
                        FillFromPacked(result, packed, fieldName);
                    } catch {
                        return null;
                    }
                } else if (found) {
                    // once we're past the files with the correct type, stop searching
                    break;
                } else {
                    // we didn't find the right table type, but cache the PackedFile we created along the way
                    List<PackedFile> cacheFiles;
                    if (!typeToPackedCache.TryGetValue(currentTable, out cacheFiles)) {
                        cacheFiles = new List<PackedFile>();
                        typeToPackedCache.Add(currentTable, cacheFiles);
                    }
                    cacheFiles.Add(packed);
                }
            }
            return result;
        }

        void FillFromPacked(SortedSet<string> result, PackedFile packed, string fieldName) {
            DBFile dbFile = PackedFileDbCodec.Decode(packed);
            int index = -1;
            List<PackedFile> loadedFrom = new List<PackedFile>();
            for (int i = 0; i < dbFile.CurrentType.fields.Count; i++) {
                if (dbFile.CurrentType.fields[i].Name.Equals(fieldName)) {
                    index = i;
                    break;
                }
            }
            if (index == -1) {
                // did not find in file with correct type
                throw new InvalidDataException(string.Format("Did not find field {0} in {1}",
                    fieldName, Path.GetFileName(packed.FullPath)));
            }
            foreach (List<FieldInstance> entry in dbFile.Entries) {
                string toAdd = entry[index].Value;
                if (toAdd != null) {
                    result.Add(toAdd);
                }
            }
        }

        public SortedSet<string> resolveReference(string key) {
            if (key.Length == 0) {
                return null;
            }
#if DEBUG
            Console.WriteLine("resolving reference {0}", key);
#endif
            string[] split = key.Split('.');
            string tableName = split[0];
            string fieldName = split[1];

            List<string> result = new List<string>();
            SortedSet<string> fromPack = new SortedSet<string>();
            if (!valueCache.TryGetValue(key, out fromPack)) {
                fromPack = collectValues(tableName, fieldName, CurrentPack);
                valueCache.Add(key, fromPack);
            }
            if (fromPack != null) {
                result.AddRange(fromPack);
            }

            SortedSet<string> fromGame;
            if (!gamePackCache.TryGetValue(key, out fromGame)) {
                IEnumerable<PackedFile> packedFiles;
                if (typeToPackedCache.ContainsKey(tableName)) {
                    packedFiles = typeToPackedCache[tableName];
                } else {
                    packedFiles = new MultiPackEnumerable(gamePacks);
                }
                fromGame = collectValues(tableName, fieldName, packedFiles);
                if (fromGame != null) {
                    gamePackCache.Add(key, fromGame);
                }
            }
            if (fromGame != null) {
                result.AddRange(fromGame);
            }
            
            SortedSet<string> resultSet = new SortedSet<string>(result);
            return resultSet;
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
    }
}
