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

        private PackFile currentPackFile;
        private PackedFile openPackedFile;

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

            modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem("None", ""));
            ModManager.Instance.ModNames.ForEach(name => modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(name, name)));
            ModManager.Instance.CurrentModChanged += delegate(string newMod) { OpenCurrentModPack(); };
            if (args.Length == 0) {
                OpenCurrentModPack();
            }
            
            string shogunPath = IOFunctions.GetShogunTotalWarDirectory();
            if (shogunPath != null) {
                shogunPath = Path.Combine(shogunPath, "data");
                List<string> packFiles = new List<string> (Directory.GetFiles(shogunPath, "*.pack"));
                packFiles.Sort();
                packFiles.ForEach(file => openCAToolStripMenuItem.DropDownItems.Add(
                    new ToolStripMenuItem(Path.GetFileName(file), null, 
                                     delegate(object s, EventArgs a) { OpenExistingPackFile(file); })));
            }
            
            ModManager.ModChangeEvent enableMenuItem = delegate(string newMod) {
                bool enabled = newMod != "";
                enabled &= IOFunctions.GetShogunTotalWarDirectory() != null;
                installModMenuItem.Enabled = uninstallModMenuItem.Enabled = enabled;
            };
            enableMenuItem(Settings.Default.CurrentMod);
            ModManager.Instance.CurrentModChanged += enableMenuItem;
        }

        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

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

        private void currentPackFile_Modified()
        {
            refreshTitle();
            EnableMenuItems();
        }

        #region Entry Add/Delete
        VirtualDirectory AddTo {
            get {
                VirtualDirectory addTo;
                if (packTreeView.SelectedNode == null) {
                    addTo = CurrentPackFile.Root;
                } else {
                    addTo = packTreeView.SelectedNode.Tag as VirtualDirectory ?? packTreeView.SelectedNode.Parent.Tag as VirtualDirectory;
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
            if (AddTo == null) {
                return;
            }
            var addReplaceOpenFileDialog = new OpenFileDialog {
                InitialDirectory = Settings.Default.ImportExportDirectory,
                Multiselect = true
            };
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK) {
                Settings.Default.ImportExportDirectory = Path.GetDirectoryName(addReplaceOpenFileDialog.FileName);
                // if (Path.GetDirectoryName(addReplaceOpenFileDialog.FileName).StartsWith(
                try {
                    foreach (string file in addReplaceOpenFileDialog.FileNames) {
                        string addBase = (Settings.Default.CurrentMod != "") 
                            ? GetPathRelativeToMod(file) : Path.GetFileName(file);
                        AddTo.Add(addBase, new PackedFile(file));
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
                    MessageBox.Show(string.Format("Failed to add {0}: {1}", addDirectoryFolderBrowserDialog.SelectedPath, x.Message), "Failed to add directory");
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
            var dir = packTreeView.SelectedNode.Tag as VirtualDirectory;
            if (dir != null) {
                OpenFileDialog openDBFileDialog = new OpenFileDialog {
                    InitialDirectory = Settings.Default.ImportExportDirectory,
                    Filter = IOFunctions.TSV_FILTER
                };
                if (openDBFileDialog.ShowDialog() == DialogResult.OK) {
                    Settings.Default.ImportExportDirectory = Path.GetDirectoryName(openDBFileDialog.FileName);
                    try {
                        using (FileStream filestream = File.OpenRead(openDBFileDialog.FileName)) {
                            string filename = Path.GetFileNameWithoutExtension(openDBFileDialog.FileName);
                            DBFile file = new TextDbCodec().Decode(filestream);
                            byte[] data = PackedFileDbCodec.FromFilename(filename).Encode(file);
                            dir.Add(new PackedFile { Data = data, Name = filename, Parent = dir });
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

        #region Extract
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
            foreach (TreeNode node in packTreeView.Nodes) {
                if (node.Nodes.Count > 0) {
                    getPackedFilesFromBranch(packedFiles, node.Nodes, unknownDbFormat);
                } else {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
            new FileExtractor(packStatusLabel, packActionProgressBar).extractFiles(packedFiles);
        }

        private void extractAllTsv_Click(object sender, EventArgs e) {
            List<PackedFile> files = new List<PackedFile>();
            VirtualDirectory dir = currentPackFile.Root.getSubdirectory("db");
            foreach (VirtualDirectory subDir in dir.Subdirectories) {
                files.AddRange(subDir.Files);
            }
            FileExtractor extractor = new FileExtractor(packStatusLabel, packActionProgressBar) {
                Preprocessor = new TsvConversionPreprocessor()
            };
            extractor.extractFiles(files);
        }
        #endregion

        protected void EnableMenuItems() {
            createReadMeToolStripMenuItem.Enabled = !CanWriteCurrentPack;
        }

        #region Packed from Tree
        private void getPackedFilesFromBranch(List<PackedFile> packedFiles, TreeNodeCollection trunk, FileFilter filter = null)
        {
            foreach (TreeNode node in trunk)
            {
                if (node.Nodes.Count > 0)
                {
                    getPackedFilesFromBranch(packedFiles, node.Nodes, filter);
                }
                else if (filter == null || filter(node.Tag as PackedFile))
                {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
        }

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

        #region Open Packed
        private void openAsTextMenuItem_Click(object sender, EventArgs e) {
            List<PackedFile> packedFiles = new List<PackedFile>();
            packedFiles.Add(packTreeView.SelectedNode.Tag as PackedFile);
            openAsText(packedFiles[0]);
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e) {
            openExternal(packTreeView.SelectedNode.Tag as PackedFile, "openas");
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
                bool flag2 = packTreeView.SelectedNode != null;
                bool flag3 = flag2 && (packTreeView.SelectedNode.Nodes.Count == 0);
                extractSelectedToolStripMenuItem.Enabled = flag2;
                replaceFileToolStripMenuItem.Enabled = !currentPackFileIsReadOnly && flag3;
                renameToolStripMenuItem.Enabled = (!currentPackFileIsReadOnly && (flag2 || flag3)) && (packTreeView.SelectedNode != packTreeView.Nodes[0]);
            }
        }

        #region Tree Handler
        private void packTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            PackEntry entry = e.Node.Tag as PackEntry;
            if ((e.Label == null) || (e.Label == e.Node.Text) || (entry == null))
            {
                e.CancelEdit = true;
            }
            else
            {
                entry.Name = e.Label;
            }
        }

        private void packTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
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

        #region Save Pack
        private bool CanWriteCurrentPack {
            get {
                bool result = true;
                if (cAPacksAreReadOnlyToolStripMenuItem.Checked) {
                    switch (currentPackFile.Type) {
                        case PackType.Mod:                            // mod files can always be saved
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

        public ToolStripLabel StatusLabel
        {
            get
            {
                return packStatusLabel;
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs ev)
        {
            tryUpdate(true, currentPackFile == null ? null : currentPackFile.Filepath);
        }

        #region DB Management
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
                        TypeInfo targetInfo = DBTypeMap.Instance[key, maxVersion];
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

        private bool unknownDbFormat(PackedFile file) {
			bool result = file.FullPath.StartsWith ("db");
			string buffer;
			result &= !PackedFileDbCodec.CanDecode (file, out buffer);
			return result;
		}

        #endregion
        
        #region Options
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
  
        #region MyMod
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

