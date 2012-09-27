using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace MMS {
    public class PackImporter {
        public PackImporter () {
        }

        static readonly Regex PACK_FILE_RE = new Regex(".pack");
        public void ImportExistingPack(string packFileName) {
            string modName = PACK_FILE_RE.Replace(packFileName, "");
            Mod newMod = new Mod(modName);

            // ToolDataBuilder can't handle paths with spaces in it...
            // avoid this problem by working on temporary data
            string tempDir = Path.Combine(ModTools.Instance.BinariesPath, "temp");
            Directory.CreateDirectory(tempDir);
            string tempPack = Path.Combine(ModTools.Instance.BinariesPath, "temp.pack");
            File.Copy(packFileName, tempPack);

            // I should actually unpack with the PackFileCodec here
            string[] args = new string[] { "unpack", "temp.pack", "temp" };
            string toolbuilder = "ToolDataBuilder.Release.exe";
#if __MonoCS__
            toolbuilder = "ToolDataBuilder.bash";
#endif
            
            Process p = Process.Start(Path.Combine(ModTools.Instance.InstallDirectory, toolbuilder), 
                                      string.Join(" ", args));
            if (p != null) {
                // wait with setting until all data are there
                p.WaitForExit();
            }

            // copy extracted temporary data to actual target directory
            DirectorySynchronizer copyToModDir = new DirectorySynchronizer {
                SourceAccessor = new FileSystemDataAccessor(tempDir),
                TargetAccessor = newMod.Accessor,
                CopyFile = DirectorySynchronizer.AlwaysCopy
            };
            copyToModDir.Synchronize();

            Directory.Delete(tempDir, true);
            File.Delete(tempPack);

            MultiMods.Instance.AddMod(modName);
        }

    }
}

