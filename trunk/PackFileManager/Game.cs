using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Microsoft.Win32;
using PackFileManager.Properties;
using System.Windows.Forms;

namespace PackFileManager {
    public class Game {
        private static string WOW_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        private static string WIN_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        
        private string GAME_DIR_FILE = "gamedirs.txt";
        
        public string Id {
            get; private set;
        }
        private string steamId;
        public string UserDir {
            get; private set;
        }
        public string GameDirectory {
            get {
                string dir = GetInstallLocation(WOW_NODE);
                if (string.IsNullOrEmpty(dir)) {
                    dir = GetInstallLocation(WIN_NODE);
                }
                if (string.IsNullOrEmpty(dir)) {
                    // empty string will be marker that gamedir file has been read, but was empty
                    dir = null;
                    if (File.Exists(GAME_DIR_FILE)) {
                        foreach (string line in File.ReadAllLines(GAME_DIR_FILE)) {
                            string[] split = line.Split(new char[] { Path.PathSeparator });
                            if (split[0].Equals(Id)) {
                                dir = split[1];
                                break;
                            }
                        }
                    }
                    // if there was an empty entry in file, don't ask again
                    if (dir == null) {
                        FolderBrowserDialog dlg = new FolderBrowserDialog() {
                            Description = string.Format("Please point to Location of {0}\nCancel if not installed.", Id)
                        };
                        if (dlg.ShowDialog() == DialogResult.OK) {
                            dir = dlg.SelectedPath;
                        } else {
                            // add empty entry to file for next time
                            dir = "";
                        }
                        string gameDir = string.Format("{0}{1}{2}{3}", Id, Path.PathSeparator, dir, Environment.NewLine);
                        File.AppendAllLines(GAME_DIR_FILE, new string[] { gameDir });
                    }
                }
                return string.IsNullOrEmpty(dir) ? null : dir;
            }
        }
        public string ScriptFilename {
            get;
            private set;
        }
        public string ScriptFile {
            get {
                string result = Path.Combine(UserDir, "scripts", ScriptFilename);
                return result;
            }
        }
        public bool IsInstalled {
            get {
                return Directory.Exists(GameDirectory);
            }
        }
        public string SchemaFilename {
            get {
                return string.Format("schema_{0}.xml", Id);
            }
        }
        
        public static Game ById(string id) {
            Game result = null;
            foreach(Game game in GAMES) {
                if (game.Id.Equals(id)) {
                    result = game;
                    break;
                }
            }
            return result;
        }
        
        private Game(string i, string steam, string gameDir, string scriptFile = "user.script.txt") {
            Id = i;
            steamId = steam;
            UserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "The Creative Assembly", gameDir);
            ScriptFilename = scriptFile;
        }

        public static readonly Game STW = new Game("STW", "34330", "Shogun2");
        public static readonly Game NTW = new Game("NTW", "34030", "Napoleon");
        public static readonly Game ETW = new Game("ETW", "10500", "Empire", "user.empire_script.txt");
        private static readonly Game[] GAMES = new Game[] {
            STW, NTW, ETW
        };

        public static List<Game> GetGames() {
            List<Game> result = new List<Game>();
            foreach (Game g in GAMES) {
                result.Add(g);
            }
            return result;
        }
        
        private string GetInstallLocation(string node) {
            string str = null;
            try {
                string regKey = string.Format(WOW_NODE, steamId);
                str = (string) Registry.GetValue(regKey, "InstallLocation", "");
                // check if directory actually exists
                if (!string.IsNullOrEmpty(str) && !Directory.Exists(str)) {
                    str = null;
                }
            } catch {}
            return str;
        }

        #region Game-specific schema (typemap) handling
        public void ApplyGameTypemap() {
            try {
                string schemaFile = DBTypeMap.Instance.GetUserFilename(Id);
                if (!File.Exists(schemaFile)) {
                    schemaFile = SchemaFilename;
                    if (!File.Exists(schemaFile)) {
                        // rebuild from master schema
                        DBTypeMap.Instance.initializeTypeMap(Path.GetDirectoryName(Application.ExecutablePath));
                        CreateSchemaFile();
                    }
                }
                DBTypeMap.Instance.initializeFromFile(schemaFile);
            } catch { }
        }
        public void CreateSchemaFile() {
            if (IsInstalled && !File.Exists(SchemaFilename)) {
                SchemaOptimizer optimizer = new SchemaOptimizer() {
                    PackDirectory = Path.Combine(GameDirectory, "data"),
                    SchemaFilename = SchemaFilename
                };
                optimizer.FilterExistingPacks();
            }
        }
        #endregion
    }

    public class GameManager {
        public delegate void GameChange();
        public event GameChange GameChanged;
        
        public static readonly GameManager Instance = new GameManager();
        private GameManager() {
            string gameName = Settings.Default.CurrentGame;
            if (!string.IsNullOrEmpty(gameName)) {
                CurrentGame = Game.ById(gameName);
            }
            foreach(Game game in Game.GetGames()) {
                if (CurrentGame != null) {
                    break;
                }
                if (game.IsInstalled) {
                    CurrentGame = game;
                }
            }
            // no game installed?
            if (CurrentGame == null) {
                CurrentGame = Game.STW;
            }
        }
        
        Game current;
        public Game CurrentGame {
            get {
                return current;
            }
            set {
                if (current != value) {
                    current = value != null ? value : Game.STW;
                    if (current != null) {
                        Settings.Default.CurrentGame = current.Id;

                        // load the appropriate type map
                        current.ApplyGameTypemap();

                        // invalidate cache of reference map cache
                        List<string> loaded = new PackLoadSequence() {
                            IgnorePack = PackLoadSequence.IsDbCaPack
                        }.GetPacksLoadedFrom(current.GameDirectory);
                        DBReferenceMap.Instance.GamePacks = loaded;
                    }
                    if (GameChanged != null) {
                        GameChanged();
                    }
                }
            }
        }
    }
    
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
        static readonly string[] EXCLUDE_PREFIXES = { 
                                                        "local", "models", "sound", "terrain", 
                                                        "anim", "ui", "voices" };
        static bool Keep(string f) { return false; }
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
        //static bool ContainsDbData(PackFile pack) {
        //    // check if file contains a db directory
        //    bool result = false;
        //    foreach (VirtualDirectory subDir in pack.Root.Subdirectories) {
        //        if (subDir.Name.Equals("db")) {
        //            result = true;
        //            break;
        //        }
        //    }
        //    return result;
        //}
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
            PFHeader p2Header = nameToHeader[p2];
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

