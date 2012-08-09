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
                }
                lastPack = value;
            }
        }
        List<PackFile> gamePacks = new List<PackFile>();
        public List<PackFile> GamePacks {
            get { return gamePacks; }
            set {
                gamePacks = value != null ? value : new List<PackFile>();
                gamePackCache.Clear();
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
            SortedSet<string> result = null;
            string[] split = reference.Split('.');
            string tableName = split[0];
            string fieldName = split[1];
            // string dbFullPath = Path.Combine("db", dbFileName);

            foreach (PackedFile packed in pack.Files) {
                string currentTable = Path.GetDirectoryName(packed.FullPath);
                if (currentTable.LastIndexOf('\\') != -1) {
                    currentTable = currentTable.Substring(currentTable.LastIndexOf('\\') + 1);
                }
                if (currentTable.Equals(tableName)) {
                    result = new SortedSet<string>();
                    DBFile dbFile = PackedFileDbCodec.Decode(packed);
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
            return result;
        }

        public SortedSet<string> resolveReference(string key) {
            if (key.Length == 0) {
                return null;
            }
            List<string> result = new List<string>();
            SortedSet<string> fromPack = new SortedSet<string>();
            if (!valueCache.TryGetValue(key, out fromPack)) {
                fromPack = collectValues(key, CurrentPack);
                valueCache.Add(key, fromPack);
            }
            if (fromPack != null) {
                result.AddRange(fromPack);
            }

            SortedSet<string> fromGame;
            if (!gamePackCache.TryGetValue(key, out fromGame)) {
                foreach(PackFile pack in gamePacks) {
                    fromGame = collectValues(key, pack);
                    if (fromGame != null) {
                        gamePackCache.Add(key, fromGame);
                        break;
                    }
                }
            }
            if (fromGame != null) {
                result.AddRange(fromGame);
            }
            
            SortedSet<string> resultSet = new SortedSet<string>(result);
            return resultSet;
        }
    }
}
