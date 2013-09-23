using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace Common {
    /*
     * Represents a single Warscape game along with some of its paths and settings.
     * Also keeps a collection of all Warscape games.
     */
    public class Game {
        private static string ROME_INSTALL_DIR = @"C:\Program Files (x86)\Steam\steamapps\common\Total War Rome II";
        public static readonly Game R2TW = new Game("R2TW", "214950", "Rome 2") {
            DefaultPfhType = "PFH4",
            GameDirectory = Directory.Exists(ROME_INSTALL_DIR) ? ROME_INSTALL_DIR : null,
            UserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "The Creative Assembly", "Rome2")
        };
        public static readonly Game STW = new Game("STW", "34330", "Shogun2");
        public static readonly Game NTW = new Game("NTW", "34030", "Napoleon");
        public static readonly Game ETW = new Game("ETW", "10500", "Empire") {
            ScriptFilename = "user.empire_script.txt"
        };
        private static readonly Game[] GAMES = new Game[] {
            R2TW, STW, NTW, ETW
        };

        /*
         * Constructor.
         * <param name="gameId">game name</param>
         * <param name="steam">steam id</param>
         * <param name="gameDir">game pathname below user dir</param>
         * <param name="schriptFile">name of the script file containing mod entries</param>
         */
        public Game(string gameId, string steam, string gameDir, string scriptFile = "user.script.txt") {
            Id = gameId;
            steamId = steam;
            UserDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                   "The Creative Assembly", gameDir);
            ScriptFilename = scriptFile;
            DefaultPfhType = "PFH3";

            retrievers = new RetrieveLocation[] {
                    delegate() { return gameDirectory; },
                    delegate() { return GetInstallLocation(WOW_NODE); },
                    delegate() { return GetInstallLocation(WIN_NODE); }
                };
        }
  
        /*
         * Retrieve list of all known games.
         */
        public static List<Game> Games {
            get {
                List<Game> result = new List<Game>();
                foreach (Game g in GAMES) {
                    result.Add(g);
                }
                return result;
            }
        }
        /*
         * Retrieve Game by given ID.
         */
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
        
        /*
         * Retrieve this game's ID.
         */
        public string Id {
            get; private set;
        }
        /*
         * Retrieve this game's settings directory below the user directory.
         */
        public string UserDir {
            get; private set;
        }
        string gameDirectory;
        private string steamId;
        public static readonly string NOT_INSTALLED = "";
        
        public string DefaultPfhType {
            get; internal set;
        }

        /*
         * Returns the install location of this game or null if it is not installed.
         */
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
            }
        }
        
        delegate string RetrieveLocation();
        RetrieveLocation[] retrievers;
  
        /*
         * Retrieve this game's data directory (containing the game's pack files).
         */
        public string DataDirectory {
            get {
                return Path.Combine(GameDirectory, "data");
            }
        }
        /*
         * Retrieve the name of the script file for this game.
         */
        public string ScriptFilename {
            get;
            private set;
        }
        /*
         * Retrieve the path of the directory containing the script file for this game.
         */
        public string ScriptDirectory {
            get {
                return Path.Combine(UserDir, "scripts");
            }
        }
        /*
         * Retrieve the absolute path to the script file for this game.
         */
        public string ScriptFile {
            get {
                string result = Path.Combine(ScriptDirectory, ScriptFilename);
                return result;
            }
        }
        /*
         * Query if this game is installed.
         */
        public bool IsInstalled {
            get {
                return Directory.Exists(GameDirectory);
            }
        }
        /*
         * Retrieve the schema filename for this game.
         */
        public string SchemaFilename {
            get {
                return string.Format("schema_{0}.xml", Id);
            }
        }
        
        // usual installation nodes in the registry (for installation autodetect)
        private static string WOW_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        private static string WIN_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";

        /*
         * Helper method to retrieve the install location.
         */
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
