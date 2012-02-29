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

        /*
         * Private to enforce Singleton access.
         */
        private DBReferenceMap () {
        }

        /*
         * The last pack file from which references were resolved; used to invalidate cache
         * when a new pack is opened.
         */
        PackFile LastPack {
            get { return lastPack; }
            set {
                if ((value != null && lastPack != null) &&
                    (value.Filepath != lastPack.Filepath)) {
                    // clear cache when using another pack file
                    valueCache.Clear ();
                }
                lastPack = value;
            }
        }

        /*
         * Resolve references for given key.
         */
        public SortedSet<string> this [string key] {
            get {
                return valueCache [key];
            }
        }

        /*
         * Go through db files contained in the given pack and resolve the reference
         * with the given key (form "tableName.columnName").
         */
        SortedSet<string> collectValues(string reference, PackFile pack) {
            if (pack != lastPack) {
                lastPack = pack;
            }
            SortedSet<string> result = new SortedSet<string> ();
            if (valueCache.TryGetValue (reference, out result)) {
                return result;
            }
            string[] split = reference.Split ('.');
            string dbFileName = split [0];
            string fieldName = split [1];
            string dbFullPath = Path.Combine ("db", dbFileName);
            Console.WriteLine ("looking for {0}", dbFullPath);

            foreach (PackedFile file in pack.Files) {
                if (file.FullPath.Contains (dbFullPath)) {
                    result = new SortedSet<string> ();
                    DBFile dbFile = new PackedFileDbCodec ().readDbFile (file);
                    int index = -1;
                    for (int i = 0; i < dbFile.CurrentType.fields.Count; i++) {
                        if (dbFile.CurrentType.fields [i].Name.Equals (fieldName)) {
                            index = i;
                            break;
                        }
                    }
                    if (index == -1) {
                        return null;
                    }
                    foreach (List<FieldInstance> entry in dbFile.Entries) {
                        string toAdd = entry [index].Value;
                        if (toAdd != null) {
                            result.Add (toAdd);
                        }
                    }
                }
            }
            valueCache.Add (reference, result);
            return result;
        }

        /*
         * Resolve given reference from given pack; returns null if reference key is empty.
         */
        public SortedSet<string> resolveFromPackFile(string key, PackFile packFile) {
            LastPack = packFile;
            if (key.Length == 0) {
                return null;
            }
            return collectValues(key, packFile);
        }
    }
}
