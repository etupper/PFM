using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Filetypes;

using DatabaseRow = System.Collections.Generic.List<Filetypes.FieldInstance>;

namespace CommonUtilities {
    /*
     * Analyses the db files of a pack whether they contain changes
     * to the original game files. Creates a new pack with only changed entries.
     */
    public class DbFileOptimizer {
        public DbFileOptimizer(Game game) {
            // game packs in correct load order
            packPaths = new PackLoadSequence() {
                IgnorePack = PackLoadSequence.IsDbCaPack
            }.GetPacksLoadedFrom(game.GameDirectory);
#if DEBUG
            Console.WriteLine("packs: {0}", string.Join(",", packPaths));
#endif
        }

        /*
         * Iterator through packed loaded from game.
         */
        public IEnumerable<PackedFile> PackedInGame {
            get {
                return new MultiPackEnumerable(packPaths);
            }
        }
        List<string> packPaths;
        
        /*
         * Main interface method.
         */
        public PackFile CreateOptimizedFile(PackFile toOptimize) {
            PFHeader header = new PFHeader(toOptimize.Header);
            string newPackName = Path.Combine(Path.GetDirectoryName(toOptimize.Filepath),
                                              string.Format("optimized_{0}", Path.GetFileName(toOptimize.Filepath)));
            PackFile result = new PackFile(newPackName, header);
            
            foreach(PackedFile file in toOptimize) {
                PackedFile optimized = CreateOptimizedFile(file);
                if (optimized != null) {
                    result.Add(optimized);
                }
            }
            return result;
        }
  
        /*
         * Create an optimized packed file from the given one.
         */
        PackedFile CreateOptimizedFile(PackedFile toOptimize) {
            PackedFile result = toOptimize;
            // special handling for db files; leave all others as they are.
            if (toOptimize.FullPath.StartsWith("db")) {
                try {
                    DBFile modDbFile = FromPacked(toOptimize);
                    if (modDbFile != null) {
                        DBFile gameDbFile = FindInGamePacks(toOptimize);
                        if (TypesCompatible(modDbFile, gameDbFile)) {
                            DBFileHeader header = new DBFileHeader(modDbFile.Header);
                            DBFile optimizedFile = new DBFile(header, modDbFile.CurrentType);
                            
                            optimizedFile.Entries.AddRange(GetDifferingRows(modDbFile, gameDbFile));
                            if (optimizedFile.Entries.Count != 0) {
                                result.Data = PackedFileDbCodec.GetCodec(toOptimize).Encode(optimizedFile);
                            } else {
                                result = null;
                            }
                        }
                    }
                } catch (Exception e) {
                    Console.Error.WriteLine(e);
                }
            }
            return result;
        }
        
        #region Find Corresponding File in Game
        /*
         * Retrieve the db file corresponding to the given packed file from game.
         */
        DBFile FindInGamePacks(PackedFile file) {
            DBFile result = null;
            string typeName = DBFile.typename(file.FullPath);
            
            foreach (PackedFile gamePacked in PackedInGame) {
                if (DBFile.typename(gamePacked.FullPath).Equals(typeName)) {
                    result = FromPacked(gamePacked);
                    break;
                }
            }

            return result;
        }
        
        /*
         * Find the packed file with the given name in the given pack.
         */
        PackedFile FindInPack(PackFile pack, string name) {
            PackedFile result = null;
            foreach(PackedFile file in pack) {
                if (file.FullPath.Equals(name)) {
                    result  = file;
                    break;
                }
            }
            return result;
        }
        
        /*
         * Create db file from the given packed file.
         * Will not throw an exception on error, but return null.
         */
        DBFile FromPacked(PackedFile packed) {
            DBFile result = null;
            try {
                PackedFileDbCodec codec = PackedFileDbCodec.GetCodec(packed);
                if (codec != null) {
                    result = codec.Decode(packed.Data);
                }
            } catch {}
            return result;
        }
  
        /*
         * Check if the db types of the given files contain the same data types.
         */
        bool TypesCompatible(DBFile file1, DBFile file2) {
            if (file1 == null || file2 == null) {
                return false;
            }
            List<FieldInfo> infos1 = file1.CurrentType.Fields;
            List<FieldInfo> infos2 = file2.CurrentType.Fields;
            
            bool result = infos1.Count == infos2.Count;
            if (result) {
                for (int i = 0; i < infos1.Count; i++) {
                    if (infos1[i].TypeCode != infos2[i].TypeCode) {
                        result = false;
                        break;
                    }
                }
            }
            return result;
        }
        #endregion

        /*
         * Retrieve all rows that are in valueFile, but not in findIn.
         */
        List<DatabaseRow> GetDifferingRows(DBFile valueFile, DBFile findIn) {
            ValueFinder finder = new ValueFinder(findIn);
#if DEBUG
            List<DatabaseRow> rows = new List<DatabaseRow>();
            foreach(DatabaseRow row in valueFile.Entries) {
                if (!finder.ContainsRow(row)) {
                    Console.WriteLine("Entry not found: {0}", string.Join(",", row));
                    rows.Add(row);
                }
            }
#else
            List<DatabaseRow> rows = new List<DatabaseRow>(valueFile.Entries);
            rows.RemoveAll(finder.ContainsRow);
#endif
            return rows;
        }
        
    }
    
    class ValueFinder {
        public ValueFinder(DBFile findIn) {
            gameDbFile = findIn;
        }
        
        DBFile gameDbFile;
        public bool ContainsRow(DatabaseRow checkRow) {
            bool result = false;
            foreach(DatabaseRow row in gameDbFile.Entries) {
                if (SameData(row, checkRow)) {
                    result = true;
                    break;
                }
            }
            return result;
        }

        bool SameData(DatabaseRow row1, DatabaseRow row2) {
            bool result = row1.Count == row2.Count;
            for (int i = 0; result && i < row1.Count; i++) {
                result = row1[i].Value.Equals(row2[i].Value);
            }
            return result;
        }
    }
}

