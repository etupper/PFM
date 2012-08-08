using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Microsoft.Win32;
using PackFileManager.Properties;

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
    }

    public class GameManager {
        public delegate void GameChange();
        public event GameChange GameChanged;
        
        public static readonly GameManager Instance = new GameManager();
        private GameManager() {
            CurrentGame = Game.ById(Settings.Default.CurrentGame);
        }
        
        Game current;
        public Game CurrentGame {
            get {
                return current;
            }
            set {
                current = value != null ? value : Game.STW;
                if (GameChanged != null) {
                    GameChanged();
                }
                Settings.Default.CurrentGame = current.Id;

                string schemaFile = string.Format("schema_{0}.xml", current.Id);
                if (File.Exists(schemaFile)) {
                    DBTypeMap.Instance.initializeFromFile(schemaFile);
                }
            }
        }
    }
}

