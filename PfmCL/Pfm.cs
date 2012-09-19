using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;

namespace PfmCL {
    class Pfm {
        public delegate void PackAction(string packFileName, List<string> containedFiles);

        static void Main(string[] args) {
            if (args.Length < 1) {
                Usage();
                return;
            }

            List<string> arguments = new List<string>(args.Length - 1);
            for (int i = 2; i < args.Length; i++) {
                arguments.Add(args[i]);
            }
            new Pfm(args[0]) {
                PackFileName = args[1],
                ContainedFiles = arguments
            }.Run();
        }

        string PackFileName {
            get;
            set;
        }
        List<string> ContainedFiles {
            get;
            set;
        }
        PackAction action;

        Pfm(string command) {
            switch (command) {
                case "c": 
                    action = CreatePack;
                    break;
                case "x":
                    action = ExtractPack;
                    break;
                case "t":
                    action = ListPack;
                    break;
                case "u":
                    action = UpdatePackReplace;
                    break;
                case "a":
                    action = UpdatePackAddOnly;
                    break;
            }
        }

        public void Run() {
            if (action != null) {
                action(PackFileName, ContainedFiles);
            } else {
                Usage();
            }
        }

        static void Usage() {
            Console.WriteLine("usage: pfm <command> <packFile> [<file1>,...]");
            Console.WriteLine("available commands:");
            Console.WriteLine("'c' to create");
            Console.WriteLine("'x' to extract (no file arguments: extract all)");
            Console.WriteLine("'t' to list contents (ignores file arguments)");
            Console.WriteLine("'u' to update (replaces files with same path)");
        }

        /*
         * Create a new pack containing the given files.
         */
        void CreatePack(string packFileName, List<string> containedFiles) {
            try {
                PFHeader header = new PFHeader("PFH3") {
                    Version = 0,
                    Type = PackType.Mod
                };
                PackFile packFile = new PackFile(packFileName, header);
                foreach (string file in containedFiles) {
                    try {
                        PackedFile toAdd = new PackedFile(file);
                        packFile.Add(toAdd, true);
                    } catch (Exception e) {
                        Console.Error.WriteLine("Failed to add {0}: {1}", file, e.Message);
                    }
                }
                new PackFileCodec().writeToFile(packFileName, packFile);
            } catch (Exception e) {
                Console.Error.WriteLine("Failed to write {0}: {1}", packFileName, e.Message);
            }
        }

        /*
         * Lists the contents of the given pack file.
         * List parameter is ignored.
         */
        void ListPack(string packFileName, List<string> containedFiles) {
            try {
                PackFile pack = new PackFileCodec().Open(packFileName);
                foreach (PackedFile file in pack) {
                    Console.WriteLine(file.FullPath);
                }
            } catch (Exception e) {
                Console.Error.WriteLine("Failed to list contents of {0}: {1}", packFileName, e.Message);
            }
        }

        /*
         * Unpacks the given files from the given pack file, or all if contained files list is empty.
         */
        void ExtractPack(string packFileName, List<string> containedFiles) {
            PackFile pack = new PackFileCodec().Open(packFileName);
            foreach (PackedFile packed in pack) {
                try {
                    if (containedFiles.Count == 0 || containedFiles.Contains(packed.FullPath)) {
                        string systemPath = packed.FullPath.Replace('/', Path.DirectorySeparatorChar);
                        string directoryName = Path.GetDirectoryName(systemPath);
                        if (directoryName.Length != 0 && !Directory.Exists(directoryName)) {
                            Directory.CreateDirectory(directoryName);
                        }
                        using (var fileStream = new MemoryStream(packed.Data)) {
                            fileStream.CopyTo(File.Create(systemPath));
                        }
                    }
                } catch (Exception e) {
                    Console.Error.WriteLine("Failed to extract {0}: {1}", packed.FullPath, e.Message);
                }
            }
        }

        /*
         * Updates the given pack file, adding the files from the given list.
         * Will replace files already present in the pack when the replace parameter is true (default).
         */
        void UpdatePack(string packFileName, List<string> toAdd, bool replace) {
            try {
                PackFile toUpdate = new PackFileCodec().Open(packFileName);
                foreach (string file in toAdd) {
                    try {
                        toUpdate.Add(new PackedFile(file), replace);
                    } catch (Exception e) {
                        Console.Error.WriteLine("Failed to add {0}: {1}", file, e.Message);
                    }
                }
                string tempFile = Path.GetTempFileName();
                new PackFileCodec().writeToFile(tempFile, toUpdate);
                File.Delete(packFileName);
                File.Move(tempFile, packFileName);
            } catch (Exception e) {
                Console.Error.WriteLine("Failed to update {0}: {1}", packFileName, e.Message);
            }
        }

        void UpdatePackReplace(string packFileName, List<string> toAdd) {
            UpdatePack(packFileName, toAdd, true);
        }
        
        /*
         * Updates the given pack file, adding the files from the list;
         * will not replace files already present in the pack.
         */
        void UpdatePackAddOnly(string packFileName, List<string> toAdd) {
            UpdatePack(packFileName, toAdd, false);
        }
    }
}
