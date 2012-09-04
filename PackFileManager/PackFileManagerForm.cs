using Common;
using Filetypes;
using DecodeTool;
using PackFileManager.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EsfControl;
using EsfLibrary;

namespace PackFileManager
{
    public partial class PackFileManagerForm : Form {

        public PackFileManagerForm() : this(new string[0]) {}

        private PackFile currentPackFile;
        public PackFile CurrentPackFile {
            get { return currentPackFile; }
            set {
                CloseEditors();

                // register previous and build tree
                currentPackFile = value;
                RefreshTitle();
                EnableMenuItems();
                currentPackFile.Modified += RefreshTitle;
                currentPackFile.Modified += EnableMenuItems;

                DBReferenceMap.Instance.CurrentPack = value;
                
                Refresh ();
            }
        }

        private bool openFileIsModified;
        private string openFilePath;
        private CustomMessageBox search;

        private PackedFile openPackedFile;
  
        #region Editors
        private readonly DBFileEditorControl dbFileEditorControl = new DBFileEditorControl {
            Dock = DockStyle.Fill
        };
        private TextFileEditorControl textFileEditorControl = new TextFileEditorControl { 
            Dock = DockStyle.Fill };
        private ReadmeEditorControl readmeEditorControl;

        private IPackedFileEditor[] editors;
        private IPackedFileEditor[] Editors {
            get { return editors; }
        }
        #endregion

        private IPackedFileEditor[] CreateEditors() {
            return new IPackedFileEditor[] {
#if __MonoCS__
#else
                    // relies on win32 dll, so can't use it on Linux
                    new AtlasFileEditorControl { Dock = DockStyle.Fill },
#endif
                    new ImageViewerControl { Dock = DockStyle.Fill },
                    new LocFileEditorControl { Dock = DockStyle.Fill },
                    new GroupformationEditor { Dock = DockStyle.Fill },
                    new UnitVariantFileEditorControl { Dock = DockStyle.Fill },
                    new PackedEsfEditor { Dock = DockStyle.Fill },
                    new DBFileEditorControl { Dock = DockStyle.Fill },
                    new DBFileEditorTree { Dock = DockStyle.Fill },
                    textFileEditorControl
                                              };
        }

        public PackFileManagerForm (string[] args) {
            InitializeComponent();
            editors = CreateEditors();
            try {
                if (!DBTypeMap.Instance.Initialized) {
                    DBTypeMap.Instance.InitializeTypeMap(Path.GetDirectoryName(Application.ExecutablePath));
                }
            } catch (Exception e) {
                if (MessageBox.Show(string.Format("Could not initialize type map: {0}.\nTry autoupdate?", e.Message),
                    "Initialize failed", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes) {
                    tryUpdate();
                }
            }

            updateOnStartupToolStripMenuItem.Checked = Settings.Default.UpdateOnStartup;
            showDecodeToolOnErrorToolStripMenuItem.Checked = Settings.Default.ShowDecodeToolOnError;

            try {
                if (Settings.Default.UpdateOnStartup) {
                    tryUpdate (false);
                }
            } catch {
            }

            InitializeBrowseDialogs (args);

            Text = string.Format("Pack File Manager {0}", Application.ProductVersion);

            // open pack file from command line if applicable
            if (args.Length == 1) {
                if (!File.Exists(args[0])) {
                    throw new ArgumentException("path is not a file or path does not exist");
                }
                OpenExistingPackFile(args[0]);
            }

            // fill CA file list
            FillCaPackMenu();
            GameManager.Instance.GameChanged += FillCaPackMenu;
            GameManager.Instance.GameChanged += EnableInstallUninstall;
            // reload when game has changed (rebuild tree etc)
            GameManager.Instance.GameChanged += OpenCurrentModPack;

            // fill game list
            foreach (Game g in Game.GetGames()) {
                ToolStripMenuItem item = new ToolStripMenuItem(g.Id);
                item.Enabled = g.IsInstalled;
                item.Checked = GameManager.Instance.CurrentGame == g;
                item.Click += new EventHandler(delegate(object o, EventArgs unused) { 
                    GameManager.Instance.CurrentGame = Game.ById(item.Text); 
                });
                GameManager.Instance.GameChanged += delegate() {
                    item.Checked = GameManager.Instance.CurrentGame.Id.Equals(item.Text);
                };
                gameToolStripMenuItem.DropDownItems.Add(item);
            }

            EnableInstallUninstall();
            ModManager.Instance.CurrentModChanged += EnableInstallUninstall;
            ModManager.Instance.CurrentModChanged += OpenCurrentModPack;

            // initialize MyMods menu
            modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem("None", ""));
            ModManager.Instance.ModNames.ForEach(name => 
                                                 modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(name, name)));
            if (args.Length == 0) {
                OpenCurrentModPack();
            }

            csvToolStripMenuItem.Checked = "csv".Equals(Settings.Default.TsvExtension);
            tsvToolStripMenuItem.Checked = "tsv".Equals(Settings.Default.TsvExtension);
        }

        private void EnableInstallUninstall() {
            bool enabled = !string.IsNullOrEmpty(Settings.Default.CurrentMod);
            enabled &= GameManager.Instance.CurrentGame.IsInstalled;
            installModMenuItem.Enabled = uninstallModMenuItem.Enabled = enabled;
        }
        
        private void FillCaPackMenu() {
            string shogunPath = GameManager.Instance.CurrentGame.GameDirectory;
            openCAToolStripMenuItem.DropDownItems.Clear();
            openCAToolStripMenuItem.Enabled = shogunPath != null;
            if (shogunPath != null) {
                shogunPath = Path.Combine(shogunPath, "data");
                if (Directory.Exists(shogunPath)) {
                    List<string> packFiles = new List<string> (Directory.GetFiles(shogunPath, "*.pack"));
                    packFiles.Sort(NumberedFileComparator.Instance);
                    packFiles.ForEach(file => openCAToolStripMenuItem.DropDownItems.Add(
                        new ToolStripMenuItem(Path.GetFileName(file), null, 
                                         delegate(object s, EventArgs a) { OpenExistingPackFile(file); })));
                }
            }
        }

