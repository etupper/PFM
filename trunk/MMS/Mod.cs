using System;
using System.IO;
using System.Collections.Generic;
using Common;

namespace MMS {
    class Mod {
        public Mod(string name) {
            this.name = name;
            
            Directory.CreateDirectory(ModDirectory);
        }

        string name;
        public string Name {
            get {
                return name;
            }
        }

        #region Backup Path and Accessor
        public string ModDirectory {
            get { return Path.Combine(BackupBaseDirectory, Name); }
        }
        // MMS directory
        static string BackupBaseDirectory {
            get {
                return Path.Combine(ModTools.Instance.InstallDirectory, "MMS");
            }
        }
        public IFileDataAccessor Accessor {
            get { return new FileSystemDataAccessor(ModDirectory); }
        }
        #endregion

        #region Synchronizers
        ModDataSynchronizer DataSynchronizer {
            get {
                return new ModDataSynchronizer(this);
            }
        }
        #endregion

        bool isActive;
        public bool IsActive {
            get {
                return isActive;
            }
            set {
                if (IsActive == value || string.IsNullOrEmpty(name)) {
                    return;
                }
                if (value) {
#if DEBUG
                    Console.WriteLine("*** restoring backup of {0}", Name);
#endif
                    // ModTools.RestoreOriginalData();

                    // retrieve data from mod directory
                    DataSynchronizer.SynchronizeFromMod();

                    ModTools.SetBobRulePackName(Name);

                } else if (!string.IsNullOrEmpty(Name)) {
#if DEBUG
                    Console.WriteLine("*** backing up {0}", Name);
#endif
                    // backup files from raw data path to the mod directory
                    // will also restore original data set
                    DataSynchronizer.BackupToMod();
                }
                isActive = value;
            }
        }

        #region Mod Install/Uninstall
        public void Install() {
            string packFilePath = Path.Combine(ModTools.Instance.RetailPath, "data", PackFileName);
            if (!File.Exists(packFilePath)) {
                throw new FileNotFoundException("Pack file not present");
            }
            File.Copy(packFilePath, InstalledPackPath);
            bool contained = false;
            List<string> writeLines = new List<string>();
            if (File.Exists(Game.STW.ScriptFile)) {
                foreach (string line in File.ReadAllLines(Game.STW.ScriptFile)) {
                    string addLine = line;
                    if (line.Contains(PackFileName)) {
                        addLine = ScriptFileEntry;
                        contained = true;
                    }
                    writeLines.Add(addLine);
                }
            }
            if (!contained) {
                writeLines.Add(ScriptFileEntry);
            }
            File.WriteAllLines(Game.STW.ScriptFile, writeLines);
        }
        public void Uninstall() {
            if (File.Exists(InstalledPackPath)) {
                File.Delete(InstalledPackPath);
            }
            if (File.Exists(Game.STW.ScriptFile)) {
                List<string> writeLines = new List<string>();
                foreach (string line in File.ReadAllLines(Game.STW.ScriptFile)) {
                    string addLine = line;
                    if (line.Contains(PackFileName) && !line.StartsWith("#")) {
                        addLine = string.Format("#{0}", ScriptFileEntry);
                    }
                    writeLines.Add(addLine);
                }
                File.WriteAllLines(Game.STW.ScriptFile, writeLines);
            }
        }
        #endregion

        string InstalledPackPath {
            get {
                return Path.Combine(Game.STW.DataDirectory, PackFileName);
            }
        }
        public string ScriptFileEntry {
            get {
                return string.Format("mod \"{0}\";", PackFileName);
            }
        }
        public string PackFileName {
            get {
                return string.Format("{0}.pack", Name);
            }
        }

        #region Overrides
        public override string ToString() {
            return string.Format("{0}{1}", Name, (IsActive ? " *" : "")); ;
        }
        public override bool Equals(object obj) {
            bool result = false;
            if (obj is Mod) {
                result = (obj as Mod).name.Equals(name);
            }
            return result;
        }
        public override int GetHashCode() {
            return name.GetHashCode();
        }
        #endregion
    }
}
