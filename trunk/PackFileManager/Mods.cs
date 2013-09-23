using Common;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PackFileManager.Properties;
using CommonDialogs;

namespace PackFileManager {
    /*
     * Class representing a Warscape mod.
     */
    public class Mod {
        /* 
         * Name of the mod.
         * Also expected to be the name of the pack file containing it.
         */
        public string Name { get; set; }
        public delegate void Notification();
        public event Notification GameChanged;

        /*
         * Directory to save mod pack and extract/import data to and from.
         */
        private string dir;
        public string BaseDirectory { 
            get {
                return dir;
            }
            set {
                dir = value;
            }
        }
        
        /*
         * The game to which this mod belongs.
         */
        private Game game;
        public Game Game { 
            get {
                return game;
            }
            set {
                game = value;
                if (GameChanged != null) {
                    GameChanged();
                }
            }
        }

        /*
         * Name of the pack file.
         */
        public string PackName {
            get {
                return string.Format("{0}.pack", Name);
            }
        }
        /*
         * Line to put into the game script file to tell game to load this mod.
         */
        public string ModScriptFileEntry {
            get {
                return string.Format("mod \"{0}\";", PackName);
            }
        }
        /*
         * Full path of the working pack file of this mod.
         */
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
 
    /*
     * Manager for all mods of a user.
     */
    public class ModManager {
        // singleton
        public static readonly ModManager Instance = new ModManager();
        
        /*
         * Allow other code to react to a user changing the current mod.
         */
        public delegate void ModChangeEvent();
        public event ModChangeEvent CurrentModChanged;
  
        /*
         * Read mod list from current settings.
         */
        private ModManager() {
            mods = DecodeMods(Settings.Default.ModList);
            GameManager.Instance.GameChanged += SetModGame;
            SetCurrentMod(Settings.Default.CurrentMod);
        }
  
        /*
         * When the user selects a different game with a mod being worked on,
         * offer to set the mod's game to the newly selected game.
         */
        private void SetModGame() {
            Game currentGame = GameManager.Instance.CurrentGame;
            if (CurrentModSet && !CurrentMod.Game.Id.Equals(currentGame.Id)) {
                string message = string.Format("Game set to {0}.\nDo you want to change the game setting for the current mod {1} (currently {2})?",
                                               currentGame.Id, CurrentMod.Name, CurrentMod.Game.Id);
                DialogResult answer = MessageBox.Show(message, "Modded Game Changed", MessageBoxButtons.YesNo);
                if (answer == DialogResult.Yes) {
                    CurrentMod.Game = currentGame;
                    StoreToSettings();
                }
            }
        }
        
        /*
         * Query if any mod is active at all right now.
         */
        public bool CurrentModSet {
            get {
                return CurrentMod != null;
            }
        }

        private List<Mod> mods;
        public List<string> ModNames {
            get {
                List<string> result = new List<string>();
                foreach (var entry in DecodeMods(Settings.Default.ModList)) {
                    result.Add(entry.Name);
                }
                return result;
            }
        }
  
        #region Add, Deletion, Change of Mods
        public string AddMod() {
            string result = null;
            InputBox box = new InputBox { Text = "Enter Mod Name:", Input = "my_mod" };
            if (box.ShowDialog() == System.Windows.Forms.DialogResult.OK && box.Input.Trim() != "") {
                string modName = box.Input;
                DirectoryDialog dialog = new DirectoryDialog {
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
                        PackFile newFile = new PackFile(result, new PFHeader(GameManager.Instance.CurrentGame.DefaultPfhType));
                        new PackFileCodec().WriteToFile(result, newFile);
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
        public void AddMod(Mod mod) {
            mods.Add(mod);
            mod.GameChanged += delegate { EncodeMods(); };
            StoreToSettings ();
            CurrentMod = mod;
        }
        /*
         * Retrieve the currently active mod.
         */
        public Mod CurrentMod {
            get {
                // the current mod is stored in the settings
                return FindByName(Settings.Default.CurrentMod);
            }
            set {
                string modName = (value != null) ? value.Name : "";
                Settings.Default.CurrentMod = modName;
                if (!string.IsNullOrEmpty(modName)) {
                    GameManager.Instance.CurrentGame = CurrentMod.Game;
                }
                if (CurrentModChanged != null) {
                    CurrentModChanged();
                }
            }
        }
        /*
         * Change currently selected mod by its mod name.
         */
        public void SetCurrentMod(string modname) {
            CurrentMod = FindByName(modname);
        }
        
        /*
         * Delete the currently active mod.
         */
        public void DeleteCurrentMod() {
            if (CurrentMod != null) {
                mods.Remove(CurrentMod);
                StoreToSettings ();
                SetCurrentMod("");
            }
        }
        void StoreToSettings() {
            Settings.Default.ModList = EncodeMods();
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

        #region Install/Uninstall
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
                PFHeader header = PackFileCodec.ReadHeader(targetFile);
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
        #endregion
        
        #region Helpers to Encode/Decode to Settings string 
        static List<Mod> DecodeMods(string encoded) {
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
        static string EncodeMods() {
            string result = "";
            foreach (var mod in Instance.mods) {
                result += string.Format("{0}{1}{2}{1}{3}{4}", mod.Name, Path.PathSeparator, mod.BaseDirectory, mod.Game.Id, "@@@");
            }
            return result;
        }
        #endregion
    }
 
    /*
     * A menu item representing a mod for the user to select.
     */
    public class ModMenuItem : ToolStripMenuItem {
        public ModMenuItem(string title, string modName)
            : base(title) {
            string currentMod = Settings.Default.CurrentMod;
            Checked = currentMod == modName;
            ModManager.Instance.CurrentModChanged += CheckSelection;
            Tag = modName;
        }
        // select this item's mod as current mod in manager when clicked
        protected override void OnClick(EventArgs e) {
            ModManager.Instance.SetCurrentMod(Tag as string);
        }
        // check if the new selected mod is the one referred to by this item
        private void CheckSelection() {
            Checked = Settings.Default.CurrentMod.Equals(Tag as string);
        }
    }
}
