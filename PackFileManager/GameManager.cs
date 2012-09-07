using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Filetypes;
using PackFileManager.Properties;
using System.Windows.Forms;

namespace PackFileManager {
    public class GameManager {
        public delegate void GameChange();
        public event GameChange GameChanged;
        
        public static readonly GameManager Instance = new GameManager();
        private GameManager() {
            string gameName = Settings.Default.CurrentGame;
            if (!string.IsNullOrEmpty(gameName)) {
                CurrentGame = Game.ById(gameName);
            }
            // correct game install directories 
            // (should be needed for first start only)
            CheckGameDirectories();
            
            foreach(Game game in Game.Games) {
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
        
        private string GAME_DIR_FILE = "gamedirs.txt";
        void CheckGameDirectories() {
            foreach(Game g in Game.Games) {
                if (g.GameDirectory == null) {
                    // empty string will be marker that gamedir file has been read, but was empty
                    string dir = null;
                    if (File.Exists(GAME_DIR_FILE)) {
                        foreach (string line in File.ReadAllLines(GAME_DIR_FILE)) {
                            string[] split = line.Split(new char[] { Path.PathSeparator });
                            if (split[0].Equals(g.Id)) {
                                dir = split[1];
                                break;
                            }
                        }
                    }
                    // if there was an empty entry in file, don't ask again
                    if (dir == null) {
                        FolderBrowserDialog dlg = new FolderBrowserDialog() {
                            Description = string.Format("Please point to Location of {0}\nCancel if not installed.", g.Id)
                        };
                        if (dlg.ShowDialog() == DialogResult.OK) {
                            dir = dlg.SelectedPath;
                        } else {
                            // add empty entry to file for next time
                            dir = "";
                        }
                        string gameDir = string.Format("{0}{1}{2}{3}", g.Id, Path.PathSeparator, dir, Environment.NewLine);
                        File.AppendAllLines(GAME_DIR_FILE, new string[] { gameDir });
                    }
                    g.GameDirectory = string.IsNullOrEmpty(dir) ? null : dir;
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

