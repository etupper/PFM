using Common;
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
                // unregister from previous
                if (currentPackFile != null) {
                    currentPackFile.Modified -= currentPackFile_Modified;
                }
                // register previous and build tree
                currentPackFile = value;
                currentPackFile.Modified += currentPackFile_Modified;
                changePackTypeToolStripMenuItem.Enabled = currentPackFile != null;
                exportFileListToolStripMenuItem.Enabled = currentPackFile != null;
                Refresh ();
            }
        }
        public ToolStripLabel StatusLabel {
            get
            {
                return packStatusLabel;
            }
        }
        public override sealed string Text {
            get { return base.Text; }
            set { base.Text = value; }
        }
        private bool openFileIsModified;
        private string openFilePath;
        private customMessageBox search;

        private PackedFile openPackedFile;
  
        #region Editors
        private AtlasFileEditorControl atlasFileEditorControl;
        private readonly DBFileEditorControl dbFileEditorControl;
        private ImageViewerControl imageViewerControl;
        private LocFileEditorControl locFileEditorControl;
        private EditEsfComponent esfEditor = new EditEsfComponent {
            Dock = DockStyle.Fill
        };
        GroupformationEditor gfEditor = new GroupformationEditor {
            Dock = DockStyle.Fill
        };
        private UnitVariantFileEditorControl unitVariantFileEditorControl;
        private TextFileEditorControl textFileEditorControl;
        private ToolStripMenuItem extractTSVFileExtensionToolStripMenuItem;
        private ToolStripMenuItem csvToolStripMenuItem;
        private ToolStripMenuItem tsvToolStripMenuItem;
        private Common.ReadmeEditorControl readmeEditorControl;
        #endregion

        delegate bool FileFilter (PackedFile file);

        public PackFileManagerForm (string[] args) {
            InitializeComponent();

            updateOnStartupToolStripMenuItem.Checked = Settings.Default.UpdateOnStartup;
            showDecodeToolOnErrorToolStripMenuItem.Checked = Settings.Default.ShowDecodeToolOnError;

            try {
                if (Settings.Default.UpdateOnStartup) {
                    tryUpdate (false);
                }
            } catch {
            }

            InitializeBrowseDialogs (args);

            // initialize db editor; need to do this before opening pack file
            // to make sure the type map is initialized
            var control = new DBFileEditorControl() {
                Dock = DockStyle.Fill
            };
            Text = string.Format("Pack File Manager {0}", Application.ProductVersion);

            // open pack file from command line if applicable
            if (args.Length == 1) {
                if (!File.Exists(args[0])) {
                    throw new ArgumentException("path is not a file or path does not exist");
                }
                OpenExistingPackFile(args[0]);
            }
            dbFileEditorControl = control;

            // initialize MyMods menu
            modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem("None", ""));
            ModManager.Instance.ModNames.ForEach(name => modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(name, name)));
            ModManager.Instance.CurrentModChanged += delegate(string newMod) { OpenCurrentModPack(); };
            if (args.Length == 0) {
                OpenCurrentModPack();
            }
            
            // fill CA file list
            string shogunPath = IOFunctions.GetShogunTotalWarDirectory();
            if (shogunPath != null) {
                shogunPath = Path.Combine(shogunPath, "data");
                if (Directory.Exists(shogunPath)) {
                    List<string> packFiles = new List<string> (Directory.GetFiles(shogunPath, "*.pack"));
                    packFiles.Sort(NumberedFileComparison);
                    packFiles.ForEach(file => openCAToolStripMenuItem.DropDownItems.Add(
                        new ToolStripMenuItem(Path.GetFileName(file), null, 
                                         delegate(object s, EventArgs a) { OpenExistingPackFile(file); })));
                    openCAToolStripMenuItem.Enabled = true;
                }
            }
            
            ModManager.ModChangeEvent enableMenuItem = delegate(string newMod) {
                bool enabled = newMod != "";
                enabled &= IOFunctions.GetShogunTotalWarDirectory() != null;
                installModMenuItem.Enabled = uninstallModMenuItem.Enabled = enabled;
            };
            enableMenuItem(Settings.Default.CurrentMod);
            ModManager.Instance.CurrentModChanged += enableMenuItem;

            csvToolStripMenuItem.Checked = "csv".Equals(Settings.Default.TsvExtension);
            tsvToolStripMenuItem.Checked = "tsv".Equals(Settings.Default.TsvExtension);
        }

        static readonly Regex NumberedFileNameRE = new Regex("([^0-9]*)([0-9]+).*");

        int NumberedFileComparison(string name1, string name2) {
            name1 = Path.GetFileName(name1);
            name2 = Path.GetFileName(name2);
            int result = name1.CompareTo(name2);
            if (NumberedFileNameRE.IsMatch(name1) && NumberedFileNameRE.IsMatch(name2)) {
                Match m1 = NumberedFileNameRE.Match(name1);
                Match m2 = NumberedFileNameRE.Match(name2);
                if (m1.Groups[1].Value.Equals(m2.Groups[1].Value)) {
                    int number1 = int.Parse(m1.Groups[2].Value);
                    int number2 = int.Parse(m2.Groups[2].Value);
                    result = number2 - number1;
                }
            }
            return result;
        }

        private void currentPackFile_Modified()
        {
            refreshTitle();
            EnableMenuItems();
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
            if ((((e.CloseReason != CloseReason.WindowsShutDown) && (e.CloseReason != CloseReason.TaskManagerClosing)) && (e.CloseReason != CloseReason.ApplicationExitCall)) && (handlePackFileChangesWithUserInput() == DialogResult.Cancel)) {
                e.Cancel = true;
            }
        }

        private void PackFileManagerForm_GotFocus(object sender, EventArgs e) {
            base.Activated -= new EventHandler (PackFileManagerForm_GotFocus);
            if (openFileIsModified) {
                openFileIsModified = false;
                if (MessageBox.Show ("Changes were made to the extracted file. Do you want to replace the packed file with the extracted file?", "Save changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    openPackedFile.Data = (File.ReadAllBytes (openFilePath));
                }
            }
            while (File.Exists(openFilePath)) {
                try {
                    File.Delete (openFilePath);
                } catch (IOException) {
                    if (MessageBox.Show ("Unable to delete the temporary file; is it still in use by the external editor?\r\n\r\nClick Retry to try deleting it again or Cancel to leave it in the temporary directory.", "Temporary file in use", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel) {
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
                    initialDialog = IOFunctions.GetShogunTotalWarDirectory();
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
                switch (MessageBox.Show(@"You modified the pack file. Do you want to save your changes?", @"Save Changes?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3))
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
            if ((handlePackFileChangesWithUserInput() != DialogResult.Cancel) && (packOpenFileDialog.ShowDialog() == DialogResult.OK)) {
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
                            // exclude files named patch_moviesX.packk
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
                closeEditors ();
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

        #endregion

        #region MyMod Menu
        private void newModMenuItem_Click(object sender, EventArgs e) {
            List<string> oldMods = ModManager.Instance.ModNames;
            string packFileName = ModManager.Instance.AddMod();
            if (packFileName != null) {
                // add mod entry to menu
                if (Settings.Default.CurrentMod != "") {
                    if (!oldMods.Contains(Settings.Default.CurrentMod)) {
                        modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(Settings.Default.CurrentMod, Settings.Default.CurrentMod));
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
                    SaveAsFile(ModManager.Instance.FullModPath);
                } else if (result == DialogResult.Cancel) {
                    return;
                }
            }
            try {
                ModManager.Instance.InstallCurrentMod();
            } catch (Exception ex) {
                MessageBox.Show(string.Format("Install failed: {0}", ex), "Install Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void uninstallModMenuItem_Click(object sender, EventArgs e) {
            try {
                ModManager.Instance.UninstallCurrentMod();
            } catch (Exception ex) {
                MessageBox.Show(string.Format("Uninstall failed: {0}", ex), "Uninstall Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                string modPath = ModManager.Instance.FullModPath;
                if (Settings.Default.CurrentMod != "" && File.Exists(modPath)) {
                    OpenExistingPackFile(modPath);
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
        private void getPackedFilesFromBranch(List<PackedFile> packedFiles, TreeNodeCollection trunk, FileFilter filter = null) {
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
            VirtualDirectory dir = packTreeView.SelectedNode != null ? packTreeView.SelectedNode.Tag as VirtualDirectory : CurrentPackFile.Root;
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
            List<PackedFile> packedFiles = new List<PackedFile>();
            packedFiles.Add(packTreeView.SelectedNode.Tag as PackedFile);
            openAsText(packedFiles[0]);
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
            if (verb == "openimage")
            {
                ImageViewerControl control = new ImageViewerControl {
                    Dock = DockStyle.Fill
                };
                imageViewerControl = control;
                splitContainer1.Panel2.Controls.Add(imageViewerControl);
                imageViewerControl.SetImage(packedFile.Data, openFilePath);
            }
            else
            {
                File.WriteAllBytes(openFilePath, packedFile.Data);
                openWith(openFilePath, verb);
            }
        }
        
        private void OpenPackedFile(object tag) {
            PackedFile packedFile = tag as PackedFile;
            if (packedFile.FullPath == "readme.xml") {
                openReadMe(packedFile);
            } else if (packedFile.FullPath.EndsWith(".loc")) {
                locFileEditorControl = new LocFileEditorControl(packedFile);
                locFileEditorControl.Dock = DockStyle.Fill;
                splitContainer1.Panel2.Controls.Add(locFileEditorControl);
            } else if (packedFile.FullPath.Contains(".tga") || packedFile.FullPath.Contains(".dds") || packedFile.FullPath.Contains(".png") || packedFile.FullPath.Contains(".jpg") || packedFile.FullPath.Contains(".bmp") || packedFile.FullPath.Contains(".psd")) {
                openExternal(packedFile, "openimage");
            } else if (packedFile.FullPath.EndsWith(".atlas")) {
                AtlasFileEditorControl control = new AtlasFileEditorControl(packedFile) {
                    Dock = DockStyle.Fill
                };
                atlasFileEditorControl = control;
                splitContainer1.Panel2.Controls.Add(atlasFileEditorControl);
            } else if (packedFile.FullPath.EndsWith(".unit_variant")) {
                UnitVariantFileEditorControl control2 = new UnitVariantFileEditorControl(packedFile) {
                    Dock = DockStyle.Fill
                };
                unitVariantFileEditorControl = control2;
                splitContainer1.Panel2.Controls.Add(unitVariantFileEditorControl);
            } else if (packedFile.FullPath.Contains(".rigid")) {
                // viewModel(packedFile);
            } else if (isTextFileType(packedFile)) {
                openAsText(packedFile);
            } else if (packedFile.FullPath.EndsWith(".esf")) {
                using (var stream = new MemoryStream(packedFile.Data)) {
                    EsfCodec codec = EsfCodecUtil.GetCodec(stream);
                    if (codec != null) {
                        esfEditor.RootNode = codec.Parse(packedFile.Data);
                    }
                    esfEditor.Tag = packedFile;
                    splitContainer1.Panel2.Controls.Add(esfEditor);
                }
            } else if (packedFile.FullPath.Contains("groupformations.bin")) {
                gfEditor.CurrentPackedFile = packedFile;
                if (!splitContainer1.Panel2.Controls.Contains(gfEditor)) {
                    splitContainer1.Panel2.Controls.Add(gfEditor);
                }
            } else if (packedFile.FullPath.StartsWith("db")) {
                try {
                    dbFileEditorControl.Open(packedFile, currentPackFile);
                    splitContainer1.Panel2.Controls.Add(dbFileEditorControl);
                } catch (FileNotFoundException exception) {
                    MessageBox.Show(exception.Message, "DB Type not found", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                } catch (Exception x) {
                    MessageBox.Show(x.Message + "\n" + x.StackTrace, "Problem, sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void closeEditors() {
            if (locFileEditorControl != null) {
                locFileEditorControl.Commit();
            }
            if (atlasFileEditorControl != null) {
                atlasFileEditorControl.Commit();
            }
            if (unitVariantFileEditorControl != null) {
                unitVariantFileEditorControl.Commit();
            }
            if (readmeEditorControl != null) {
                readmeEditorControl.updatePackedFile();
            }
            if (textFileEditorControl != null) {
                textFileEditorControl.updatePackedFile();
            }
            if (gfEditor != null) {
                gfEditor.Commit();
            }
            if (esfEditor.RootNode != null && esfEditor.RootNode.Modified) {
                byte[] data;
                var stream = new MemoryStream();
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    esfEditor.RootNode.Codec.EncodeRootNode(writer, esfEditor.RootNode);
                    esfEditor.RootNode.Modified = false;
                    data = stream.ToArray();
                }
                (esfEditor.Tag as PackedFile).Data = data;
            }
        }
        
        private void openWith(string file, string verb) {
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
        
        private void openAsText(PackedFile packedFile) {
            TextFileEditorControl control3 = new TextFileEditorControl(packedFile) {
                Dock = DockStyle.Fill
            };
            textFileEditorControl = control3;
            splitContainer1.Panel2.Controls.Add(textFileEditorControl);
        }

        private static bool isTextFileType(PackedFile file) {
            string[] extensions = {
                                      "txt", "lua", "csv", "fx", "fx_fragment", "h", "battle_script", "xml", 
                                      "tai", "xml.rigging", "placement", "hlsl"

                                  };
            bool result = false;
            foreach (string ext in extensions) {
                if (file.FullPath.EndsWith(ext)) {
                    result = true;
                    break;
                }
            }
            return result;
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
//            foreach (TreeNode node in packTreeView.Nodes) {
//                if (node.Nodes.Count > 0) {
//                    getPackedFilesFromBranch(packedFiles, node.Nodes, unknownDbFormat);
//                } else {
//                    packedFiles.Add(node.Tag as PackedFile);
//                }
//            }
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
                DBTypeMap.Instance.saveToFile(Path.GetDirectoryName(Application.ExecutablePath));
                string message = "You just saved your own DB definitions in a new file.\n" +
                    "This means that these will be used instead of the ones received in updates from TWC.\n" +
                    "Once you have uploaded your changes and they have been integrated,\n" +
                    "please delete the file schema_user.xml.";
                MessageBox.Show(message, "New User DB Definitions created", MessageBoxButtons.OK, MessageBoxIcon.Information);

            } catch (Exception x) {
                MessageBox.Show(string.Format("Could not save user db descriptions: {0}\nUser file won't be used anymore. A backup has been made.", x.Message));
            }
        }

        private void updateAllToolStripMenuItem_Click(object sender, EventArgs e) {
            if (currentPackFile != null) {
                foreach (PackedFile packedFile in currentPackFile.Files) {
                    updatePackedFile(packedFile);
                }
            }
        }

        private void updateCurrentToolStripMenuItem_Click(object sender, EventArgs e) {
            if (dbFileEditorControl.CurrentPackedFile != null) {
                updatePackedFile(dbFileEditorControl.CurrentPackedFile);
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            string path = Path.GetDirectoryName(Application.ExecutablePath);
            DBTypeMap.Instance.initializeTypeMap(path);
            MessageBox.Show("DB File Definitions reloaded.");
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
                    DBTypeMap.Instance.initializeTypeMap(path);
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
        private void updatePackedFile(PackedFile packedFile) {
            try {
                string key = DBFile.typename(packedFile.FullPath);
                if (DBTypeMap.Instance.IsSupported(key)) {
                    PackedFileDbCodec codec = PackedFileDbCodec.FromFilename(packedFile.FullPath);
                    int maxVersion = DBTypeMap.Instance.MaxVersion(key);
                    DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
                    if (header.Version < maxVersion) {
                        // found a more recent db definition; read data from db file
                        DBFile updatedFile = PackedFileDbCodec.Decode(packedFile);

                        // identify FieldInstances missing in db file
                        TypeInfo dbFileInfo = updatedFile.CurrentType;
                        TypeInfo targetInfo = DBTypeMap.Instance.GetVersionedInfo(header.GUID, key, maxVersion);
                        for (int i = dbFileInfo.fields.Count; i < targetInfo.fields.Count; i++) {
                            foreach (List<FieldInstance> entry in updatedFile.Entries) {
                                var field = new FieldInstance(targetInfo.fields[i], targetInfo.fields[i].DefaultValue);
                                entry.Add(field);
                            }
                        }
                        updatedFile.Header.Version = maxVersion;
                        packedFile.Data = codec.Encode(updatedFile);

                        if (dbFileEditorControl.CurrentPackedFile == packedFile) {
                            dbFileEditorControl.Open(packedFile, currentPackFile);
                        }
                    }
                }
            } catch (Exception x) {
                MessageBox.Show(string.Format("Could not update {0}: {1}", Path.GetFileName(packedFile.FullPath), x.Message));
            }
        }
        #endregion

        #region Options Menu
        private void cAPacksAreReadOnlyToolStripMenuItem_CheckStateChanged(object sender, EventArgs e) {
            if (cAPacksAreReadOnlyToolStripMenuItem.CheckState == CheckState.Unchecked) {
                var advisory = new caFileEditAdvisory();
                cAPacksAreReadOnlyToolStripMenuItem.CheckState = advisory.DialogResult == DialogResult.Yes ? CheckState.Unchecked : CheckState.Checked;
            }
        }

        private void updateOnStartupToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.UpdateOnStartup = updateOnStartupToolStripMenuItem.Checked;
        }

        private void showDecodeToolOnErrorToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.ShowDecodeToolOnError = showDecodeToolOnErrorToolStripMenuItem.Checked;
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
            closeEditors();
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
            refreshTitle();
            base.Refresh();
        }

        private void refreshTitle()
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
            search = new customMessageBox();
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

        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                Common.Utilities.DisposeHandlers(this);
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.packTreeView = new System.Windows.Forms.TreeView();
            this.packActionMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextAddMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextAddDirMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextAddEmptyDirMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextAddFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextImportTsvMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextDeleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextRenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.contextOpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextOpenExternalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextOpenDecodeToolMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextOpenTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextExtractMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextExtractSelectedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextExtractAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.emptyDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importTSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openCAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.changePackTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bootXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.releaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.movieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shader2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.exportFileListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newModMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.editModMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.installModMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.uninstallModMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteCurrentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.filesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openExternalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDecodeToolMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openAsTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllTsv = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.createReadMeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchForUpdateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fromXsdFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateDBFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateCurrentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extrasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cAPacksAreReadOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateOnStartupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showDecodeToolOnErrorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractTSVFileExtensionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.csvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.indexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.packStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.packActionProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.packActionMenuStrip.SuspendLayout();
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // packTreeView
            // 
            this.packTreeView.ContextMenuStrip = this.packActionMenuStrip;
            this.packTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packTreeView.ForeColor = System.Drawing.SystemColors.WindowText;
            this.packTreeView.HideSelection = false;
            this.packTreeView.Indent = 19;
            this.packTreeView.Location = new System.Drawing.Point(0, 0);
            this.packTreeView.Name = "packTreeView";
            this.packTreeView.Size = new System.Drawing.Size(198, 599);
            this.packTreeView.TabIndex = 2;
            this.packTreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.packTreeView_AfterLabelEdit);
            this.packTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.packTreeView_ItemDrag);
            this.packTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.packTreeView_AfterSelect);
            this.packTreeView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.packTreeView_MouseDoubleClick);
            this.packTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.packTreeView_MouseDown);
            this.packTreeView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.packTreeView_PreviewKeyDown);
            // 
            // packActionMenuStrip
            // 
            this.packActionMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contextAddMenuItem,
            this.contextDeleteMenuItem,
            this.contextRenameMenuItem,
            this.toolStripSeparator10,
            this.contextOpenMenuItem,
            this.contextExtractMenuItem});
            this.packActionMenuStrip.Name = "packActionMenuStrip";
            this.packActionMenuStrip.Size = new System.Drawing.Size(132, 120);
            this.packActionMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.packActionMenuStrip_Opening);
            // 
            // contextAddMenuItem
            // 
            this.contextAddMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contextAddDirMenuItem,
            this.contextAddEmptyDirMenuItem,
            this.contextAddFileMenuItem,
            this.contextImportTsvMenuItem});
            this.contextAddMenuItem.Name = "contextAddMenuItem";
            this.contextAddMenuItem.Size = new System.Drawing.Size(131, 22);
            this.contextAddMenuItem.Text = "Add";
            // 
            // contextAddDirMenuItem
            // 
            this.contextAddDirMenuItem.Name = "contextAddDirMenuItem";
            this.contextAddDirMenuItem.ShortcutKeyDisplayString = "Shift+Ins";
            this.contextAddDirMenuItem.Size = new System.Drawing.Size(185, 22);
            this.contextAddDirMenuItem.Text = "&Directory...";
            this.contextAddDirMenuItem.Click += new System.EventHandler(this.addDirectoryToolStripMenuItem_Click);
            // 
            // contextAddEmptyDirMenuItem
            // 
            this.contextAddEmptyDirMenuItem.Name = "contextAddEmptyDirMenuItem";
            this.contextAddEmptyDirMenuItem.Size = new System.Drawing.Size(185, 22);
            this.contextAddEmptyDirMenuItem.Text = "Empty Directory";
            this.contextAddEmptyDirMenuItem.Click += new System.EventHandler(this.emptyDirectoryToolStripMenuItem_Click);
            // 
            // contextAddFileMenuItem
            // 
            this.contextAddFileMenuItem.Name = "contextAddFileMenuItem";
            this.contextAddFileMenuItem.ShortcutKeyDisplayString = "Ins";
            this.contextAddFileMenuItem.Size = new System.Drawing.Size(185, 22);
            this.contextAddFileMenuItem.Text = "&File(s)...";
            this.contextAddFileMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // contextImportTsvMenuItem
            // 
            this.contextImportTsvMenuItem.Name = "contextImportTsvMenuItem";
            this.contextImportTsvMenuItem.Size = new System.Drawing.Size(185, 22);
            this.contextImportTsvMenuItem.Text = "DB file from TSV";
            this.contextImportTsvMenuItem.Click += new System.EventHandler(this.dBFileFromTSVToolStripMenuItem_Click);
            // 
            // contextDeleteMenuItem
            // 
            this.contextDeleteMenuItem.Name = "contextDeleteMenuItem";
            this.contextDeleteMenuItem.ShortcutKeyDisplayString = "Del";
            this.contextDeleteMenuItem.Size = new System.Drawing.Size(131, 22);
            this.contextDeleteMenuItem.Text = "Delete";
            this.contextDeleteMenuItem.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // contextRenameMenuItem
            // 
            this.contextRenameMenuItem.Name = "contextRenameMenuItem";
            this.contextRenameMenuItem.Size = new System.Drawing.Size(131, 22);
            this.contextRenameMenuItem.Text = "Rename";
            this.contextRenameMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(128, 6);
            // 
            // contextOpenMenuItem
            // 
            this.contextOpenMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contextOpenExternalMenuItem,
            this.contextOpenDecodeToolMenuItem,
            this.contextOpenTextMenuItem});
            this.contextOpenMenuItem.Name = "contextOpenMenuItem";
            this.contextOpenMenuItem.Size = new System.Drawing.Size(131, 22);
            this.contextOpenMenuItem.Text = "Open";
            // 
            // contextOpenExternalMenuItem
            // 
            this.contextOpenExternalMenuItem.Name = "contextOpenExternalMenuItem";
            this.contextOpenExternalMenuItem.Size = new System.Drawing.Size(156, 22);
            this.contextOpenExternalMenuItem.Text = "Open External...";
            this.contextOpenExternalMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // contextOpenDecodeToolMenuItem
            // 
            this.contextOpenDecodeToolMenuItem.Name = "contextOpenDecodeToolMenuItem";
            this.contextOpenDecodeToolMenuItem.Size = new System.Drawing.Size(156, 22);
            this.contextOpenDecodeToolMenuItem.Text = "Open DecodeTool...";
            this.contextOpenDecodeToolMenuItem.Click += new System.EventHandler(this.openDecodeToolMenuItem_Click);
            // 
            // contextOpenTextMenuItem
            // 
            this.contextOpenTextMenuItem.Name = "contextOpenTextMenuItem";
            this.contextOpenTextMenuItem.Size = new System.Drawing.Size(156, 22);
            this.contextOpenTextMenuItem.Text = "Open as Text";
            this.contextOpenTextMenuItem.Click += new System.EventHandler(this.openAsTextMenuItem_Click);
            // 
            // contextExtractMenuItem
            // 
            this.contextExtractMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contextExtractSelectedMenuItem,
            this.contextExtractAllMenuItem,
            this.extractUnknownToolStripMenuItem});
            this.contextExtractMenuItem.Name = "contextExtractMenuItem";
            this.contextExtractMenuItem.Size = new System.Drawing.Size(131, 22);
            this.contextExtractMenuItem.Text = "Extract";
            // 
            // contextExtractSelectedMenuItem
            // 
            this.contextExtractSelectedMenuItem.Name = "contextExtractSelectedMenuItem";
            this.contextExtractSelectedMenuItem.ShortcutKeyDisplayString = "Ctl+X";
            this.contextExtractSelectedMenuItem.Size = new System.Drawing.Size(202, 22);
            this.contextExtractSelectedMenuItem.Text = "Extract &Selected...";
            this.contextExtractSelectedMenuItem.Click += new System.EventHandler(this.extractSelectedToolStripMenuItem_Click);
            // 
            // extractAllToolStripMenuItem2
            // 
            this.contextExtractAllMenuItem.Name = "contextExtractAllMenuItem";
            this.contextExtractAllMenuItem.Size = new System.Drawing.Size(202, 22);
            this.contextExtractAllMenuItem.Text = "Extract &All...";
            this.contextExtractAllMenuItem.Click += new System.EventHandler(this.extractAllToolStripMenuItem_Click);
            // 
            // extractUnknownToolStripMenuItem
            // 
            this.extractUnknownToolStripMenuItem.Name = "extractUnknownToolStripMenuItem";
            this.extractUnknownToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.extractUnknownToolStripMenuItem.Text = "Extract Unknown...";
            this.extractUnknownToolStripMenuItem.Click += new System.EventHandler(this.exportUnknownToolStripMenuItem_Click);
            // 
            // extractSelectedToolStripMenuItem
            // 
            this.extractSelectedToolStripMenuItem.Name = "extractSelectedToolStripMenuItem";
            this.extractSelectedToolStripMenuItem.ShortcutKeyDisplayString = "Ctl+X";
            this.extractSelectedToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.extractSelectedToolStripMenuItem.Text = "Extract &Selected...";
            this.extractSelectedToolStripMenuItem.Click += new System.EventHandler(this.extractSelectedToolStripMenuItem_Click);
            // 
            // extractAllToolStripMenuItem
            // 
            this.extractAllToolStripMenuItem.Name = "extractAllToolStripMenuItem";
            this.extractAllToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.extractAllToolStripMenuItem.Text = "Extract &All...";
            this.extractAllToolStripMenuItem.Click += new System.EventHandler(this.extractAllToolStripMenuItem_Click);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.emptyDirectoryToolStripMenuItem,
            this.addDirectoryToolStripMenuItem,
            this.addFileToolStripMenuItem,
            this.importTSVToolStripMenuItem});
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.addToolStripMenuItem.Text = "Add";
            // 
            // emptyDirectoryToolStripMenuItem
            // 
            this.emptyDirectoryToolStripMenuItem.Name = "emptyDirectoryToolStripMenuItem";
            this.emptyDirectoryToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.emptyDirectoryToolStripMenuItem.Text = "Empty Directory";
            this.emptyDirectoryToolStripMenuItem.Click += new System.EventHandler(this.emptyDirectoryToolStripMenuItem_Click);
            // 
            // addDirectoryToolStripMenuItem
            // 
            this.addDirectoryToolStripMenuItem.Name = "addDirectoryToolStripMenuItem";
            this.addDirectoryToolStripMenuItem.ShortcutKeyDisplayString = "Shift+Ins";
            this.addDirectoryToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.addDirectoryToolStripMenuItem.Text = "&Directory...";
            this.addDirectoryToolStripMenuItem.Click += new System.EventHandler(this.addDirectoryToolStripMenuItem_Click);
            // 
            // addFileToolStripMenuItem
            // 
            this.addFileToolStripMenuItem.Name = "addFileToolStripMenuItem";
            this.addFileToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
            this.addFileToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.addFileToolStripMenuItem.Text = "&File(s)...";
            this.addFileToolStripMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // importTSVToolStripMenuItem
            // 
            this.importTSVToolStripMenuItem.Name = "importTSVToolStripMenuItem";
            this.importTSVToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.importTSVToolStripMenuItem.Text = "Import TSV";
            this.importTSVToolStripMenuItem.Click += new System.EventHandler(this.dBFileFromTSVToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Location = new System.Drawing.Point(-2, 27);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.packTreeView);
            this.splitContainer1.Size = new System.Drawing.Size(909, 603);
            this.splitContainer1.SplitterDistance = 202;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 9;
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.modsToolStripMenuItem,
            this.filesMenu,
            this.editToolStripMenuItem,
            this.updateToolStripMenuItem,
            this.extrasToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(906, 24);
            this.menuStrip.TabIndex = 10;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.openCAToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator2,
            this.changePackTypeToolStripMenuItem,
            this.toolStripSeparator9,
            this.exportFileListToolStripMenuItem,
            this.toolStripSeparator7,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // openCAToolStripMenuItem
            // 
            this.openCAToolStripMenuItem.Enabled = false;
            this.openCAToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openCAToolStripMenuItem.Name = "openCAToolStripMenuItem";
            this.openCAToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.openCAToolStripMenuItem.Text = "Open CA pack...";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(169, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(169, 6);
            // 
            // changePackTypeToolStripMenuItem
            // 
            this.changePackTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bootToolStripMenuItem,
            this.bootXToolStripMenuItem,
            this.releaseToolStripMenuItem,
            this.patchToolStripMenuItem,
            this.modToolStripMenuItem,
            this.movieToolStripMenuItem,
            this.shaderToolStripMenuItem,
            this.shader2ToolStripMenuItem});
            this.changePackTypeToolStripMenuItem.Enabled = false;
            this.changePackTypeToolStripMenuItem.Name = "changePackTypeToolStripMenuItem";
            this.changePackTypeToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.changePackTypeToolStripMenuItem.Text = "Change Pack &Type";
            // 
            // bootToolStripMenuItem
            // 
            this.bootToolStripMenuItem.CheckOnClick = true;
            this.bootToolStripMenuItem.Name = "bootToolStripMenuItem";
            this.bootToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.bootToolStripMenuItem.Text = "Boot";
            this.bootToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // bootXToolStripMenuItem
            // 
            this.bootXToolStripMenuItem.CheckOnClick = true;
            this.bootXToolStripMenuItem.Name = "bootXToolStripMenuItem";
            this.bootXToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.bootXToolStripMenuItem.Text = "BootX";
            this.bootXToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // releaseToolStripMenuItem
            // 
            this.releaseToolStripMenuItem.CheckOnClick = true;
            this.releaseToolStripMenuItem.Name = "releaseToolStripMenuItem";
            this.releaseToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.releaseToolStripMenuItem.Text = "Release";
            this.releaseToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // patchToolStripMenuItem
            // 
            this.patchToolStripMenuItem.CheckOnClick = true;
            this.patchToolStripMenuItem.Name = "patchToolStripMenuItem";
            this.patchToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.patchToolStripMenuItem.Text = "Patch";
            this.patchToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // modToolStripMenuItem
            // 
            this.modToolStripMenuItem.Name = "modToolStripMenuItem";
            this.modToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.modToolStripMenuItem.Text = "Mod";
            this.modToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // movieToolStripMenuItem
            // 
            this.movieToolStripMenuItem.CheckOnClick = true;
            this.movieToolStripMenuItem.Name = "movieToolStripMenuItem";
            this.movieToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.movieToolStripMenuItem.Text = "Movie";
            this.movieToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // shaderToolStripMenuItem
            // 
            this.shaderToolStripMenuItem.CheckOnClick = true;
            this.shaderToolStripMenuItem.Name = "shaderToolStripMenuItem";
            this.shaderToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.shaderToolStripMenuItem.Text = "Shader";
            this.shaderToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // shader2ToolStripMenuItem
            // 
            this.shader2ToolStripMenuItem.CheckOnClick = true;
            this.shader2ToolStripMenuItem.Name = "shader2ToolStripMenuItem";
            this.shader2ToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.shader2ToolStripMenuItem.Text = "Shader2";
            this.shader2ToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(169, 6);
            // 
            // exportFileListToolStripMenuItem
            // 
            this.exportFileListToolStripMenuItem.Enabled = false;
            this.exportFileListToolStripMenuItem.Name = "exportFileListToolStripMenuItem";
            this.exportFileListToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.exportFileListToolStripMenuItem.Text = "Export File &List...";
            this.exportFileListToolStripMenuItem.Click += new System.EventHandler(this.exportFileListToolStripMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(169, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // modsToolStripMenuItem
            // 
            this.modsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newModMenuItem,
            this.toolStripSeparator11,
            this.editModMenuItem,
            this.installModMenuItem,
            this.uninstallModMenuItem,
            this.deleteCurrentToolStripMenuItem,
            this.toolStripSeparator12});
            this.modsToolStripMenuItem.Name = "modsToolStripMenuItem";
            this.modsToolStripMenuItem.Size = new System.Drawing.Size(69, 20);
            this.modsToolStripMenuItem.Text = "My Mods";
            // 
            // newModMenuItem
            // 
            this.newModMenuItem.Name = "newModMenuItem";
            this.newModMenuItem.Size = new System.Drawing.Size(163, 22);
            this.newModMenuItem.Text = "New";
            this.newModMenuItem.Click += new System.EventHandler(this.newModMenuItem_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(160, 6);
            // 
            // editModMenuItem
            // 
            this.editModMenuItem.Name = "editModMenuItem";
            this.editModMenuItem.Size = new System.Drawing.Size(163, 22);
            this.editModMenuItem.Text = "Edit Current";
            this.editModMenuItem.Visible = false;
            // 
            // installModMenuItem
            // 
            this.installModMenuItem.Name = "installModMenuItem";
            this.installModMenuItem.Size = new System.Drawing.Size(163, 22);
            this.installModMenuItem.Text = "Install Current";
            this.installModMenuItem.Click += new System.EventHandler(this.installModMenuItem_Click);
            // 
            // uninstallModMenuItem
            // 
            this.uninstallModMenuItem.Name = "uninstallModMenuItem";
            this.uninstallModMenuItem.Size = new System.Drawing.Size(163, 22);
            this.uninstallModMenuItem.Text = "Uninstall Current";
            this.uninstallModMenuItem.Click += new System.EventHandler(this.uninstallModMenuItem_Click);
            // 
            // deleteCurrentToolStripMenuItem
            // 
            this.deleteCurrentToolStripMenuItem.Name = "deleteCurrentToolStripMenuItem";
            this.deleteCurrentToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.deleteCurrentToolStripMenuItem.Text = "Delete Current";
            this.deleteCurrentToolStripMenuItem.Click += new System.EventHandler(this.deleteCurrentToolStripMenuItem_Click);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(160, 6);
            // 
            // filesMenu
            // 
            this.filesMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.deleteFileToolStripMenuItem,
            this.replaceFileToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.toolStripSeparator4,
            this.openMenuItem,
            this.extractToolStripMenuItem,
            this.toolStripSeparator8,
            this.createReadMeToolStripMenuItem,
            this.searchFileToolStripMenuItem});
            this.filesMenu.Enabled = false;
            this.filesMenu.Name = "filesMenu";
            this.filesMenu.Size = new System.Drawing.Size(42, 20);
            this.filesMenu.Text = "Files";
            // 
            // deleteFileToolStripMenuItem
            // 
            this.deleteFileToolStripMenuItem.Name = "deleteFileToolStripMenuItem";
            this.deleteFileToolStripMenuItem.ShortcutKeyDisplayString = "Del";
            this.deleteFileToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.deleteFileToolStripMenuItem.Text = "Delete";
            this.deleteFileToolStripMenuItem.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // replaceFileToolStripMenuItem
            // 
            this.replaceFileToolStripMenuItem.Name = "replaceFileToolStripMenuItem";
            this.replaceFileToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.replaceFileToolStripMenuItem.Text = "&Replace File...";
            this.replaceFileToolStripMenuItem.Click += new System.EventHandler(this.replaceFileToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this.renameToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.renameToolStripMenuItem.Text = "Rename";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(151, 6);
            // 
            // openToolStripMenuItem1
            // 
            this.openMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openExternalMenuItem,
            this.openDecodeToolMenuItem,
            this.openAsTextMenuItem});
            this.openMenuItem.Name = "openToolStripMenuItem1";
            this.openMenuItem.Size = new System.Drawing.Size(154, 22);
            this.openMenuItem.Text = "Open";
            // 
            // openExternalMenuItem
            // 
            this.openExternalMenuItem.Name = "openExternalMenuItem";
            this.openExternalMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openExternalMenuItem.Text = "Open External...";
            this.openExternalMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // openDecodeToolMenuItem
            // 
            this.openDecodeToolMenuItem.Name = "openDecodeToolMenuItem";
            this.openDecodeToolMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openDecodeToolMenuItem.Text = "Open DecodeTool...";
            this.openDecodeToolMenuItem.Click += new System.EventHandler(this.openDecodeToolMenuItem_Click);
            // 
            // openAsTextMenuItem
            // 
            this.openAsTextMenuItem.Name = "openAsTextMenuItem";
            this.openAsTextMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openAsTextMenuItem.Text = "Open as Text";
            this.openAsTextMenuItem.Click += new System.EventHandler(this.openAsTextMenuItem_Click);
            // 
            // extractToolStripMenuItem
            // 
            this.extractToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.extractSelectedToolStripMenuItem,
            this.extractAllToolStripMenuItem,
            this.exportUnknownToolStripMenuItem,
            this.extractAllTsv});
            this.extractToolStripMenuItem.Name = "extractToolStripMenuItem";
            this.extractToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.extractToolStripMenuItem.Text = "Extract";
            // 
            // exportUnknownToolStripMenuItem
            // 
            this.exportUnknownToolStripMenuItem.Name = "exportUnknownToolStripMenuItem";
            this.exportUnknownToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.exportUnknownToolStripMenuItem.Text = "Extract Unknown...";
            this.exportUnknownToolStripMenuItem.Click += new System.EventHandler(this.exportUnknownToolStripMenuItem_Click);
            // 
            // extractAllTsv
            // 
            this.extractAllTsv.Name = "extractAllTsv";
            this.extractAllTsv.Size = new System.Drawing.Size(202, 22);
            this.extractAllTsv.Text = "Extract All as TSV...";
            this.extractAllTsv.Click += new System.EventHandler(this.extractAllTsv_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(151, 6);
            // 
            // createReadMeToolStripMenuItem
            // 
            this.createReadMeToolStripMenuItem.Enabled = false;
            this.createReadMeToolStripMenuItem.Name = "createReadMeToolStripMenuItem";
            this.createReadMeToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.createReadMeToolStripMenuItem.Text = "Create ReadMe";
            this.createReadMeToolStripMenuItem.Click += new System.EventHandler(this.createReadMeToolStripMenuItem_Click);
            // 
            // searchFileToolStripMenuItem
            // 
            this.searchFileToolStripMenuItem.Name = "searchFileToolStripMenuItem";
            this.searchFileToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.searchFileToolStripMenuItem.Text = "Search Files...";
            this.searchFileToolStripMenuItem.Click += new System.EventHandler(this.searchFileToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripSeparator3,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripSeparator6,
            this.selectAllToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            this.editToolStripMenuItem.Visible = false;
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.undoToolStripMenuItem.Text = "&Undo";
            // 
            // redoToolStripMenuItem
            // 
            this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            this.redoToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.redoToolStripMenuItem.Text = "&Redo";
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(143, 6);
            // 
            // cutToolStripMenuItem
            // 
            this.cutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.cutToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.cutToolStripMenuItem.Text = "Cu&t";
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.copyToolStripMenuItem.Text = "&Copy";
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.pasteToolStripMenuItem.Text = "&Paste";
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(143, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.selectAllToolStripMenuItem.Text = "Select &All";
            // 
            // updateToolStripMenuItem
            // 
            this.updateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.searchForUpdateToolStripMenuItem,
            this.fromXsdFileToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.saveToDirectoryToolStripMenuItem,
            this.updateDBFilesToolStripMenuItem});
            this.updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            this.updateToolStripMenuItem.Size = new System.Drawing.Size(102, 20);
            this.updateToolStripMenuItem.Text = "DB Descriptions";
            // 
            // searchForUpdateToolStripMenuItem
            // 
            this.searchForUpdateToolStripMenuItem.Name = "searchForUpdateToolStripMenuItem";
            this.searchForUpdateToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.searchForUpdateToolStripMenuItem.Text = "Search for Update";
            this.searchForUpdateToolStripMenuItem.Click += new System.EventHandler(this.updateToolStripMenuItem_Click);
            // 
            // fromXsdFileToolStripMenuItem
            // 
            this.fromXsdFileToolStripMenuItem.Enabled = false;
            this.fromXsdFileToolStripMenuItem.Name = "fromXsdFileToolStripMenuItem";
            this.fromXsdFileToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.fromXsdFileToolStripMenuItem.Text = "Load from xsd File";
            this.fromXsdFileToolStripMenuItem.Visible = false;
            this.fromXsdFileToolStripMenuItem.Click += new System.EventHandler(this.fromXsdFileToolStripMenuItem_Click);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.reloadToolStripMenuItem.Text = "Reload from Local Directory";
            this.reloadToolStripMenuItem.Visible = false;
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // saveToDirectoryToolStripMenuItem
            // 
            this.saveToDirectoryToolStripMenuItem.Name = "saveToDirectoryToolStripMenuItem";
            this.saveToDirectoryToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.saveToDirectoryToolStripMenuItem.Text = "Save to Directory";
            this.saveToDirectoryToolStripMenuItem.Click += new System.EventHandler(this.saveToDirectoryToolStripMenuItem_Click);
            // 
            // updateDBFilesToolStripMenuItem
            // 
            this.updateDBFilesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.updateCurrentToolStripMenuItem,
            this.updateAllToolStripMenuItem});
            this.updateDBFilesToolStripMenuItem.Name = "updateDBFilesToolStripMenuItem";
            this.updateDBFilesToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.updateDBFilesToolStripMenuItem.Text = "Update DB Files";
            // 
            // updateCurrentToolStripMenuItem
            // 
            this.updateCurrentToolStripMenuItem.Name = "updateCurrentToolStripMenuItem";
            this.updateCurrentToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.updateCurrentToolStripMenuItem.Text = "Update Current";
            this.updateCurrentToolStripMenuItem.Click += new System.EventHandler(this.updateCurrentToolStripMenuItem_Click);
            // 
            // updateAllToolStripMenuItem
            // 
            this.updateAllToolStripMenuItem.Name = "updateAllToolStripMenuItem";
            this.updateAllToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.updateAllToolStripMenuItem.Text = "Update All";
            this.updateAllToolStripMenuItem.Click += new System.EventHandler(this.updateAllToolStripMenuItem_Click);
            // 
            // extrasToolStripMenuItem
            // 
            this.extrasToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cAPacksAreReadOnlyToolStripMenuItem,
            this.updateOnStartupToolStripMenuItem,
            this.showDecodeToolOnErrorToolStripMenuItem,
            this.extractTSVFileExtensionToolStripMenuItem});
            this.extrasToolStripMenuItem.Name = "extrasToolStripMenuItem";
            this.extrasToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.extrasToolStripMenuItem.Text = "Options";
            // 
            // cAPacksAreReadOnlyToolStripMenuItem
            // 
            this.cAPacksAreReadOnlyToolStripMenuItem.Checked = true;
            this.cAPacksAreReadOnlyToolStripMenuItem.CheckOnClick = true;
            this.cAPacksAreReadOnlyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cAPacksAreReadOnlyToolStripMenuItem.Name = "cAPacksAreReadOnlyToolStripMenuItem";
            this.cAPacksAreReadOnlyToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.cAPacksAreReadOnlyToolStripMenuItem.Text = "CA Packs Are Read Only";
            this.cAPacksAreReadOnlyToolStripMenuItem.ToolTipText = "If checked, the original pack files for the game can be viewed but not edited.";
            // 
            // updateOnStartupToolStripMenuItem
            // 
            this.updateOnStartupToolStripMenuItem.CheckOnClick = true;
            this.updateOnStartupToolStripMenuItem.Name = "updateOnStartupToolStripMenuItem";
            this.updateOnStartupToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.updateOnStartupToolStripMenuItem.Text = "Update on Startup";
            this.updateOnStartupToolStripMenuItem.Click += new System.EventHandler(this.updateOnStartupToolStripMenuItem_Click);
            // 
            // showDecodeToolOnErrorToolStripMenuItem
            // 
            this.showDecodeToolOnErrorToolStripMenuItem.CheckOnClick = true;
            this.showDecodeToolOnErrorToolStripMenuItem.Name = "showDecodeToolOnErrorToolStripMenuItem";
            this.showDecodeToolOnErrorToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.showDecodeToolOnErrorToolStripMenuItem.Text = "Show Decode Tool on Error";
            this.showDecodeToolOnErrorToolStripMenuItem.Click += new System.EventHandler(this.showDecodeToolOnErrorToolStripMenuItem_Click);
            // 
            // extractTSVFileExtensionToolStripMenuItem
            // 
            this.extractTSVFileExtensionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.csvToolStripMenuItem,
            this.tsvToolStripMenuItem});
            this.extractTSVFileExtensionToolStripMenuItem.Name = "extractTSVFileExtensionToolStripMenuItem";
            this.extractTSVFileExtensionToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.extractTSVFileExtensionToolStripMenuItem.Text = "Extract TSV File Extension";
            // 
            // csvToolStripMenuItem
            // 
            this.csvToolStripMenuItem.Checked = true;
            this.csvToolStripMenuItem.CheckOnClick = true;
            this.csvToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.csvToolStripMenuItem.Name = "csvToolStripMenuItem";
            this.csvToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.csvToolStripMenuItem.Text = "csv";
            this.csvToolStripMenuItem.Click += new System.EventHandler(this.extensionSelectionChanged);
            // 
            // tsvToolStripMenuItem
            // 
            this.tsvToolStripMenuItem.CheckOnClick = true;
            this.tsvToolStripMenuItem.Name = "tsvToolStripMenuItem";
            this.tsvToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.tsvToolStripMenuItem.Text = "tsv";
            this.tsvToolStripMenuItem.Click += new System.EventHandler(this.extensionSelectionChanged);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contentsToolStripMenuItem,
            this.indexToolStripMenuItem,
            this.searchToolStripMenuItem,
            this.toolStripSeparator5,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // contentsToolStripMenuItem
            // 
            this.contentsToolStripMenuItem.Name = "contentsToolStripMenuItem";
            this.contentsToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.contentsToolStripMenuItem.Text = "&Contents";
            this.contentsToolStripMenuItem.Visible = false;
            // 
            // indexToolStripMenuItem
            // 
            this.indexToolStripMenuItem.Name = "indexToolStripMenuItem";
            this.indexToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.indexToolStripMenuItem.Text = "&Index";
            this.indexToolStripMenuItem.Visible = false;
            // 
            // searchToolStripMenuItem
            // 
            this.searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            this.searchToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.searchToolStripMenuItem.Text = "&Search";
            this.searchToolStripMenuItem.Visible = false;
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(119, 6);
            this.toolStripSeparator5.Visible = false;
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.aboutToolStripMenuItem.Text = "&About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.packStatusLabel,
            this.packActionProgressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 628);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(906, 22);
            this.statusStrip.TabIndex = 11;
            this.statusStrip.Text = "statusStrip1";
            // 
            // packStatusLabel
            // 
            this.packStatusLabel.Name = "packStatusLabel";
            this.packStatusLabel.Size = new System.Drawing.Size(769, 17);
            this.packStatusLabel.Spring = true;
            this.packStatusLabel.Text = "Use the File menu to create a new pack file or open an existing one.";
            this.packStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // packActionProgressBar
            // 
            this.packActionProgressBar.Name = "packActionProgressBar";
            this.packActionProgressBar.Size = new System.Drawing.Size(120, 16);
            // 
            // PackFileManagerForm
            // 
            this.AutoScroll = true;
            this.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.ClientSize = new System.Drawing.Size(906, 650);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.Location = new System.Drawing.Point(192, 114);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "PackFileManagerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Pack File Manager 10.0.40219.1 BETA (Total War - Shogun 2)";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Activated += new System.EventHandler(this.PackFileManagerForm_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PackFileManagerForm_FormClosing);
            this.Load += new System.EventHandler(this.PackFileManagerForm_Load);
            this.Shown += new System.EventHandler(this.PackFileManagerForm_Shown);
            this.packActionMenuStrip.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            //((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.IO.FileSystemWatcher openFileWatcher;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contentsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem indexToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openCAToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip packActionMenuStrip;
        private System.Windows.Forms.ToolStripProgressBar packActionProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel packStatusLabel;
        public System.Windows.Forms.TreeView packTreeView;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searchForUpdateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fromXsdFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filesMenu;
        private System.Windows.Forms.ToolStripMenuItem changePackTypeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bootToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bootXToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem releaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem patchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem movieToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shaderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shader2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replaceFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDecodeToolMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openExternalMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openAsTextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractSelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportUnknownToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem searchFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateDBFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateCurrentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportFileListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createReadMeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extrasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cAPacksAreReadOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateOnStartupToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem contextAddMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextAddFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextAddDirMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextDeleteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextRenameMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem contextOpenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextOpenExternalMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextOpenDecodeToolMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextOpenTextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextExtractMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextExtractSelectedMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextExtractAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractUnknownToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem emptyDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextAddEmptyDirMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importTSVToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem contextImportTsvMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showDecodeToolOnErrorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem extractAllTsv;
        private System.Windows.Forms.ToolStripMenuItem modsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newModMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem editModMenuItem;
        private System.Windows.Forms.ToolStripMenuItem installModMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uninstallModMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteCurrentToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;

        private void extensionSelectionChanged(object sender, EventArgs e) {
            Settings.Default.TsvExtension = (sender as ToolStripMenuItem).Text;
            csvToolStripMenuItem.Checked = "csv".Equals(Settings.Default.TsvExtension);
            tsvToolStripMenuItem.Checked = "tsv".Equals(Settings.Default.TsvExtension);
        }
    }

    class LoadUpdater 
    {
        private readonly string file;
        private int currentCount;
        private uint count;
        private readonly ToolStripLabel label;
        private readonly ToolStripProgressBar progress;
        PackFileCodec currentCodec;
        public LoadUpdater(PackFileCodec codec, string f, ToolStripLabel l, ToolStripProgressBar bar) 
        {
            file = Path.GetFileName(f);
            label = l;
            progress = bar;
            bar.Minimum = 0;
            bar.Value = 0;
            bar.Step = 10;
            Connect(codec);
        }

        public void Connect(PackFileCodec codec) 
        {
            codec.HeaderLoaded += HeaderLoaded;
            codec.PackedFileLoaded += PackedFileLoaded;
            codec.PackFileLoaded += PackFileLoaded;
            currentCodec = codec;
        }

        public void Disconnect() 
        {
            if (currentCodec != null) 
            {
                currentCodec.HeaderLoaded -= HeaderLoaded;
                currentCodec.PackedFileLoaded -= PackedFileLoaded;
                currentCodec.PackFileLoaded -= PackFileLoaded;
                currentCodec = null;
            }
        }

        public void HeaderLoaded(PFHeader header) 
        {
            count = header.FileCount;
            progress.Maximum = (int) header.FileCount;
            label.Text = string.Format("Loading {0}: 0 of {1} files loaded", file, header.FileCount);
            Application.DoEvents();
        }

        public void PackedFileLoaded(PackedFile packedFile) 
        {
            currentCount++;
            if (currentCount % 10 <= 0) 
            {
                label.Text = string.Format("Opening {0} ({1} of {2} files loaded)", packedFile, currentCount, count);
                progress.PerformStep();
                Application.DoEvents();
            }
        }

        public void PackFileLoaded(PackFile packedFile)
        {
            label.Text = string.Format("Finished opening {0} - {1} files loaded", packedFile, count);
            progress.Maximum = 0;
            Disconnect();
        }
    }
}

