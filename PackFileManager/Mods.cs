using Common;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PackFileManager.Properties;

namespace PackFileManager {
    public class Mod {
        public string Name { get; set; }
        public string BaseDirectory { get; set; }
    }

    public class ModManager {
        public static readonly ModManager Instance = new ModManager();
        public delegate void ModChangeEvent(string newCurrentMod);
        public event ModChangeEvent CurrentModChanged;

        private ModManager() {
            mods = decodeMods(Settings.Default.ModList);
        }
        private Dictionary<string, string> mods;
        public string AddMod() {
            string result = null;
            InputBox box = new InputBox { Text = "Enter Mod Name:", Input = "my_mod" };
            if (box.ShowDialog() == System.Windows.Forms.DialogResult.OK && box.Input.Trim() != "") {
                string modName = box.Input;
                FolderBrowserDialog dialog = new FolderBrowserDialog {
                    SelectedPath = Settings.Default.LastPackDirectory
                };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    Mod mod = new Mod {
                        Name = modName,
                        BaseDirectory = dialog.SelectedPath
                    };

                    // create new mod file to start off with
                    result = Path.Combine(mod.BaseDirectory, string.Format("{0}.pack", modName));
                    if (Directory.Exists(mod.BaseDirectory) && !File.Exists(result)) {
                        var header = new PFHeader("PFH3") {
                            Type = PackType.Mod,
                            Version = 0,
                            FileCount = 0,
                            ReplacedPackFileNames = new List<string>(),
                            DataStart = 0x20
                        };
                        PackFile newFile = new PackFile(result, header);
                        new PackFileCodec().writeToFile(result, newFile);
                    }

                    SetMod(mod);

                    // open existing CA pack or create new pack
                    string shogunPath = IOFunctions.GetShogunTotalWarDirectory();
                    if (shogunPath != null && Directory.Exists(shogunPath)) {
                        OpenFileDialog packOpenFileDialog = new OpenFileDialog {
                            InitialDirectory = Path.Combine(shogunPath, "data"),
                            Filter = IOFunctions.PACKAGE_FILTER,
                            Title = "Open pack to extract basic data from"
                        };
                        if (packOpenFileDialog.ShowDialog() == DialogResult.OK) {
                            result = packOpenFileDialog.FileName;
                        }
                    }
                }
            }
            return result;
        }
        public List<string> ModNames {
            get {
                List<string> result = new List<string>();
                foreach (var entry in decodeMods(Settings.Default.ModList)) {
                    result.Add(entry.Key);
                }
                return result;
            }
        }
        public void SetCurrentMod(string modname) {
            Settings.Default.CurrentMod = modname;
            if (CurrentModChanged != null) {
                CurrentModChanged(modname);
            }
        }
        public void DeleteCurrentMod() {
            mods.Remove(Settings.Default.CurrentMod);
            Settings.Default.ModList = encodeMods(mods);
            SetCurrentMod("");
        }

        public void InstallCurrentMod() {
            string targetDir = IOFunctions.GetShogunTotalWarDirectory();
            if (targetDir == null) {
                throw new FileNotFoundException(string.Format("Shogun install directory not found"));
            }
            targetDir = Path.Combine(targetDir, "data");
            string targetFile = Path.Combine(targetDir, ModPackName);
            if (File.Exists(FullModPath) && Directory.Exists(targetDir)) {
                
                // copy to data directory
                File.Copy(FullModPath, targetFile, true);
                
                // add entry to user.script.txt if it's a mod file
                using(BinaryReader reader = new BinaryReader(File.OpenRead(targetFile))) {
                    PFHeader header = new PackFileCodec().readHeader(reader);
                    if (header.Type == PackType.Mod) {
                        string modEntry = ModScriptFileEntry;
                        string scriptFile = GetUserScriptPath();
                        List<string> linesToWrite = new List<string>();
                        if (File.Exists(scriptFile)) {
                            // retain all other mods in the script file; will add our mod afterwards
                            foreach(string line in File.ReadAllLines(scriptFile, Encoding.Unicode)) {
                                if (!line.Contains(modEntry)) {
                                    linesToWrite.Add(line);
                                }
                            }
                        }
                        if (!linesToWrite.Contains(modEntry)) {
                            linesToWrite.Add(modEntry);
                        }
                        File.WriteAllLines(scriptFile, linesToWrite, Encoding.Unicode);
                    }
                }
            }
        }

        public void UninstallCurrentMod() {
            string targetDir = IOFunctions.GetShogunTotalWarDirectory();
            if (targetDir == null) {
                throw new FileNotFoundException(string.Format("Shogun install directory not found"));
            }

            string targetFile = Path.Combine(targetDir, "data", ModPackName);
            if (File.Exists(targetFile)) {
                File.Move(targetFile, string.Format("{0}.old", targetFile));
            }

            string modEntry = ModScriptFileEntry;
            string scriptFile = GetUserScriptPath();
            List<string> linesToWrite = new List<string>();
            if (File.Exists(scriptFile)) {
                // retain all other mods in the script file
                foreach(string line in File.ReadAllLines(scriptFile, Encoding.Unicode)) {
                    if (!line.Contains(modEntry)) {
                        linesToWrite.Add(line);
                    } else {
                        linesToWrite.Add(string.Format("#{0}", modEntry));
                    }
                }
                File.WriteAllLines(scriptFile, linesToWrite, Encoding.Unicode);
            }
        }
        
        string ModScriptFileEntry {
            get {
                return string.Format("mod \"{0}.pack\";", Settings.Default.CurrentMod);
            }
        }
        public string ModPackName {
            get {
                return string.Format("{0}.pack", Settings.Default.CurrentMod);
            }
        }
        public string FullModPath {
            get {
                return Path.Combine(CurrentModDirectory, ModPackName);
            }
        }
        
        string GetUserScriptPath() {
            string scriptFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            scriptFile = Path.Combine(scriptFile, "The Creative Assembly", "Shogun2", "scripts", "user.script.txt");
            return scriptFile;
        }
        
        public void SetMod(Mod mod) {
            mods[mod.Name] = mod.BaseDirectory;
            Settings.Default.ModList = encodeMods(mods);
            SetCurrentMod(mod.Name);
        }
        public string CurrentModDirectory {
            get {
                return mods[Settings.Default.CurrentMod];
            }
        }
        private Dictionary<string, string> decodeMods(string encoded) {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] entries = encoded.Split(new string[] { "@@@" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries) {
                string[] nameDirTuple = entry.Split(Path.PathSeparator);
                try {
                    result[nameDirTuple[0]] = nameDirTuple[1];
                } catch { }
            }
            return result;
        }
        public string encodeMods(Dictionary<string, string> mods) {
            string result = "";
            foreach (var key in mods.Keys) {
                result += string.Format("{0}{1}{2}{3}", key, Path.PathSeparator, mods[key], "@@@");
            }
            return result;
        }
    }

    public class ModMenuItem : ToolStripMenuItem {
        public ModMenuItem(string title, string modName)
            : base(title) {
            string currentMod = Settings.Default.CurrentMod;
            Checked = currentMod == modName;
            ModManager.Instance.CurrentModChanged += CheckSelection;
            Tag = modName;
        }
        protected override void OnClick(EventArgs e) {
            ModManager.Instance.SetCurrentMod(Tag as string);
        }
        private void CheckSelection(string mod) {
            Checked = mod == (Tag as string);
        }
    }
}
