using Common;
using Filetypes;
using System;
using System.IO;

namespace DbDecoding {
    class MainClass {
        public static void Main(string[] args) {
            DBTypeMap.Instance.initializeFromFile("master_schema.xml");
            foreach (Game game in Game.Games) {
                LoadGameLocationFromFile(game);
                if (game.IsInstalled) {
                    foreach (string packFileName in Directory.EnumerateFiles(game.DataDirectory, "*pack")) {
                        Console.WriteLine("checking {0}", packFileName);
                        PackFile packFile = new PackFileCodec().Open(packFileName);
                        foreach (VirtualDirectory dir in packFile.Root.Subdirectories) {
                            if (dir.Name.Equals("db")) {
                                foreach(PackedFile dbFile in dir.AllFiles) {
                                    if (dbFile.Name.Contains("models_naval")) {
                                        continue;
                                    }
                                    // DBFileHeader header = PackedFileDbCodec.readHeader(dbFile);
                                    DBFile decoded = PackedFileDbCodec.Decode(dbFile);
                                    if (decoded == null && PackedFileDbCodec.readHeader(dbFile).EntryCount != 0) {
                                        Console.WriteLine("failed to read {0} in {1}", dbFile.FullPath, packFile);
                                        string key = DBFile.Typename(dbFile.FullPath);
                                        bool unicode = true;
                                        if (game == Game.R2TW ||
                                            game == Game.ATW) {
                                            unicode = false;
                                        }
                                        DecodeTool.DecodeTool decoder = new DecodeTool.DecodeTool(unicode) { 
                                            TypeName = key, Bytes = dbFile.Data 
                                        };
                                        decoder.ShowDialog();
                                    }
                                }
                            }
                        }
                    }
                } else {
                    Console.Error.WriteLine("Game {0} not installed in {1}", game, game.GameDirectory);
                }
            }
        }
        // load the given game's directory from the gamedirs file
        public static void LoadGameLocationFromFile(Game g) {
            string result = null;
            string GameDirFilepath = "gamedirs.txt";
            // load from file
            if (File.Exists(GameDirFilepath)) {
                // marker that file entry was present
                result = "";
                foreach (string line in File.ReadAllLines(GameDirFilepath)) {
                    string[] split = line.Split(new char[] { Path.PathSeparator });
                    if (split[0].Equals(g.Id)) {
                        result = split[1];
                        break;
                    }
                }
            }
            if (result != null) {
                g.GameDirectory = result;
            }
        }
    }
}
