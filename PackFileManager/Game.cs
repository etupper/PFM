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
        public string ScriptFile {
            get {
                string result = Path.Combine(UserDir, "scripts", "user.script.txt");
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
        
        private Game(string i, string steam, string gameDir) {
            Id = i;
            steamId = steam;
            UserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "The Creative Assembly", gameDir);
        }

        public static readonly Game STW = new Game("STW", "34330", "Shogun2");
        public static readonly Game NTW = new Game("NTW", "34030", "Napoleon");
        public static readonly Game ETW = new Game("ETW", "10500", "Empire");
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
                        }.GetPacksLoadedFrom(CurrentGame.GameDirectory);
                        DBReferenceMap.Instance.GamePacks = loaded;
                    }
                }
            }
        }
    }
    
    public class PackLoadSequence {
        private Predicate<PackFile> ignore;
        public Predicate<PackFile> IgnorePack {
            get { 
                return ignore != null ? ignore : Keep;
            }
            set {
                ignore = (value != null) ? value : Keep;
            }
        }
        
        public List<PackFile> GetPacksLoadedFrom(string directory) {
            List<PackFile> result = new List<PackFile>();
            if (Directory.Exists(directory)) {
                // remove obsoleted packs
                List<string> obsoleted = new List<string>();
                foreach (string filename in Directory.EnumerateFiles(directory, "*pack")) {
                    PackFile pack = new PackFileCodec().Open(filename);
                    if (!IgnorePack(pack)) {
                        result.Add(pack);
                        foreach (string replacedFile in pack.Header.ReplacedPackFileNames) {
                            obsoleted.Add(Path.Combine(directory, replacedFile));
                        }
                    }
                }
                result.RemoveAll(delegate(PackFile pack) {
                    return obsoleted.Contains(pack.Filepath);
                });
                result.Sort(LoadOrder);
            }
            return result;
        }
        
        #region Pack filtering
        static bool Keep(PackFile f) { return false; }
        public static bool IsDbCaPack(PackFile pack) {
            bool result = (pack.Type == PackType.Patch) || (pack.Type == PackType.Release);
            return result;
        }
        #endregion
  
        #region Pack load order
        private static List<PackType> Ordered = new List<PackType>(new PackType[] {
            PackType.Boot, PackType.BootX, PackType.Shader1, PackType.Shader2,
            PackType.Release, PackType.Patch,
            PackType.Mod, PackType.Movie
        });
        static int LoadOrder(PackFile p1, PackFile p2) {
            int index1 = Ordered.IndexOf(p1.Header.Type);
            int index2 = Ordered.IndexOf(p2.Header.Type);
            return index2 - index1;
        }
        #endregion
    }
}

