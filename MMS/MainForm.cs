using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using MMS.Properties;
using Common;

namespace MMS {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
            
            CheckShogunInstallation();
            
            // string modPath = Settings.Default.ModToolPath;
            if (string.IsNullOrEmpty(Settings.Default.ModToolPath) || !Directory.Exists(Settings.Default.ModToolPath)) {
                SetInstallDirectory();
                if (ModTools.Instance.InstallDirectory == null) {
                    throw new Exception("Need installation directory to continue.");
                }
            } else {
                ModTools.Instance.InstallDirectory = Settings.Default.ModToolPath;
            }

            SetInstallDirectoryLabelText();

            FillModList();
            MultiMods.Instance.ModListChanged += FillModList;
            MultiMods.Instance.CurrentModSet += FillModList;

            FormClosing += delegate(object o, FormClosingEventArgs args) {
                new Thread(Cleanup).Start();
#if DEBUG
                // to play around with no settings to imitate original start
                //Settings.Default.ModToolPath = "";
                //Settings.Default.ActiveMod = "";
#else
                this.setActiveToolStripMenuItem.Visible = false;
#endif
                Settings.Default.Save();
            };

        }

        List<Process> startedProcesses = new List<Process>();

        void Cleanup() {
            // the user might close the MMS window before exiting a tool he started,
            // so wait for all processes to finish
            foreach (Process process in startedProcesses) {
                process.Exited -= ProcessExited;
                process.WaitForExit();
            }
            if (MultiMods.Instance.CurrentMod != null) {
                MultiMods.Instance.CurrentMod.IsActive = false;
            }
            // ModTools.RestoreOriginalData();
        }
  
        /*
         * Retrieve the mod currently selected in the list view.
         */
        Mod SelectedMod {
            get {
                Mod result = modList.SelectedItem as Mod;
                if (result == null) {
                    result = MultiMods.Instance.CurrentMod;
                }
                return result;
            }
        }
  
        /*
         * Lets the user choose the mod installation directory.
         */
        private void SetInstallDirectory(object sender = null, EventArgs e = null) {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog() {
            };
            if (folderBrowser.ShowDialog() == DialogResult.OK) {
                ModTools.Instance.InstallDirectory = folderBrowser.SelectedPath;
                Settings.Default.ModToolPath = folderBrowser.SelectedPath;
                SetInstallDirectoryLabelText();
            }
        }
        void CheckShogunInstallation() {
            // prefer loaded from file so the user can force an installation location
            if (!Game.STW.IsInstalled) {
                throw new InvalidOperationException("Cannot find Shogun installation directory.\n"+
                                                    "If you do have it, enter its path in a file called 'gamedirs.txt' and restart.");
            }
        }

        void SetInstallDirectoryLabelText() {
            installDirectoryLabel.Text = string.Format("Mod Tools Location: {0}", ModTools.Instance.InstallDirectory);
        }

        void FillModList() {
            modList.Items.Clear();
            foreach (Mod mod in MultiMods.Instance.Mods) {
                modList.Items.Add(mod);
            }
            modList.SelectedItem = MultiMods.Instance.CurrentMod;
        }

        private void AddMod(object sender, EventArgs e) {
            InputBox inputBox = new InputBox {
                Text = "Enter new Mod name"
            };
            if (inputBox.ShowDialog() == DialogResult.OK) {
                Mod newMod = MultiMods.Instance.AddMod(inputBox.InputValue);
            }
        }

        private void DeleteMod(object sender, EventArgs e) {
            if (SelectedMod != null) {
                DialogResult query = MessageBox.Show("Do you also want to delete the mod data?\n" +
                    "Warning: this can not be undone!",
                    "Also delete data?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                if (query == DialogResult.Cancel) {
                    return;
                }
                bool deleteData = (query == DialogResult.Yes);
                MultiMods.Instance.DeleteMod(SelectedMod, deleteData);
            }
        }

        private void InstallMod(object sender, EventArgs e) {
            if (SelectedMod != null) {
                try {
                    SetMod();
                    SelectedMod.Install();
                    MessageBox.Show("Mod Installed!");
                } catch (Exception ex) {
                    MessageBox.Show(string.Format("Failed to install mod: {0}", ex.Message),
                        "Failed to install mod", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetMod(object sender = null, EventArgs e = null) {
            if (SelectedMod != null) {
                MultiMods.Instance.CurrentMod = SelectedMod;
                modList.SelectedItem = MultiMods.Instance.CurrentMod;
            }
        }

        private void UninstallMod(object sender, EventArgs e) {
            if (SelectedMod != null) {
                SetMod();
                SelectedMod.Uninstall();
                MessageBox.Show("Mod Uninstalled!");
            }
        }

        #region External Processes
        private void LaunchBob(object sender, EventArgs e) {
            LaunchWithMod("BOB.Release.exe");
        }

        private void LaunchTweak(object sender, EventArgs e) {
            LaunchWithMod("TWeak.Release.exe");
        }

        private void LaunchShogun(object sender, EventArgs e) {
            Process.Start(Path.Combine(Game.STW.GameDirectory, "Shogun2.exe"));
        }

        static readonly Regex PACK_FILE_RE = new Regex(".pack");
        private void ImportExistingPack(object sender, EventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = "Pack Files (*.pack)|*.pack|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == DialogResult.OK) {
                try {
                    string modName = Path.GetFileName(dialog.FileName);
                    modName = PACK_FILE_RE.Replace(modName, "");
                    Mod newMod = new Mod(modName);

                    // ToolDataBuilder can't handle paths with spaces in it...
                    // avoid this problem by working on temporary data
                    string tempDir = Path.Combine(ModTools.Instance.BinariesPath, "temp");
                    Directory.CreateDirectory(tempDir);
                    string tempPack = Path.Combine(ModTools.Instance.BinariesPath, "temp.pack");
                    File.Copy(dialog.FileName, tempPack);

                    string[] args = new string[] { "unpack", "temp.pack", "temp" };
                    string toolbuilder = "ToolDataBuilder.Release.exe";
#if __MonoCS__
                    toolbuilder = "ToolDataBuilder.bash";
#endif
                    Process p = Launch(toolbuilder, args);
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
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString(), "Failed to import mod", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LaunchWithMod(string executable, string[] args = null) {
            if (SelectedMod != null) {
                SetMod();
                Launch(executable, args);
            } else {
                MessageBox.Show("Select or set a mod to work with.", "No active mod", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private Process Launch(string executable, string[] args = null) {
#if DEBUG
            Console.WriteLine("Launching {0}", executable);
#endif
            // string executablePath = Path.Combine(ModTools.Instance.BinariesPath, executable);
            string argString = "";
            if (args != null) {
                argString = string.Join(" ", args);
            }
            ProcessStartInfo info = new ProcessStartInfo(executable, argString) {
                WorkingDirectory = ModTools.Instance.BinariesPath
            };
            Process process = Process.Start(info);
            process.EnableRaisingEvents = true;
            process.Exited += ProcessExited;
            startedProcesses.Add(process);
            return process;
        }

        void ProcessExited(object o, EventArgs e) {
            Process p = o as Process;
            if (p != null) {
                startedProcesses.Remove(p);
            }
        }
        #endregion

        private void RestoreData(object sender, EventArgs e) {
            ModTools.RestoreOriginalData();
        }
    }
}
