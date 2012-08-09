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
                return dir;
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
                    if (GameChanged != null) {
                        GameChanged();
                    }
                    if (current != null) {
                        Settings.Default.CurrentGame = current.Id;
         
                        // load the appropriate type map
                        current.ApplyGameTypemap();
    
                        // invalidate cache of reference map cache
                        List<PackFile> loaded = new PackLoadSequence() {
                            IgnorePack = PackLoadSequence.IsDbCaPack
                        }.GetPacksLoadedFrom(current.GameDirectory);
                        DBReferenceMap.Instance.GamePacks = loaded;
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

        public List<PackFile> GetPacksLoadedFrom(string directory) {
            List<string> paths = new List<string>();
            List<PackFile> result = new List<PackFile>();
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
                    DateTime start = DateTime.Now;
                    PackFile file = new PackFileCodec().Open(p);
                    if (ContainsDbData(file)) {
                        result.Add(file);
                    }
                    Console.WriteLine("{0} for {1}", DateTime.Now.Subtract(start), Path.GetFileName(p));
                });
                result.Sort(new PackLoadOrder(result));
            }
            return result;
        }
        
        #region Pack filtering
        static readonly string[] EXCLUDE_PREFIXES = { 
                                                        "local", "models", "sound", "terrain", 
                                                        "anim", "ui" };
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
        static bool ContainsDbData(PackFile pack) {
            // check if file contains a db directory
            bool result = false;
            foreach (VirtualDirectory subDir in pack.Root.Subdirectories) {
                if (subDir.Name.Equals("db")) {
                    result = true;
                    break;
                }
            }
            return result;
        }
        #endregion
    }

    public class PackLoadOrder : Comparer<PackFile> {
        private static List<PackType> Ordered = new List<PackType>(new PackType[] {
            PackType.Boot, PackType.BootX, PackType.Shader1, PackType.Shader2,
            PackType.Release, PackType.Patch,
            PackType.Mod, PackType.Movie
        });

        Dictionary<string, PackFile> nameToFile = new Dictionary<string, PackFile>();
        public PackLoadOrder(ICollection<PackFile> files) {
            foreach (PackFile file in files) {
                nameToFile.Add(Path.GetFileName(file.Filepath), file);
            }
        }
        public override int Compare(PackFile p1, PackFile p2) {
            int index1 = Ordered.IndexOf(p1.Header.Type);
            int index2 = Ordered.IndexOf(p2.Header.Type);
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
        bool Obsoletes(PackFile p1, PackFile p2) {
            if (p1.Header.ReplacedPackFileNames.Count == 0) {
                return false;
            }
            bool result = p1.Header.ReplacedPackFileNames.Contains(Path.GetFileName(p2.Filepath));
            if (!result) {
                foreach (string name in p1.Header.ReplacedPackFileNames) {
                    PackFile otherCandidate;
                    if (nameToFile.TryGetValue(name, out otherCandidate)) {
                        result = Obsoletes(otherCandidate, p2);
                    }
                    if (result) {
                        break;
                    }
                }
            }
            if (result) {
                Console.WriteLine("{0} obsoletes {1}", Path.GetFileName(p1.Filepath), Path.GetFileName(p2.Filepath));
            }
            return result;
        }
    }
}