        #region Form Management
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            base.Close();
        }

        private void PackFileManagerForm_Activated(object sender, EventArgs e) {
            if ((base.OwnedForms.Length > 0) && (search != null)) {
                packTreeView.SelectedNode = search.nextNode;
            }
        }

        private void PackFileManagerForm_FormClosing(object sender, FormClosingEventArgs e) {
            switch(e.CloseReason) {
            case CloseReason.WindowsShutDown:
            case CloseReason.TaskManagerClosing:
            case CloseReason.ApplicationExitCall:
                break;
            default:
                e.Cancel = handlePackFileChangesWithUserInput() == DialogResult.Cancel;
                break;
            }
        }

        private void PackFileManagerForm_GotFocus(object sender, EventArgs e) {
            base.Activated -= new EventHandler (PackFileManagerForm_GotFocus);
            if (openFileIsModified) {
                openFileIsModified = false;
                if (MessageBox.Show ("Changes were made to the extracted file. "+
                                     "Do you want to replace the packed file with the extracted file?", "Save changes?", 
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    openPackedFile.Data = (File.ReadAllBytes (openFilePath));
                }
            }
            while (File.Exists(openFilePath)) {
                try {
                    File.Delete (openFilePath);
                } catch (IOException) {
                    if (MessageBox.Show ("Unable to delete the temporary file; is it still in use by the external editor?" + 
                                         "\r\n\r\nClick Retry to try deleting it again or Cancel to leave it in the temporary directory.", 
                                         "Temporary file in use", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel) {
                        break;
                    }
                }
            }
        }

        /*
         * Determine a reasonable initial directory for the open/save/extract
         * browse dialogs.
         */
        void InitializeBrowseDialogs(string[] args) {

            // do we have a last directory? use as default
            // default: current directory
            string initialDialog = Directory.GetCurrentDirectory();
            try {

                // use the last load/save location if we have one
                if (!string.IsNullOrEmpty(Settings.Default.LastPackDirectory)) {
                    initialDialog = Settings.Default.LastPackDirectory;
                } else {
                    // otherwise, try to determine the shogun install path and use the data directory
                    initialDialog = GameManager.Instance.CurrentGame.GameDirectory;
                    if (!string.IsNullOrEmpty(initialDialog)) {
                        initialDialog = Path.Combine(initialDialog, "data");
                    } else {

                        // go through the arguments (interpreted as file names)
                        // and use the first for which the directory exists
                        foreach (string file in args) {
                            string dir = Path.GetDirectoryName(file);
                            if (File.Exists(dir)) {
                                initialDialog = dir;
                                break;
                            }
                        }
                    }
                }
            } catch {
                // we have not set an invalid path along the way; should still be current dir here
            }
            // set to the dialogs
            Settings.Default.LastPackDirectory = initialDialog;
        }

        private void PackFileManagerForm_Load(object sender, EventArgs e) {
            base.TopMost = true;
        }

        private void PackFileManagerForm_Shown(object sender, EventArgs e) {
            base.TopMost = false;
        }
        #endregion

        private void exportFileListToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fileListDialog = new SaveFileDialog {
                InitialDirectory = Settings.Default.ImportExportDirectory,
                FileName = Path.GetFileNameWithoutExtension(currentPackFile.Filepath) + ".pack-file-list.txt"
            };
            if (fileListDialog.ShowDialog() == DialogResult.OK) {
                Settings.Default.ImportExportDirectory = Path.GetDirectoryName(fileListDialog.FileName);
                using (StreamWriter writer = new StreamWriter(fileListDialog.FileName)) {
                    foreach (PackedFile file in currentPackFile.Files) {
                        writer.WriteLine(file.FullPath);
                    }
                }
            }
        }

        protected void EnableMenuItems() {
            createReadMeToolStripMenuItem.Enabled = !CanWriteCurrentPack;
            changePackTypeToolStripMenuItem.Enabled = currentPackFile != null;
            exportFileListToolStripMenuItem.Enabled = currentPackFile != null;
        }

        #region Packed from Tree
        private List<TreeNode> getTreeViewBranch(TreeNodeCollection trunk)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            getTreeViewBranch(nodes, trunk);
            return nodes;
        }

        private void getTreeViewBranch(List<TreeNode> nodes, TreeNodeCollection trunk)
        {
            foreach (TreeNode node in trunk)
            {
                nodes.Add(node);
                getTreeViewBranch(nodes, node.Nodes);
            }
        }
        #endregion

        private DialogResult handlePackFileChangesWithUserInput()
        {
            if ((currentPackFile != null) && currentPackFile.IsModified)
            {
                switch (MessageBox.Show(@"You modified the pack file. Do you want to save your changes?", @"Save Changes?", 
                                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        if (!currentPackFile.IsModified)
                        {
                            break;
                        }
                        return DialogResult.Cancel;

                    case DialogResult.No:
                        return DialogResult.No;

                    case DialogResult.Cancel:
                        return DialogResult.Cancel;
                }
            }
            return DialogResult.No;
        }
  
        #region File Menu
        #region Open Pack
        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            if (handlePackFileChangesWithUserInput() == System.Windows.Forms.DialogResult.No) {
                NewMod("Untitled.pack");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog packOpenFileDialog = new OpenFileDialog {
                InitialDirectory = Settings.Default.LastPackDirectory,
                Filter = IOFunctions.PACKAGE_FILTER
            };
            if ((handlePackFileChangesWithUserInput() != DialogResult.Cancel) && 
                (packOpenFileDialog.ShowDialog() == DialogResult.OK)) {
                OpenExistingPackFile(packOpenFileDialog.FileName);
            }
        }

        private void OpenExistingPackFile(string filepath)
        {
            Settings.Default.LastPackDirectory = Path.GetDirectoryName(filepath);
            try
            {
                var codec = new PackFileCodec();
                new LoadUpdater(codec, filepath, packStatusLabel, packActionProgressBar);
                CurrentPackFile = codec.Open(filepath);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                packTreeView.Enabled = true;
            }
        }
        #endregion
        #region Save Pack
        private bool CanWriteCurrentPack {
            get {
                bool result = true;
                if (cAPacksAreReadOnlyToolStripMenuItem.Checked) {
                    switch (currentPackFile.Type) {
                        case PackType.Mod:
                            // mod files can always be saved
                            result = true;
                            break;
                        case PackType.Movie:
                            // exclude files named patch_moviesX.pack
                            var caMovieRe = new Regex("(patch_)?movies([0-9]*).pack");
                            result = !caMovieRe.IsMatch(Path.GetFileName(currentPackFile.Filepath));
                            break;
                        default:
                            result = false;
                            break;
                    }
                }
                return result;
            }
        }

        static string CA_FILE_WARNING = "Will only save MOD and non-CA MOVIE files with current Setting.";
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CanWriteCurrentPack) {
                MessageBox.Show(CA_FILE_WARNING);
            } else {
                var dialog = new SaveFileDialog {
                    InitialDirectory = Settings.Default.LastPackDirectory,
                    AddExtension = true,
                    Filter = IOFunctions.PACKAGE_FILTER
                };
                if (dialog.ShowDialog() == DialogResult.OK) {
                    Settings.Default.LastPackDirectory = Path.GetDirectoryName(dialog.FileName);
                    SaveAsFile(dialog.FileName);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!CanWriteCurrentPack) {
                MessageBox.Show(CA_FILE_WARNING);
            } else
                if (currentPackFile.Filepath.EndsWith("Untitled.pack")) {
                    // ask for a name first
                    saveAsToolStripMenuItem_Click(null, null);
                } else {
                    SaveAsFile(currentPackFile.Filepath);
                }
        }

        void SaveAsFile(string filename) {
            if (!CanWriteCurrentPack) {
                MessageBox.Show(CA_FILE_WARNING);
            } else {
                CloseEditors ();
                string tempFile = Path.GetTempFileName ();
                new PackFileCodec ().writeToFile (tempFile, currentPackFile);
                if (File.Exists (filename)) {
                    File.Delete (filename);
                }
                File.Move (tempFile, filename);
                OpenExistingPackFile (filename);
            }
        }
        #endregion
        
        private void packTypeToolStripMenuItem_Click(object sender, EventArgs e) {
            bootToolStripMenuItem.Checked = object.ReferenceEquals (sender, bootToolStripMenuItem);
            bootXToolStripMenuItem.Checked = object.ReferenceEquals (sender, bootXToolStripMenuItem);
            releaseToolStripMenuItem.Checked = object.ReferenceEquals (sender, releaseToolStripMenuItem);
            patchToolStripMenuItem.Checked = object.ReferenceEquals (sender, patchToolStripMenuItem);
            movieToolStripMenuItem.Checked = object.ReferenceEquals (sender, movieToolStripMenuItem);
            shaderToolStripMenuItem.Checked = object.ReferenceEquals (sender, shaderToolStripMenuItem);
            shader2ToolStripMenuItem.Checked = object.ReferenceEquals(sender, shader2ToolStripMenuItem);
            modToolStripMenuItem.Checked = object.ReferenceEquals(sender, modToolStripMenuItem);
            if (bootToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Boot;
            } else if (bootXToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.BootX;
            } else if (releaseToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Release;
            } else if (patchToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Patch;
            } else if (movieToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Movie;
            } else if (modToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Mod;
            } else if (shaderToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Shader1;
            } else if (shader2ToolStripMenuItem.Checked) {
                currentPackFile.Type = PackType.Shader2;
            }
        }

        #endregion

        #region MyMod Menu
        private void newModMenuItem_Click(object sender, EventArgs e) {
            List<string> oldMods = ModManager.Instance.ModNames;
            string packFileName = ModManager.Instance.AddMod();
            if (packFileName != null) {
                // add mod entry to menu
                if (Settings.Default.CurrentMod != "") {
                    if (!oldMods.Contains(Settings.Default.CurrentMod)) {
                        modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(Settings.Default.CurrentMod, 
                                                                                Settings.Default.CurrentMod));
                    }
                }
                if (File.Exists(packFileName)) {
                    OpenExistingPackFile(packFileName);
                } else {
                    NewMod(Path.Combine(ModManager.Instance.CurrentModDirectory, packFileName));
                    OpenExistingPackFile(Path.Combine(ModManager.Instance.CurrentModDirectory, packFileName));
                }
            }
        }

        private void NewMod(string name) {
            var header = new PFHeader("PFH3") {
                Type = PackType.Mod,
                Version = 0,
                FileCount = 0,
                ReplacedPackFileNames = new List<string>(),
                DataStart = 0x20
            };
            CurrentPackFile = new PackFile(name, header);
        }
        
        private void installModMenuItem_Click(object sender, EventArgs e) {
            if (CurrentPackFile != null && CurrentPackFile.IsModified) {
                var result = MessageBox.Show("The current pack has been modified. Save first?\n"+
                                "Otherwise, the last saved version will be installed.", "Save?", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Yes) {
                    string modPath =  (ModManager.Instance.CurrentMod != null) 
                        ? ModManager.Instance.CurrentMod.FullModPath : CurrentPackFile.Filepath;
                    SaveAsFile(modPath);
                } else if (result == DialogResult.Cancel) {
                    return;
                }
            }
            try {
                ModManager.Instance.InstallCurrentMod();
            } catch (Exception ex) {
                MessageBox.Show(string.Format("Install failed: {0}", ex), "Install Failed", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void uninstallModMenuItem_Click(object sender, EventArgs e) {
            try {
                ModManager.Instance.UninstallCurrentMod();
            } catch (Exception ex) {
                MessageBox.Show(string.Format("Uninstall failed: {0}", ex), "Uninstall Failed", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteCurrentToolStripMenuItem_Click(object sender, EventArgs e) {
            string current = Settings.Default.CurrentMod;
            if (current != "") {
                ModManager.Instance.DeleteCurrentMod();
                foreach (ToolStripItem item in modsToolStripMenuItem.DropDownItems) {
                    if (item.Text == current) {
                        modsToolStripMenuItem.DropDownItems.Remove(item);
                        break;
                    }
                }
            }
        }
        
        private void OpenCurrentModPack() {
            try {
                if (ModManager.Instance.CurrentMod != null) {
                    string modPath = ModManager.Instance.CurrentMod.FullModPath;
                    if (File.Exists(modPath)) {
                        OpenExistingPackFile(modPath);
                    }
                }
            } catch { }
        }
        #endregion

        #region Files Menu
        #region Entry Add/Delete
        VirtualDirectory AddTo {
            get {
                VirtualDirectory addTo;
                if (packTreeView.SelectedNode == null) {
                    addTo = CurrentPackFile.Root;
                } else {
                    addTo = packTreeView.SelectedNode.Tag as VirtualDirectory 
                        ?? packTreeView.SelectedNode.Parent.Tag as VirtualDirectory;
                }
                return addTo;
            }
        }

        // removes the part of the given path up to the current mod directory
        string GetPathRelativeToMod(string file) {
            string addBase = "" + Path.DirectorySeparatorChar;
            string modDir = ModManager.Instance.CurrentModDirectory;
            if (!string.IsNullOrEmpty(modDir) && file.StartsWith(modDir)) {
                Uri baseUri = new Uri(ModManager.Instance.CurrentModDirectory);
                Uri createPath = baseUri.MakeRelativeUri(new Uri(file));
                addBase = createPath.ToString().Replace('/', Path.DirectorySeparatorChar);
                addBase = addBase.Remove(0, addBase.IndexOf(Path.DirectorySeparatorChar) + 1);
                addBase = Uri.UnescapeDataString(addBase);
            }
            return addBase;
        }

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e) {
            VirtualDirectory addToBase = (Settings.Default.CurrentMod != "")
                ? currentPackFile.Root : AddTo;
            if (addToBase == null) {
                return;
            }
            var addReplaceOpenFileDialog = new OpenFileDialog {
                InitialDirectory = Settings.Default.ImportExportDirectory,
                Multiselect = true
            };
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK) {
                Settings.Default.ImportExportDirectory = Path.GetDirectoryName(addReplaceOpenFileDialog.FileName);
                try {
                    foreach (string file in addReplaceOpenFileDialog.FileNames) {
                        string addBase = (Settings.Default.CurrentMod != "") 
                            ? GetPathRelativeToMod(file) : Path.GetFileName(file);
                        addToBase.Add(addBase, new PackedFile(file));
                    }
                } catch (Exception x) {
                    MessageBox.Show(x.Message, "Problem, Sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (packTreeView.SelectedNode == null) {
                return;
            }
            var packedFiles = new List<PackedFile>();
            if ((packTreeView.SelectedNode == packTreeView.Nodes[0]) || (packTreeView.SelectedNode.Nodes.Count > 0))
            {
                getPackedFilesFromBranch(packedFiles, packTreeView.SelectedNode.Nodes);
            }
            else
            {
                packedFiles.Add(packTreeView.SelectedNode.Tag as PackedFile);
            }
            foreach (PackedFile file in packedFiles)
            {
                file.Deleted = true;
            }
        }
        private void getPackedFilesFromBranch(List<PackedFile> packedFiles, TreeNodeCollection trunk, Predicate<PackedFile> filter = null) {
            foreach (TreeNode node in trunk)
            {
                if (node.Nodes.Count > 0) {
                    getPackedFilesFromBranch(packedFiles, node.Nodes, filter);
                }
                else if (filter == null || filter(node.Tag as PackedFile)) {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
        }
        
        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e) {
            FolderBrowserDialog addDirectoryFolderBrowserDialog = new FolderBrowserDialog() {
                Description = "Add which directory?",
                SelectedPath = Settings.Default.ImportExportDirectory
            };
            if (AddTo != null && addDirectoryFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    string basePath = addDirectoryFolderBrowserDialog.SelectedPath;
                    VirtualDirectory addToBase = AddTo;
                    if (Settings.Default.CurrentMod != "" && basePath.StartsWith(ModManager.Instance.CurrentModDirectory)) {
                        string relativePath = GetPathRelativeToMod(basePath);
                        addToBase = CurrentPackFile.Root;
                        foreach (string pathElement in relativePath.Split(Path.DirectorySeparatorChar)) {
                            addToBase = addToBase.getSubdirectory(pathElement);
                        }
                        addToBase = addToBase.Parent as VirtualDirectory;
                    }
                    addToBase.Add(addDirectoryFolderBrowserDialog.SelectedPath);
                } catch (Exception x) {
                    MessageBox.Show(string.Format("Failed to add {0}: {1}", 
                                                  addDirectoryFolderBrowserDialog.SelectedPath, x.Message), 
                                    "Failed to add directory");
                }
            }
        }

        private void createReadMeToolStripMenuItem_Click(object sender, EventArgs e) {
            var readme = new PackedFile { Name = "readme.xml", Data = new byte[0] };
            currentPackFile.Add("readme.xml", readme);
            openReadMe(readme);
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
            packTreeView.SelectedNode.BeginEdit();
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog replaceFileDialog = new OpenFileDialog() {
                InitialDirectory = Settings.Default.ImportExportDirectory,
                Multiselect = false
            };
            if (replaceFileDialog.ShowDialog() == DialogResult.OK) {
                Settings.Default.ImportExportDirectory = Path.GetDirectoryName(replaceFileDialog.FileName);
                PackedFile tag = packTreeView.SelectedNode.Tag as PackedFile;
                tag.Source = new FileSystemSource(replaceFileDialog.FileName);
            }
        }
        
        private void emptyDirectoryToolStripMenuItem_Click(object sender, EventArgs e) {
            VirtualDirectory dir = 
                (packTreeView.SelectedNode != null) ? packTreeView.SelectedNode.Tag as VirtualDirectory : CurrentPackFile.Root;
            if (dir != null) {
                try {
                    VirtualDirectory newDir = new VirtualDirectory() { Name = "empty" };
                    dir.Add(newDir);
                    foreach (TreeNode node in packTreeView.SelectedNode.Nodes) {
                        if (node.Tag == newDir) {
                            node.EnsureVisible();
                            node.BeginEdit();
                            break;
                        }
                    }
                } catch { }
            }
        }

        private void dBFileFromTSVToolStripMenuItem_Click(object sender, EventArgs e) {
            VirtualDirectory addToBase = (Settings.Default.CurrentMod != "")
                ? currentPackFile.Root : AddTo;
            if (addToBase != null) {
                OpenFileDialog openDBFileDialog = new OpenFileDialog {
                    InitialDirectory = Settings.Default.ImportExportDirectory,
                    Filter = IOFunctions.TSV_FILTER
                };
                if (openDBFileDialog.ShowDialog() == DialogResult.OK) {
                    Settings.Default.ImportExportDirectory = Path.GetDirectoryName(openDBFileDialog.FileName);
                    try {
                        using (FileStream filestream = File.OpenRead(openDBFileDialog.FileName)) {
                            string filename = Path.GetFileNameWithoutExtension(openDBFileDialog.FileName);
                            byte[] data;
                            if (openDBFileDialog.FileName.Contains(".loc.")) {
                                LocFile file = new LocFile();
                                using (StreamReader reader = new StreamReader(filestream)) {
                                    file.Import(reader);
                                    using (MemoryStream stream = new MemoryStream()) {
                                        new LocCodec().Encode(stream, file);
                                        data = stream.ToArray();
                                    }
                                }
                            } else {
                                DBFile file = new TextDbCodec().Decode(filestream);
                                data = PackedFileDbCodec.FromFilename(openDBFileDialog.FileName).Encode(file);
                            }
                            string addBase = (Settings.Default.CurrentMod != "")
                                ? GetPathRelativeToMod(openDBFileDialog.FileName) : Path.GetFileName(openDBFileDialog.FileName);

                            addToBase.Add(addBase, new PackedFile { Data = data, Name = filename });
                        }
                    } catch (Exception x) {
                        MessageBox.Show(x.Message);
                    }
                }
            } else {
                MessageBox.Show("Select a directory to add to");
            }
        }
        #endregion

        #region Open Packed
        private void openAsTextMenuItem_Click(object sender, EventArgs e) {
            textFileEditorControl.CurrentPackedFile = packTreeView.SelectedNode.Tag as PackedFile;
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e) {
            openExternal(packTreeView.SelectedNode.Tag as PackedFile, "openas");
        }

        private void openDecodeToolMenuItem_Click(object sender, EventArgs e) {
            PackedFile packedFile = packTreeView.SelectedNode.Tag as PackedFile;
            if (packedFile != null) {
                DecodeTool.DecodeTool decoder = null;
                // best used if a db file...
                try {
                    string key = DBFile.typename(packedFile.FullPath);
                    decoder = new DecodeTool.DecodeTool { TypeName = key, Bytes = packedFile.Data };
                    decoder.ShowDialog();
                } catch (Exception ex) {
                    MessageBox.Show (string.Format("DecodeTool could not be opened:\n{0}", ex.Message), 
                                     "DecodeTool problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                } finally {
                    if (decoder != null) {
                        decoder.Dispose();
                    }
                }
            }
        }

        public void openExternal(PackedFile packedFile, string verb)
        {
            if (packedFile == null)
            {
                return;
            }
            openPackedFile = packedFile;
            openFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(packedFile.FullPath));
            File.WriteAllBytes(openFilePath, packedFile.Data);
            OpenWith(openFilePath, verb);
        }
        
        private void OpenPackedFile(object tag) {
            PackedFile packedFile = tag as PackedFile;
            if (packedFile == null) {
                return;
            }
            IPackedFileEditor editor = null;
            foreach(IPackedFileEditor e in Editors) {
                if (e.CanEdit(packedFile)) {
                    editor = e;
                    break;
                }
            }
            if (editor != null) {
                try {
                    editor.CurrentPackedFile = packedFile;
                    if (!splitContainer1.Panel2.Controls.Contains(editor as UserControl)) {
                        splitContainer1.Panel2.Controls.Add(editor as UserControl);
                    }
                } catch (Exception ex) {
                    MessageBox.Show(string.Format("Failed to open {0}: {1}", Path.GetFileName(packedFile.FullPath), ex));
                }
                return;
            }

            if (packedFile.FullPath == "readme.xml") {
                openReadMe(packedFile);
            } else if (packedFile.FullPath.Contains(".rigid")) {
                // viewModel(packedFile);
                }
            }

        private void CloseEditors() {
            foreach(IPackedFileEditor editor in Editors) {
                editor.Commit();
            }

            if (readmeEditorControl != null) {
                readmeEditorControl.updatePackedFile();
            }

            splitContainer1.Panel2.Controls.Clear();
        }
        
        private void OpenWith(string file, string verb) {
            ProcessStartInfo startInfo = new ProcessStartInfo(file) {
                ErrorDialog = true
            };
            if (startInfo.Verbs.Length == 0) {
                startInfo.Verb = "openas";
            } else {
                startInfo.Verb = verb;
            }
            try {
                Process.Start(startInfo);
                openFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file));
                openFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                openFileWatcher.Changed += new FileSystemEventHandler(openFileWatcher_Changed);
                openFileWatcher.EnableRaisingEvents = true;
                openFileIsModified = false;
                base.Activated += new EventHandler(PackFileManagerForm_GotFocus);
            } catch (Exception x) {
                MessageBox.Show("Problem opening " + file + ": " + x.Message, "Could not open file");
            }
        }
        
        private void openFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (openFileWatcher != null)
            {
                openFileWatcher.EnableRaisingEvents = false;
                openFileWatcher = null;
                openFileIsModified = true;
            }
        }
        
        private void openReadMe(PackedFile packedFile)
        {
            try
            {
                if (packedFile.Size != 0)
                {
                    readmeEditorControl = new ReadmeEditorControl();
                    readmeEditorControl.Dock = DockStyle.Fill;
                    readmeEditorControl.setPackedFile(packedFile);
                    splitContainer1.Panel2.Controls.Add(readmeEditorControl);
                }
            }
            catch (ConstraintException)
            {
            }
        }
        #endregion

        #region Extract
        private void extractAllTsv_Click(object sender, EventArgs e) {
            List<PackedFile> files = new List<PackedFile>();
            IExtractionPreprocessor tsvExport = new TsvExtractionPreprocessor();
            currentPackFile.Files.ForEach(f => { if (tsvExport.CanExtract(f)) { files.Add(f); }});
            FileExtractor extractor = new FileExtractor(packStatusLabel, packActionProgressBar) {
                Preprocessor = tsvExport
            };
            extractor.extractFiles(files);
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<PackedFile> packedFiles = new List<PackedFile>();
            foreach (TreeNode node in packTreeView.Nodes)
            {
                if (node.Nodes.Count > 0)
                {
                    getPackedFilesFromBranch(packedFiles, node.Nodes);
                }
                else
                {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
            new FileExtractor(packStatusLabel, packActionProgressBar).extractFiles(packedFiles);
        }

        private void extractSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<PackedFile> packedFiles = new List<PackedFile>();
            if (packTreeView.SelectedNode.Nodes.Count > 0)
            {
                getPackedFilesFromBranch(packedFiles, packTreeView.SelectedNode.Nodes);
            }
            else
            {
                packedFiles.Add(packTreeView.SelectedNode.Tag as PackedFile);
            }
            new FileExtractor(packStatusLabel, packActionProgressBar).extractFiles(packedFiles);
        }

        private void exportUnknownToolStripMenuItem_Click(object sender, EventArgs e) {
            var packedFiles = new List<PackedFile>();
            CurrentPackFile.Files.ForEach(f => { if (unknownDbFormat(f)) { packedFiles.Add (f); }});
            new FileExtractor(packStatusLabel, packActionProgressBar).extractFiles(packedFiles);
        }

        private bool unknownDbFormat(PackedFile file) {
            bool result = file.FullPath.StartsWith ("db");
            string buffer;
            result &= !PackedFileDbCodec.CanDecode (file, out buffer);
            return result;
        }
        #endregion
        #endregion
        
        #region DB Descriptions Menu
        private void fromXsdFileToolStripMenuItem_Click(object sender, EventArgs e) {
            var open = new OpenFileDialog {InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath)};
            if (open.ShowDialog() == DialogResult.OK) {
                DBTypeMap.Instance.loadFromXsd(open.FileName);
            }
        }

        private void saveToDirectoryToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                DBTypeMap.Instance.saveToFile(Path.GetDirectoryName(Application.ExecutablePath), 
                                              GameManager.Instance.CurrentGame.Id);
                string message = "You just saved your own DB definitions in a new file.\n" +
                    "This means that these will be used instead of the ones received in updates from TWC.\n" +
                    "Once you have uploaded your changes and they have been integrated,\n" +
                    "please delete the file schema_user.xml.";
                MessageBox.Show(message, "New User DB Definitions created", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

            } catch (Exception x) {
                MessageBox.Show(string.Format("Could not save user db descriptions: {0}\n" + 
                                              "User file won't be used anymore. A backup has been made.", x.Message));
            }
        }

        private void updateAllToolStripMenuItem_Click(object sender, EventArgs e) {
            if (currentPackFile != null) {
                foreach (PackedFile packedFile in currentPackFile.Files) {
                    UpdatePackedFile(packedFile);
                }
            }
        }

        private void updateCurrentToolStripMenuItem_Click(object sender, EventArgs e) {
            if (dbFileEditorControl.CurrentPackedFile != null) {
                UpdatePackedFile(dbFileEditorControl.CurrentPackedFile);
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK) {
                DBTypeMap.Instance.initializeFromFile(dlg.FileName);
            }
        }
  
        private void updateToolStripMenuItem_Click(object sender, EventArgs ev) {
            tryUpdate(true, currentPackFile == null ? null : currentPackFile.Filepath);
        }

        public static void tryUpdate(bool showSuccess = true, string currentPackFile = null) {
            try {
                string path = Path.GetDirectoryName(Application.ExecutablePath);
                string version = Application.ProductVersion;
                bool update = DBFileTypesUpdater.checkVersion(path, ref version);
                if (showSuccess) {
                    string message = update ? "DB File description updated." : "No update performed.";
                    MessageBox.Show(message, "Update result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (update) {
                    GameManager.Instance.ApplyGameTypemap();
                }
                if (version != Application.ProductVersion) {
                    if (MessageBox.Show(string.Format("A new version of PFM is available ({0})\nAutoinstall?", version),
                                         "New Software version available",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes) {
                        Process myProcess = Process.GetCurrentProcess();
                        if (myProcess.CloseMainWindow()) {
                            // re-open file if one is open already
                            string currentPackPath = currentPackFile == null ? "" : string.Format(" {0}", currentPackFile);
                            string arguments = string.Format("{0} {1} PackFileManager.exe{2}", myProcess.Id, version, currentPackPath);
                            Process.Start("AutoUpdater.exe", arguments);
                            myProcess.Close();
                        }
                    }
                }
            } catch (Exception e) {
                MessageBox.Show(
                    string.Format("Update failed: \n{0}\n{1}", e.Message, e.StackTrace),
                    "Problem, sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // this could do with an update; since the transition to schema.xml,
        // we also know obsolete fields and can remove them,
        // and we can add fields in the middle instead of assuming they got appended.
        private void UpdatePackedFile(PackedFile packedFile) {
            try {
                string key = DBFile.typename(packedFile.FullPath);
                if (DBTypeMap.Instance.IsSupported(key)) {
                    PackedFileDbCodec codec = PackedFileDbCodec.FromFilename(packedFile.FullPath);
                    int maxVersion = DBTypeMap.Instance.MaxVersion(key);
                    DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
                    if (header.Version < maxVersion) {
                        // found a more recent db definition; read data from db file
                        DBFile updatedFile = PackedFileDbCodec.Decode(packedFile);

                        TypeInfo dbFileInfo = updatedFile.CurrentType;
                        string guid;
                        TypeInfo targetInfo = GetTargetTypeInfo (key, maxVersion, out guid);
                        if (targetInfo == null) {
                            MessageBox.Show("Will not update this table: can't decide new structure.");
                            return;
                        }

                        // identify FieldInstances missing in db file
                        for (int i = dbFileInfo.Fields.Count; i < targetInfo.Fields.Count; i++) {
                            foreach (List<FieldInstance> entry in updatedFile.Entries) {
                                var field = targetInfo.Fields[i].CreateInstance();
                                entry.Add(field);
                            }
                        }
                        updatedFile.Header.GUID = guid;
                        updatedFile.Header.Version = maxVersion;
                        packedFile.Data = codec.Encode(updatedFile);

                        if (dbFileEditorControl.CurrentPackedFile == packedFile) {
                            dbFileEditorControl.Open();
                        }
                    }
                }
            } catch (Exception x) {
                MessageBox.Show(string.Format("Could not update {0}: {1}", Path.GetFileName(packedFile.FullPath), x.Message));
            }
        }

        TypeInfo GetTargetTypeInfo(string key, int maxVersion, out string guid) {
            TypeInfo targetInfo = null;
            List<string> newGuid = GetGuidsForInfo(key, maxVersion);
            guid = null;
            if (newGuid.Count == 0) {
                guid = "";
            } else if (newGuid.Count == 1) {
                guid = newGuid[0];
            } if (newGuid.Count > 1) {
                for (int index = 0; newGuid.Count > index && guid == null; index++) {
                    string message = string.Format("There are more than one definitions for the maximum version {0}.\nUse GUID {1}?",
                                                   maxVersion, newGuid[index]);
                    if (MessageBox.Show(message, "Choose GUID", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                        guid = newGuid[index];
                    }
                }
            }
            
            if (guid != null) {
                targetInfo = DBTypeMap.Instance.GetVersionedInfo(key, maxVersion);
            }
            return targetInfo;
        }
        private List<string> GetGuidsForInfo(string type, int version) {
            List<string> result = new List<string>();
            foreach(GuidTypeInfo info in DBTypeMap.Instance.GuidMap.Keys) {
                if (info.Version == version && info.TypeName.Equals(type)) {
                    result.Add(info.Guid);
                }
            }
            return result;
        }
  

        #endregion

        #region Options Menu
        private void cAPacksAreReadOnlyToolStripMenuItem_CheckStateChanged(object sender, EventArgs e) {
            if (cAPacksAreReadOnlyToolStripMenuItem.CheckState == CheckState.Unchecked) {
                var advisory = new CaFileEditAdvisory();
                cAPacksAreReadOnlyToolStripMenuItem.CheckState = 
                    (advisory.DialogResult == DialogResult.Yes) ? CheckState.Unchecked : CheckState.Checked;
            }
        }

        private void updateOnStartupToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.UpdateOnStartup = updateOnStartupToolStripMenuItem.Checked;
        }

        private void showDecodeToolOnErrorToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.ShowDecodeToolOnError = showDecodeToolOnErrorToolStripMenuItem.Checked;
        }

        private void extensionSelectionChanged(object sender, EventArgs e) {
            Settings.Default.TsvExtension = (sender as ToolStripMenuItem).Text;
            csvToolStripMenuItem.Checked = "csv".Equals(Settings.Default.TsvExtension);
            tsvToolStripMenuItem.Checked = "tsv".Equals(Settings.Default.TsvExtension);
        }
        #endregion
  
        #region Help Menu
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            var form = new Form {
                Text = string.Format("About Pack File Manager {0}", Application.ProductVersion),
                Size = new Size (0x177, 0xe1),
                WindowState = FormWindowState.Normal,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };
            var label = new Label {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.Format (
                    "\r\nPack File Manager {0} by daniu\r\n" +
                    "\r\nBased on original work by Matt Chambers\r\n" +
                    "\r\nPack File Manager Update for NTW by erasmus777\r\n" +
                    "\r\nPack File Manager Update for TWS2 by Lord Maximus and Porphyr\r\n" +
                    "Copyright 2009-2012 Distributed under the Simple Public License 2.0\r\n" +
                    "\r\nThanks to the hard work of the people at twcenter.net.\r\n" +
                    "\r\nSpecial thanks to alpaca, just, ancientxx, Delphy, Scanian, iznagi11, barvaz, " +
                    "Mechanic, mac89, badger1815, husserlTW, The Vicar, AveiMil, and many others!", Application.ProductVersion)
            };
            form.Controls.Add (label);
            form.ShowDialog (this);
        }
        #endregion

        #region Tree Handler
        static readonly Regex versionedRegex = new Regex("(.*) - version.*");
        private void packTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
            PackEntry entry = e.Node.Tag as PackEntry;
            if ((e.Label != null) && (e.Label != e.Node.Text) && (entry != null))             {
                string newName = e.Label;
                if (versionedRegex.IsMatch(newName)) {
                    newName = versionedRegex.Match(newName).Groups[1].Value;
                }
                entry.Name = newName;
            }
            e.CancelEdit = true;
        }

        private void packTreeView_AfterSelect(object sender, TreeViewEventArgs e) {
            if (e.Action != TreeViewAction.ByKeyboard && e.Action != TreeViewAction.ByMouse) {
#if DEBUG
                Console.WriteLine("Ignoring tree selection");
#endif
                return;
            }
#if DEBUG
            Console.WriteLine("handling tree selection");
#endif
            CloseEditors();
            splitContainer1.Panel2.Controls.Clear();
         
            if (packTreeView.SelectedNode != null)
            {
                packStatusLabel.Text = " Viewing: " + packTreeView.SelectedNode.Text;
                packTreeView.LabelEdit = packTreeView.Nodes.Count > 0 && packTreeView.SelectedNode != packTreeView.Nodes[0];
                if (packTreeView.SelectedNode.Tag is PackedFile)
                {
                    OpenPackedFile(packTreeView.SelectedNode.Tag);
                }
            }
            bool nodeSelected = packTreeView.SelectedNode != null;
            extractSelectedToolStripMenuItem.Enabled = nodeSelected;
            bool isChildNode = nodeSelected && (packTreeView.SelectedNode.Nodes.Count == 0);
            replaceFileToolStripMenuItem.Enabled = CanWriteCurrentPack && isChildNode;
            renameToolStripMenuItem.Enabled = (CanWriteCurrentPack && nodeSelected) 
                && (packTreeView.SelectedNode != packTreeView.Nodes[0]);
            contextDeleteMenuItem.Enabled = nodeSelected;
        }

        private void packTreeView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeNode nodeAt = packTreeView.GetNodeAt(e.Location);
            if ((nodeAt != null) && (e.Button == MouseButtons.Right))
            {
                packTreeView.SelectedNode = nodeAt;
            }
            if ((packTreeView.SelectedNode != null) && (packTreeView.SelectedNode.Tag != null))
            {
                openExternal(packTreeView.SelectedNode.Tag as PackedFile, "open");
            }
        }

        private void packTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode nodeAt = packTreeView.GetNodeAt(e.Location);
            if ((nodeAt != null) && (e.Button == MouseButtons.Right))
            {
                packTreeView.SelectedNode = nodeAt;
            }
        }

        private void packTreeView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Insert:
                    if (!e.Shift)
                    {
                        addFileToolStripMenuItem_Click(this, EventArgs.Empty);
                        break;
                    }
                    addDirectoryToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;

                case Keys.Delete:
                    if (packTreeView.SelectedNode != null)
                    {
                        deleteFileToolStripMenuItem_Click(this, EventArgs.Empty);
                    }
                    break;

                case Keys.X:
                    if (e.Control)
                    {
                        extractSelectedToolStripMenuItem_Click(this, EventArgs.Empty);
                    }
                    break;
            }
        }

        private void packTreeView_ItemDrag(object sender, ItemDragEventArgs e) {
            // Proceed with the drag-and-drop, passing the selected items for 
            if (e.Button == MouseButtons.Left && e.Item is TreeNode && e.Item != null &&
                ((TreeNode)e.Item).Tag is PackedFile && ((TreeNode)e.Item).Tag != null) {
                var file = ((TreeNode)e.Item).Tag as PackedFile;
                if (file != null) {
                    var dataObject = new DataObject();
                    var filesInfo = new DragFileInfo(file.FullPath, file.Size);

                    using (MemoryStream infoStream = DragDropHelper.GetFileDescriptor(filesInfo),
                                        contentStream = DragDropHelper.GetFileContents(file.Data)) {
                        dataObject.SetData(DragDropHelper.CFSTR_FILEDESCRIPTORW, infoStream);
                        dataObject.SetData(DragDropHelper.CFSTR_FILECONTENTS, contentStream);
                        dataObject.SetData(DragDropHelper.CFSTR_PERFORMEDDROPEFFECT, null);

                        DoDragDrop(dataObject, DragDropEffects.All);
                    }
                }
            }
        }
        #endregion

        private void packActionMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (currentPackFile == null)
            {
                e.Cancel = true;
            }
            else
            {
                bool currentPackFileIsReadOnly = CanWriteCurrentPack;
                addDirectoryToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                addFileToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                deleteFileToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                changePackTypeToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
            }
        }

        public override void Refresh() {
            var expandedNodes = new List<string>();
            foreach (TreeNode node in getTreeViewBranch(packTreeView.Nodes)) {
                if (node.IsExpanded && node is PackEntryNode) {
                    expandedNodes.Add((node.Tag as PackEntry).FullPath);
                }
            }
            string str = (packTreeView.SelectedNode != null) ? (packTreeView.SelectedNode.Tag as PackEntry).FullPath : "";
            packTreeView.Nodes.Clear();
            if (currentPackFile == null) {
                return;
            }
            TreeNode node2 = new DirEntryNode(CurrentPackFile.Root);
            packTreeView.Nodes.Add(node2);

            foreach (TreeNode node in getTreeViewBranch(packTreeView.Nodes)) {
                string path = (node.Tag as PackEntry).FullPath;
                if (expandedNodes.Contains(path)) {
                    node.Expand();
                }
				if (path == str) {
                    packTreeView.SelectedNode = node;
                    OpenPackedFile(node.Tag as PackedFile);
                }
            }
            filesMenu.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            createReadMeToolStripMenuItem.Enabled = true;
            bootToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Boot;
            bootXToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.BootX;
            releaseToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Release;
            patchToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Patch;
            movieToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Movie;
            modToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Mod;
            shaderToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Shader1;
            shader2ToolStripMenuItem.Checked = currentPackFile.Header.Type == PackType.Shader2;
            packTreeView_AfterSelect(this, new TreeViewEventArgs(packTreeView.SelectedNode));
            RefreshTitle();
            base.Refresh();
        }

        private void RefreshTitle()
        {
            Text = Path.GetFileName(currentPackFile.Filepath);
            if (currentPackFile.IsModified)
            {
                Text = Text + " (modified)";
            }
            Text = Text + string.Format(" - Pack File Manager {0}", Application.ProductVersion);
        }

        private void searchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            search = new CustomMessageBox();
            AddOwnedForm(search);
            search.lblMessage.Text = "Query:";
            search.Text = @"Search files\directories";
            findChild(packTreeView.Nodes[0]);
            search.Show();
        }

        private void findChild(TreeNode tnChild) {
            foreach (TreeNode node in tnChild.Nodes) {
                search.tnList.Add(node);
                findChild(node);
            }
        }

        private System.IO.FileSystemWatcher openFileWatcher;
    }
}

