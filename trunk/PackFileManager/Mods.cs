using Common;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PackFileManager.Properties;

namespace PackFileManager {
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
                    SetMod(mod);

                    // open existing CA pack or create new pack
                    OpenFileDialog packOpenFileDialog = new OpenFileDialog {
                        InitialDirectory = Path.Combine(IOFunctions.GetShogunTotalWarDirectory(), "data"),
                        Filter = IOFunctions.PACKAGE_FILTER,
                        Title = "Open pack to extract basic data from"
                    };
                    if (packOpenFileDialog.ShowDialog() == DialogResult.OK) {
                        result = packOpenFileDialog.FileName;
                    } else {
                        result = string.Format("{0}.pack", Settings.Default.CurrentMod);
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
}
