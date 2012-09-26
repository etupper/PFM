using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Common {
    public class Game {
        private static string WOW_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        private static string WIN_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        
        public static readonly Game STW = new Game("STW", "34330", "Shogun2");
        public static readonly Game NTW = new Game("NTW", "34030", "Napoleon");
        public static readonly Game ETW = new Game("ETW", "10500", "Empire", "user.empire_script.txt");
        private static readonly Game[] GAMES = new Game[] {
            STW, NTW, ETW
        };

        public static List<Game> Games {
            get {
                List<Game> result = new List<Game>();
                foreach (Game g in GAMES) {
                    result.Add(g);
                }
                return result;
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
        
        public string Id {
            get; private set;
        }
        private string steamId;
        public string UserDir {
            get; private set;
        }
        string gameDirectory;

        private static string GAME_DIR_FILE = "gamedirs.txt";
        public static readonly string NOT_INSTALLED = "";

        // returns the install location of the game.
        // if it is not 
        public string GameDirectory {
            get {
                string dir = null;
                foreach (RetrieveLocation retrieveLocation in retrievers) {
                    dir = retrieveLocation();
                    if (dir != null) {
                        break;
                    }
                }
                return dir;
            }
            set {
                gameDirectory = value;
                // save to file
                List<string> entries = new List<string>();
                foreach (string entry in File.ReadAllLines(GAME_DIR_FILE)) {
                    string write = entry;
                    if (entry.StartsWith(Id)) {
                        write = string.Format("{0}{1}{2}{3}", Id, Path.PathSeparator, 
                            gameDirectory == null ? NOT_INSTALLED : gameDirectory, Environment.NewLine);
                    }
                    entries.Add(write);
                }
                File.WriteAllLines(GAME_DIR_FILE, entries);
            }
        }
        delegate string RetrieveLocation();
        RetrieveLocation[] retrievers;

        public string LoadLocationFromFile() {
            string result = null;
            // load from file
            if (File.Exists(GAME_DIR_FILE)) {
                // marker that file was present
                result = "";
                foreach (string line in File.ReadAllLines(GAME_DIR_FILE)) {
                    string[] split = line.Split(new char[] { Path.PathSeparator });
                    if (split[0].Equals(Id)) {
                        result = split[1];
                        break;
                    }
                }
            }
            return result;
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
        public string ScriptDirectory {
            get {
                return Path.Combine(UserDir, "scripts");
            }
        }
        public string ScriptFile {
            get {
                string result = Path.Combine(ScriptDirectory, ScriptFilename);
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
        
        public Game(string i, string steam, string gameDir, string scriptFile = "user.script.txt") {
            Id = i;
            steamId = steam;
            UserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "The Creative Assembly", gameDir);
            ScriptFilename = scriptFile;

            retrievers = new RetrieveLocation[] {
                    delegate() { return gameDirectory; },
                    delegate() { return LoadLocationFromFile(); },
                    delegate() { return GetInstallLocation(WOW_NODE); },
                    delegate() { return GetInstallLocation(WIN_NODE); }
                };
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
}

