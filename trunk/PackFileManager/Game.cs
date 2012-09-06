using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Filetypes;
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
        public string DataDirectory {
            get {
                return Path.Combine(GameDirectory, "data");
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
                        ApplyGameTypemap();

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
        
        #region Game-specific schema (typemap) handling
        public void ApplyGameTypemap() {
            try {
                Game game = CurrentGame;
                string schemaFile = DBTypeMap.Instance.GetUserFilename(game.Id);
                if (!File.Exists(schemaFile)) {
                    schemaFile = game.SchemaFilename;
                    if (!File.Exists(schemaFile)) {
                        // rebuild from master schema
                        DBTypeMap.Instance.InitializeTypeMap(Path.GetDirectoryName(Application.ExecutablePath));
                        CreateSchemaFile(game);
                    }
                }
                DBTypeMap.Instance.initializeFromFile(schemaFile);
            } catch { }
        }
        public void CreateSchemaFile(Game game) {
            if (game.IsInstalled && !File.Exists(game.SchemaFilename)) {
                SchemaOptimizer optimizer = new SchemaOptimizer() {
                    PackDirectory = Path.Combine(game.GameDirectory, "data"),
                    SchemaFilename = game.SchemaFilename
                };
                optimizer.FilterExistingPacks();
            }
        }
        #endregion
    }
}

