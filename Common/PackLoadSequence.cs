using System;
using System.IO;
using System.Collections.Generic;

namespace Common {
    public class PackLoadSequence {
        private Predicate<string> ignore;
        public Predicate<string> IgnorePack {
            get { 
                return ignore != null ? ignore : Keep;
            }
            set {
                ignore = (value != null) ? value : Keep;
            }
        }

        public List<string> GetPacksLoadedFrom(string directory) {
            List<string> paths = new List<string>();
            List<string> result = new List<string>();
            if (Directory.Exists(directory)) {
                directory = Path.Combine(directory, "data");
                // remove obsoleted packs
                List<string> obsoleted = new List<string>();
                foreach (string filename in Directory.EnumerateFiles(directory, "*.pack")) {
                    if (!IgnorePack(filename)) {
                        paths.Add(filename);
                    }
                }
                paths.RemoveAll(delegate(string pack) {
                    return obsoleted.Contains(pack);
                });
                paths.ForEach(p => {
#if DEBUG
                    DateTime start = DateTime.Now;
#endif
                    // more efficient to use the enumerable class instead of instantiating an actual PackFile
                    // prevents having to parse all contained file names
                    PackedFileEnumerable packedFiles = new PackedFileEnumerable(p);
                    foreach (PackedFile file in packedFiles) {
                        if (file.FullPath.StartsWith("db")) {
                            result.Add(p);
                            break;
                        }
                    }
#if DEBUG
                    Console.WriteLine("{0} for {1}", DateTime.Now.Subtract(start), Path.GetFileName(p));
#endif
                });
                result.Sort(new PackLoadOrder(result));
            }
            return result;
        }
        
        #region Pack filtering
        static bool Keep(string f) { return false; }
        static readonly string[] EXCLUDE_PREFIXES = { 
                                                        "local", "models", "sound", "terrain", 
                                                        "anim", "ui", "voices" };
        public static bool IsDbCaPack(string filename) {
            foreach (string exclude in EXCLUDE_PREFIXES) {
                if (Path.GetFileName(filename).StartsWith(exclude)) {
                    return true;
                }
            }
            PFHeader pack = PackFileCodec.ReadHeader(filename);
            bool result = (pack.Type != PackType.Patch) && (pack.Type != PackType.Release);
            return result;
        }
        #endregion
    }

    public class PackLoadOrder : Comparer<string> {
        private static List<PackType> Ordered = new List<PackType>(new PackType[] {
            PackType.Boot, PackType.BootX, PackType.Shader1, PackType.Shader2,
            PackType.Release, PackType.Patch,
            PackType.Mod, PackType.Movie
        });

        Dictionary<string, PFHeader> nameToHeader = new Dictionary<string, PFHeader>();
        Dictionary<string, string> nameToPath = new Dictionary<string, string>();
        public PackLoadOrder(ICollection<string> files) {
            foreach (string file in files) {
                nameToPath.Add(Path.GetFileName(file), file);
                nameToHeader.Add(file, PackFileCodec.ReadHeader(file));
            }
        }
        public override int Compare(string p1, string p2) {
            PFHeader p1Header = nameToHeader[p1];
            PFHeader p2Header = nameToHeader[p2];
            int index1 = Ordered.IndexOf(p1Header.Type);
            int index2 = Ordered.IndexOf(p2Header.Type);
            int result = index2 - index1;
            if (result == 0) {
                if (Obsoletes(p1, p2)) {
                    result = -1;
                } else if (Obsoletes(p2, p1)) {
                    result = 1;
                }
            }
            return result;
        }
        bool Obsoletes(string p1, string p2) {
            PFHeader p1Header = nameToHeader[p1];
            if (p1Header.ReplacedPackFileNames.Count == 0) {
                return false;
            }
            bool result = p1Header.ReplacedPackFileNames.Contains(Path.GetFileName(p2));
            if (!result) {
                foreach (string name in p1Header.ReplacedPackFileNames) {
                    string otherCandidate;
                    if (nameToPath.TryGetValue(name, out otherCandidate)) {
                        result = Obsoletes(otherCandidate, p2);
                    }
                    if (result) {
                        break;
                    }
                }
            }
#if DEBUG
            //if (result) {
            //    Console.WriteLine("{0} obsoletes {1}", Path.GetFileName(p1), Path.GetFileName(p2));
            //}
#endif
            return result;
        }
    }
}

