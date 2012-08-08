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
        private string dir;
        public string BaseDirectory { 
            get {
                return dir;
            }
            set {
                dir = value;
                Settings.Default.ModList = ModManager.Instance.encodeMods();
            }
        }
        private Game game;
        public Game Game { 
            get {
                return game;
            }
            set {
                game = value;
                Settings.Default.ModList = ModManager.Instance.encodeMods();
            }
        }

        public string PackName {
            get {
                return string.Format("{0}.pack", Name);
            }
        }
        public string ModScriptFileEntry {
            get {
                return string.Format("mod \"{0}\";", PackName);
            }
        }
        public string FullModPath {
            get {
                return Path.Combine(BaseDirectory, PackName);
            }
        }

        #region Overrides
        public override bool Equals(object obj) {
            bool result = obj is Mod;
            if (result) {
                result = (obj as Mod).Name.Equals(Name);
            }
            return result;
        }
        public override int GetHashCode() {
            return Name.GetHashCode ();
        }
        #endregion
    }

    public class ModManager {
        public static readonly ModManager Instance = new ModManager();
        public delegate void ModChangeEvent();
        public event ModChangeEvent CurrentModChanged;

        private ModManager() {
            mods = decodeMods(Settings.Default.ModList);
            GameManager.Instance.GameChanged += SetModGame;
        }

        private void SetModGame() {
            if (CurrentMod != null) {
                CurrentMod.Game = GameManager.Instance.CurrentGame;
                Settings.Default.ModList = encodeMods();
            }
        }

        private List<Mod> mods;
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
     
                    mod.Game = GameManager.Instance.CurrentGame;
                    AddMod(mod);

                    // open existing CA pack or create new pack
                    string shogunPath = GameManager.Instance.CurrentGame.GameDirectory;
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
                    result.Add(entry.Name);
                }
                return result;
            }
        }
  
        #region Add, Deletion, Change of Mods
        public void AddMod(Mod mod) {
            mods.Add(mod);
            Settings.Default.ModList = encodeMods();
            CurrentMod = mod;
        }
        public Mod CurrentMod {
            get {
                return FindByName(Settings.Default.CurrentMod);
            }
            set {
                string modName = (value != null) ? value.Name : "";
                Settings.Default.CurrentMod = modName;
                if (CurrentModChanged != null) {
                    CurrentModChanged();
                }
            }
        }
        public void DeleteCurrentMod() {
            if (CurrentMod != null) {
                mods.Remove(CurrentMod);
                Settings.Default.ModList = encodeMods();
                SetCurrentMod("");
            }
        }
        public void SetCurrentMod(string modname) {
            Settings.Default.CurrentMod = modname;
            if (CurrentModChanged != null) {
                CurrentModChanged();
            }
        }
        #endregion
  
        #region Current Mod properties
        public string CurrentModDirectory {
            get {
                string result = (CurrentMod != null) ? CurrentMod.BaseDirectory : null;
                return result;
            }
        }
        #endregion

        private Mod FindByName(string name) {
            Mod result = null;
            foreach(Mod m in mods) {
                if (m.Name.Equals(name)) {
                    result = m;
                    break;
                }
            }
            return result;
        }


        public void InstallCurrentMod() {
            if (CurrentMod == null) {
                throw new InvalidOperationException("No mod set");
            }
            string targetDir = CurrentMod.Game.GameDirectory;
            if (targetDir == null) {
                throw new FileNotFoundException(string.Format("Game install directory not found"));
            }
            targetDir = Path.Combine(targetDir, "data");
            string targetFile = Path.Combine(targetDir, CurrentMod.PackName);
            if (File.Exists(CurrentMod.FullModPath) && Directory.Exists(targetDir)) {
                
                // copy to data directory
                File.Copy(CurrentMod.FullModPath, targetFile, true);
                
                // add entry to user.script.txt if it's a mod file
                using(BinaryReader reader = new BinaryReader(File.OpenRead(targetFile))) {
                    PFHeader header = new PackFileCodec().readHeader(reader);
                    if (header.Type == PackType.Mod) {
                        string modEntry = CurrentMod.ModScriptFileEntry;
                        string scriptFile = GameManager.Instance.CurrentGame.ScriptFile;
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
            if (CurrentMod == null) {
                throw new InvalidOperationException("No mod set");
            }
            
            string targetDir = GameManager.Instance.CurrentGame.GameDirectory;
            if (targetDir == null) {
                throw new FileNotFoundException(string.Format("Install directory not found"));
            }

            string targetFile = Path.Combine(targetDir, "data", CurrentMod.Name);
            if (File.Exists(targetFile)) {
                File.Move(targetFile, string.Format("{0}.old", targetFile));
            }

            string modEntry = CurrentMod.ModScriptFileEntry;
            string scriptFile = GameManager.Instance.CurrentGame.ScriptFile;
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
        
        private List<Mod> decodeMods(string encoded) {
            List<Mod> result = new List<Mod>();
            string[] entries = encoded.Split(new string[] { "@@@" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries) {
                string[] nameDirTuple = entry.Split(Path.PathSeparator);
                try {
                    Game game = (nameDirTuple.Length > 2)?
                        Game.ById(nameDirTuple[2]) :
                        Game.STW;
                    Mod newMod = new Mod {
                        Name = nameDirTuple[0],
                        BaseDirectory = nameDirTuple[1],
                        Game = game
                    };
                    result.Add(newMod);
                } catch { }
            }
            return result;
        }
        public string encodeMods() {
            string result = "";
            foreach (var mod in mods) {
                result += string.Format("{0}{1}{2}{1}{3}{4}", mod, Path.PathSeparator, mod.BaseDirectory, mod.Game.Id, "@@@");
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
        private void CheckSelection() {
            Checked = Settings.Default.CurrentMod.Equals(Tag as string);
        }
    }
}
