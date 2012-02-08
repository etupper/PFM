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
using System.Windows.Forms;

namespace PackFileManager
{

    public class PackFileManagerForm : Form
    {
        private ToolStripMenuItem aboutToolStripMenuItem;
        private FolderBrowserDialog addDirectoryFolderBrowserDialog;
        private ToolStripMenuItem addDirectoryToolStripMenuItem;
        private ToolStripMenuItem addFileToolStripMenuItem;
        private AtlasFileEditorControl atlasFileEditorControl;
        private ToolStripMenuItem bootToolStripMenuItem;
        private ToolStripMenuItem cAPacksAreReadOnlyToolStripMenuItem;
        private ToolStripMenuItem changePackTypeToolStripMenuItem;
        private FolderBrowserDialog choosePathAnchorFolderBrowserDialog;
        private IContainer components;
        private ToolStripMenuItem contentsToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem createReadMeToolStripMenuItem;
        private PackFile currentPackFile = null;
        private ToolStripMenuItem cutToolStripMenuItem;
        private DBFileEditorControl dbFileEditorControl;
        private ToolStripMenuItem deleteFileToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem exportFileListToolStripMenuItem;
        private ToolStripMenuItem extractAllToolStripMenuItem;
        private FolderBrowserDialog extractFolderBrowserDialog;
        private ToolStripMenuItem extractSelectedToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ImageViewerControl imageViewerControl;
        private ToolStripMenuItem indexToolStripMenuItem;
        private Label label1;
        private LocFileEditorControl locFileEditorControl;
        private MenuStrip menuStrip;
        private ToolStripMenuItem modToolStripMenuItem;
        private ToolStripMenuItem movieToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private bool nodeRenamed;
        private string nodeText = "";
        public OpenFileDialog openDBFileDialog;
        private bool openFileIsModified;
        private string openFilePath;
        private ToolStripMenuItem openFileToolStripMenuItem;
        private FileSystemWatcher openFileWatcher;
        private PackedFile openPackedFile;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripDropDownButton packActionDropDownButton;
        private ContextMenuStrip packActionMenuStrip;
        private ToolStripProgressBar packActionProgressBar;
        private ToolStrip packActionToolStrip;
        public OpenFileDialog packOpenFileDialog;
        private ToolStripStatusLabel packStatusLabel;
        public TreeView packTreeView;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripMenuItem patchToolStripMenuItem;
        private PictureBox pictureBox1;
        private ReadmeEditorControl readmeEditorControl;
        private ToolStripMenuItem redoToolStripMenuItem;
        private Label relativePathAnchorLabel;
        private ToolStripMenuItem releaseToolStripMenuItem;
        private ToolStripMenuItem renameToolStripMenuItem;
        private ToolStripMenuItem replaceFileToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private SaveFileDialog saveFileDialog;
        private ToolStripMenuItem saveToolStripMenuItem;
        private customMessageBox search;
        private ToolStripMenuItem searchFileToolStripMenuItem;
        private ToolStripMenuItem searchToolStripMenuItem;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip;
        private TextFileEditorControl textFileEditorControl;
        private ToolStripSeparator toolStripSeparator;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem updateToolStripMenuItem;
        private ToolStripMenuItem searchForUpdateToolStripMenuItem;
        private ToolStripMenuItem fromXsdFileToolStripMenuItem;
        private ToolStripMenuItem reloadToolStripMenuItem;
        private ToolStripMenuItem updateOnStartupToolStripMenuItem;
        private ToolStripMenuItem updateDBFilesToolStripMenuItem;
        private ToolStripMenuItem saveToDirectoryToolStripMenuItem;
        private ToolStripMenuItem updateCurrentToolStripMenuItem;
        private ToolStripMenuItem updateAllToolStripMenuItem;
        private ToolStripMenuItem openAsTextMenuItem;
        private ToolStripMenuItem exportUnknownToolStripMenuItem;
        private UnitVariantFileEditorControl unitVariantFileEditorControl;

        delegate bool FileFilter (PackedFile file);

        public PackFileManagerForm(string[] args)
        {
            this.InitializeComponent();

            if (Settings.Default.UpdateOnStartup)
            {
                tryUpdate(false);
            }

            string ShogunTotalWarDirectory = IOFunctions.GetShogunTotalWarDirectory();
            if (string.IsNullOrEmpty(ShogunTotalWarDirectory))
            {
                if ((args.Length != 1) || !File.Exists(args[0]))
                {
                    if (this.choosePathAnchorFolderBrowserDialog.ShowDialog() != DialogResult.OK)
                    {
                        throw new InvalidDataException("unable to determine path to \"Total War : Shogun 2\" directory");
                    }
                    this.extractFolderBrowserDialog.SelectedPath = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
                    this.relativePathAnchorLabel.Text = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
                }
                else
                {
                    this.choosePathAnchorFolderBrowserDialog.SelectedPath = Path.GetDirectoryName(args[0]);
                    this.extractFolderBrowserDialog.SelectedPath = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
                    this.relativePathAnchorLabel.Text = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
                }
            }
            else
            {
                this.choosePathAnchorFolderBrowserDialog.SelectedPath = ShogunTotalWarDirectory + @"\data";
                this.extractFolderBrowserDialog.SelectedPath = ShogunTotalWarDirectory + @"\data";
                this.relativePathAnchorLabel.Text = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
            }
            this.saveFileDialog.InitialDirectory = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
            this.addDirectoryFolderBrowserDialog.SelectedPath = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
            DBFileEditorControl control = new DBFileEditorControl {
                Dock = DockStyle.Fill
            };
            this.dbFileEditorControl = control;
            this.nodeRenamed = false;
            Text = string.Format("Pack File Manager {0} BETA (Total War - Shogun 2)", Application.ProductVersion);
            if (args.Length == 1)
            {
                if (!File.Exists(args[0]))
                {
                    throw new ArgumentException("path is not a file or path does not exist");
                }
                this.OpenExistingPackFile(args[0]);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new Form {
                Text = "About Pack File Manager " + Application.ProductVersion,
                Size = new Size(0x177, 0xe1),
                WindowState = FormWindowState.Normal,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };
            Label label = new Label {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.Format("v{0} Copyright 2009-2011 Distributed under the Simple Public License 2.0\r\n" +
                    "\r\nPack File Manager by Matt Chambers\r\n"+
                    "\r\nPack File Manager Update for NTW by erasmus777\r\n"+
                    "\r\nPack File Manager Update for TWS2 by Lord Maximus and Porphyr\r\n"+
                    "\r\nPack File Manager Update 1.7 for TWS2 by daniu\r\n" +
                    "\r\nThanks to the hard work of the people at twcenter.net.\r\n" +
                    "\r\nSpecial thanks to alpaca, just, ancientxx, Delphy, Scanian, iznagi11, barvaz, Mechanic, mac89, badger1815, husserlTW, The Vicar, and many others!", Application.ProductVersion)
            };
            form.Controls.Add(label);
            form.ShowDialog(this);
        }

        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.addDirectoryFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string[] strArray = Directory.GetFiles(this.addDirectoryFolderBrowserDialog.SelectedPath, "*.*", SearchOption.AllDirectories);
                FileAlreadyExistsDialog.DefaultAction ask = FileAlreadyExistsDialog.DefaultAction.Ask;
                foreach (string str in strArray)
                {
                    PackedFile file;
                    if (this.currentPackFile.TryGetValue(str, out file))
                    {
                        if (ask == FileAlreadyExistsDialog.DefaultAction.Ask)
                        {
                            FileAlreadyExistsDialog dialog = new FileAlreadyExistsDialog(str) {
                                CanRename = false
                            };
                            dialog.ShowDialog(this);
                            ask = dialog.NextAction;
                            bool flag = false;
                            switch (dialog.ChosenAction)
                            {
                                case FileAlreadyExistsDialog.ChoosableAction.Overwrite:
                                    file.Replace(str);
                                    break;

                                case FileAlreadyExistsDialog.ChoosableAction.Skip:
                                    goto Label_0153;

                                case FileAlreadyExistsDialog.ChoosableAction.Cancel:
                                    flag = true;
                                    break;
                            }
                            if (flag)
                            {
                                break;
                            }
                            goto Label_0153;
                        }
                        switch (ask)
                        {
                            case FileAlreadyExistsDialog.DefaultAction.Overwrite:
                                file.Replace(str);
                                goto Label_0153;

                            case FileAlreadyExistsDialog.DefaultAction.Skip:
                                goto Label_0153;

                            default:
                                goto Label_0153;
                        }
                    }
                    PackedFile file2 = this.currentPackFile.Add(str);
                    TreeNode node = this.addTreeViewNodeByPath(this.packTreeView.Nodes[0], file2);
                Label_0153:;
                }
            }
        }

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog addReplaceOpenFileDialog = new OpenFileDialog();
            addReplaceOpenFileDialog.Multiselect = true;
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    this.currentPackFile.AddRange(addReplaceOpenFileDialog.FileNames);
                    this.nodeRenamed = true;
                }
                catch (Exception x)
                {
                    MessageBox.Show(x.Message, "Problem, Sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.Refresh();
            }
        }

        /*
         * Adds a tree node for the given pack file.
         * If we don't have a description for that file, shows the entry in red.
         */
        private TreeNode addTreeViewNodeByPath(TreeNode parent, PackedFile file2) {
            string path = file2.Filepath.Replace('\\', '/');
            TreeNode node = addTreeViewNodeByPath(parent.Nodes, path);
            node.Tag = file2;
            string mouseover = "";
            try
            {
                if (file2.Filepath.StartsWith("db\\"))
                {
                    if (!canShow(file2, out mouseover))
                    {
                        node.Parent.ToolTipText = mouseover;
                        node.Parent.ForeColor = Color.Red;
                        node.ForeColor = Color.Red;
                    }
                    else if (headerVersionObsolete(file2))
                    {
                        node.Parent.BackColor = Color.Yellow;
                        node.BackColor = Color.Yellow;
                    }
                }
            }
            catch (Exception x) {
//                Console.WriteLine(x);
            }
            node.ToolTipText = mouseover;
            return node;
        }

        private TreeNode addTreeViewNodeByPath(TreeNodeCollection trunk, string path)
        {
            string[] strArray = path.Split("/".ToCharArray(), 2);
            TreeNode[] nodeArray = trunk.Find(strArray[0], false);
            if (nodeArray.Length == 0)
            {
                TreeNodeCollection nodes = trunk;
                TreeNode node = null;
                foreach (string str in path.Split("/".ToCharArray()))
                {
                    node = nodes.Add(str, str);
                    nodes = node.Nodes;
                }
                return node;
            }
            if (strArray.Length == 1)
            {
                throw new InvalidDataException("leaf node already in the tree");
            }
            if (nodeArray.Length != 1)
            {
                throw new InvalidDataException("branch node has non-unique key");
            }
            return this.addTreeViewNodeByPath(nodeArray[0].Nodes, strArray[1]);
        }

        private void cAPacksAreReadOnlyToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if (this.cAPacksAreReadOnlyToolStripMenuItem.CheckState == CheckState.Unchecked)
            {
                caFileEditAdvisory advisory = new caFileEditAdvisory();
                if (advisory.DialogResult == DialogResult.Yes)
                {
                    this.cAPacksAreReadOnlyToolStripMenuItem.CheckState = CheckState.Unchecked;
                }
                else
                {
                    this.cAPacksAreReadOnlyToolStripMenuItem.CheckState = CheckState.Checked;
                }
            }
        }

        private void createReadMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PackedFile packedFile = this.currentPackFile.AddData("readme.xml", new byte[0]);
            this.Refresh();
            this.openReadMe(packedFile);
        }

        private void currentPackFile_FinishedLoading(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private void currentPackFile_HeaderLoaded(object sender, EventArgs e)
        {
            this.packStatusLabel.Text = string.Format("Opening {0} (0 of {1} files loaded)", Path.GetFileName(this.currentPackFile.Filepath), this.currentPackFile.FileCount);
            this.packActionProgressBar.Minimum = 0;
            this.packActionProgressBar.Maximum = this.currentPackFile.FileCount;
            this.packActionProgressBar.Step = 10;
            this.packActionProgressBar.Value = 0;
            Application.DoEvents();
        }

        private void currentPackFile_Modified(object sender, EventArgs e)
        {
            this.refreshColors();
            this.refreshTitle();
        }

        private void currentPackFile_PackedFileLoaded(object sender, EventArgs e)
        {
            if (((this.currentPackFile.FileList.Count % 10) <= 0) || (this.currentPackFile.FileList.Count == this.currentPackFile.FileCount))
            {
                this.packStatusLabel.Text = string.Format("Opening {0} ({1} of {2} files loaded)", Path.GetFileName(this.currentPackFile.Filepath), this.currentPackFile.FileList.Count, this.currentPackFile.FileCount);
                this.packActionProgressBar.PerformStep();
                Application.DoEvents();
            }
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<PackedFile> packedFiles = new List<PackedFile>();
            if ((this.packTreeView.SelectedNode == this.packTreeView.Nodes[0]) || (this.packTreeView.SelectedNode.Nodes.Count > 0))
            {
                this.getPackedFilesFromBranch(packedFiles, this.packTreeView.SelectedNode.Nodes);
            }
            else
            {
                packedFiles.Add(this.packTreeView.SelectedNode.Tag as PackedFile);
            }
            foreach (PackedFile file in packedFiles)
            {
                file.Delete();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                Utilities.DisposeHandlers(this);
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void exportFileListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.saveFileDialog.FileName = Path.GetFileNameWithoutExtension(this.currentPackFile.Filepath) + ".pack-file-list.txt";
            if (this.saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(this.saveFileDialog.FileName))
                {
                    foreach (PackedFile file in this.currentPackFile.FileList)
                    {
                        writer.WriteLine(file.Filepath);
                    }
                }
            }
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<PackedFile> packedFiles = new List<PackedFile>();
            foreach (TreeNode node in this.packTreeView.Nodes)
            {
                if (node.Nodes.Count > 0)
                {
                    this.getPackedFilesFromBranch(packedFiles, node.Nodes);
                }
                else
                {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
            this.extractFiles(packedFiles);
        }

        private void extractFiles(List<PackedFile> packedFiles)
        {
            if (this.extractFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = this.extractFolderBrowserDialog.SelectedPath;
                FileAlreadyExistsDialog.DefaultAction ask = FileAlreadyExistsDialog.DefaultAction.Ask;
                this.packStatusLabel.Text = string.Format("Extracting file (0 of {0} files extracted, 0 skipped)", packedFiles.Count);
                this.packActionProgressBar.Visible = true;
                this.packActionProgressBar.Minimum = 0;
                this.packActionProgressBar.Maximum = packedFiles.Count;
                this.packActionProgressBar.Step = 1;
                this.packActionProgressBar.Value = 0;
                int num = 0;
                int num2 = 0;
                foreach (PackedFile file in packedFiles)
                {
                    string path = Path.Combine(selectedPath, file.Filepath);
                    if (File.Exists(path))
                    {
                        string str3;
                        if (ask == FileAlreadyExistsDialog.DefaultAction.Ask)
                        {
                            FileAlreadyExistsDialog dialog = new FileAlreadyExistsDialog(path);
                            dialog.ShowDialog(this);
                            ask = dialog.NextAction;
                            bool flag = false;
                            switch (dialog.ChosenAction)
                            {
                                case FileAlreadyExistsDialog.ChoosableAction.Skip:
                                {
                                    num2++;
                                    this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.Filepath, num, packedFiles.Count, num2 });
                                    this.packActionProgressBar.PerformStep();
                                    Application.DoEvents();
                                    continue;
                                }
                                case FileAlreadyExistsDialog.ChoosableAction.RenameExisting:
                                    str3 = path + ".bak";
                                    while (File.Exists(str3))
                                    {
                                        str3 = str3 + ".bak";
                                    }
                                    File.Move(path, str3);
                                    break;

                                case FileAlreadyExistsDialog.ChoosableAction.RenameNew:
                                    do
                                    {
                                        path = path + ".new";
                                    }
                                    while (File.Exists(path));
                                    break;

                                case FileAlreadyExistsDialog.ChoosableAction.Cancel:
                                    flag = true;
                                    break;
                            }
                            if (flag)
                            {
                                this.packStatusLabel.Text = "Extraction cancelled.";
                                this.packActionProgressBar.Visible = false;
                                return;
                            }
                            goto Label_031E;
                        }
                        switch (ask)
                        {
                            case FileAlreadyExistsDialog.DefaultAction.Overwrite:
                                goto Label_031E;

                            case FileAlreadyExistsDialog.DefaultAction.Skip:
                            {
                                num2++;
                                this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.Filepath, num, packedFiles.Count, num2 });
                                this.packActionProgressBar.PerformStep();
                                Application.DoEvents();
                                continue;
                            }
                            case FileAlreadyExistsDialog.DefaultAction.RenameExisting:
                                str3 = path + ".bak";
                                while (File.Exists(str3))
                                {
                                    str3 = str3 + ".bak";
                                }
                                File.Move(path, str3);
                                goto Label_031E;

                            case FileAlreadyExistsDialog.DefaultAction.RenameNew:
                                do
                                {
                                    path = path + ".new";
                                }
                                while (File.Exists(path));
                                goto Label_031E;

                            default:
                                goto Label_031E;
                        }
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                Label_031E:;
                    this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.Filepath, num, packedFiles.Count, num2 });
                    Application.DoEvents();
                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        stream.Write(file.Data, 0, (int) file.Size);
                    }
                    num++;
                    this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.Filepath, num, packedFiles.Count, num2 });
                    this.packActionProgressBar.PerformStep();
                    Application.DoEvents();
                }
            }
        }

        private void extractSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<PackedFile> packedFiles = new List<PackedFile>();
            if (this.packTreeView.SelectedNode.Nodes.Count > 0)
            {
                this.getPackedFilesFromBranch(packedFiles, this.packTreeView.SelectedNode.Nodes);
            }
            else
            {
                packedFiles.Add(this.packTreeView.SelectedNode.Tag as PackedFile);
            }
            this.extractFiles(packedFiles);
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            this.saveToolStripMenuItem.Enabled = !this.CurrentPackFileIsReadOnly && (((((this.currentPackFile != null) && this.currentPackFile.IsModified) || ((this.atlasFileEditorControl != null) && this.atlasFileEditorControl.dataChanged)) || ((this.locFileEditorControl != null) && this.locFileEditorControl.dataChanged)) || ((this.unitVariantFileEditorControl != null) && this.unitVariantFileEditorControl.dataChanged));
            this.createReadMeToolStripMenuItem.Enabled = !this.CurrentPackFileIsReadOnly;
        }

        private void findChild(TreeNode tnChild)
        {
            foreach (TreeNode node in tnChild.Nodes)
            {
                this.search.tnList.Add(node);
                this.findChild(node);
            }
        }

        private void getPackedFilesFromBranch(List<PackedFile> packedFiles, TreeNodeCollection trunk, FileFilter filter = null)
        {
            foreach (TreeNode node in trunk)
            {
                if (node.Nodes.Count > 0)
                {
                    this.getPackedFilesFromBranch(packedFiles, node.Nodes, filter);
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
            this.getTreeViewBranch(nodes, trunk);
            return nodes;
        }

        private void getTreeViewBranch(List<TreeNode> nodes, TreeNodeCollection trunk)
        {
            foreach (TreeNode node in trunk)
            {
                nodes.Add(node);
                this.getTreeViewBranch(nodes, node.Nodes);
            }
        }

        private DialogResult handlePackFileChangesWithUserInput()
        {
            if ((this.currentPackFile != null) && this.currentPackFile.IsModified)
            {
                switch (MessageBox.Show("You modified the pack file. Do you want to save your changes?", "Save Changes?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button3))
                {
                    case DialogResult.Yes:
                        this.saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        if (!this.currentPackFile.IsModified)
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

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.packTreeView = new System.Windows.Forms.TreeView();
            this.packActionMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exportFileListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openAsTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateDBFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateCurrentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.changePackTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.releaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.movieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packActionDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.packOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.relativePathAnchorLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.packActionToolStrip = new System.Windows.Forms.ToolStrip();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.cAPacksAreReadOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createReadMeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.updateOnStartupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fromXsdFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.indexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.packStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.packActionProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.extractFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.choosePathAnchorFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.addDirectoryFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.openDBFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.exportUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packActionMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.packActionToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // packTreeView
            // 
            this.packTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.packTreeView.ContextMenuStrip = this.packActionMenuStrip;
            this.packTreeView.ForeColor = System.Drawing.SystemColors.WindowText;
            this.packTreeView.HideSelection = false;
            this.packTreeView.Indent = 19;
            this.packTreeView.Location = new System.Drawing.Point(0, 52);
            this.packTreeView.Name = "packTreeView";
            this.packTreeView.Size = new System.Drawing.Size(199, 548);
            this.packTreeView.TabIndex = 2;
            this.packTreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.packTreeView_AfterLabelEdit);
            this.packTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.packTreeView_AfterSelect);
            this.packTreeView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.packTreeView_MouseDoubleClick);
            this.packTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.packTreeView_MouseDown);
            this.packTreeView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.packTreeView_PreviewKeyDown);
            // 
            // packActionMenuStrip
            // 
            this.packActionMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportFileListToolStripMenuItem,
            this.extractAllToolStripMenuItem,
            this.extractSelectedToolStripMenuItem,
            this.exportUnknownToolStripMenuItem,
            this.addFileToolStripMenuItem,
            this.addDirectoryToolStripMenuItem,
            this.searchFileToolStripMenuItem,
            this.openFileToolStripMenuItem,
            this.openAsTextMenuItem,
            this.replaceFileToolStripMenuItem,
            this.deleteFileToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.updateDBFilesToolStripMenuItem,
            this.toolStripSeparator1,
            this.changePackTypeToolStripMenuItem});
            this.packActionMenuStrip.Name = "packActionMenuStrip";
            this.packActionMenuStrip.OwnerItem = this.packActionDropDownButton;
            this.packActionMenuStrip.Size = new System.Drawing.Size(211, 340);
            this.packActionMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.packActionMenuStrip_Opening);
            // 
            // exportFileListToolStripMenuItem
            // 
            this.exportFileListToolStripMenuItem.Name = "exportFileListToolStripMenuItem";
            this.exportFileListToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.exportFileListToolStripMenuItem.Text = "Export File &List...";
            this.exportFileListToolStripMenuItem.Click += new System.EventHandler(this.exportFileListToolStripMenuItem_Click);
            // 
            // extractAllToolStripMenuItem
            // 
            this.extractAllToolStripMenuItem.Name = "extractAllToolStripMenuItem";
            this.extractAllToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.extractAllToolStripMenuItem.Text = "Extract &All...";
            this.extractAllToolStripMenuItem.Click += new System.EventHandler(this.extractAllToolStripMenuItem_Click);
            // 
            // extractSelectedToolStripMenuItem
            // 
            this.extractSelectedToolStripMenuItem.Name = "extractSelectedToolStripMenuItem";
            this.extractSelectedToolStripMenuItem.ShortcutKeyDisplayString = "Ctl+X";
            this.extractSelectedToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.extractSelectedToolStripMenuItem.Text = "Extract &Selected...";
            this.extractSelectedToolStripMenuItem.Click += new System.EventHandler(this.extractSelectedToolStripMenuItem_Click);
            // 
            // addFileToolStripMenuItem
            // 
            this.addFileToolStripMenuItem.Name = "addFileToolStripMenuItem";
            this.addFileToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
            this.addFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.addFileToolStripMenuItem.Text = "Add &File(s)...";
            this.addFileToolStripMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // addDirectoryToolStripMenuItem
            // 
            this.addDirectoryToolStripMenuItem.Name = "addDirectoryToolStripMenuItem";
            this.addDirectoryToolStripMenuItem.ShortcutKeyDisplayString = "Shift+Ins";
            this.addDirectoryToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.addDirectoryToolStripMenuItem.Text = "Add &Directory...";
            this.addDirectoryToolStripMenuItem.Click += new System.EventHandler(this.addDirectoryToolStripMenuItem_Click);
            // 
            // searchFileToolStripMenuItem
            // 
            this.searchFileToolStripMenuItem.Name = "searchFileToolStripMenuItem";
            this.searchFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.searchFileToolStripMenuItem.Text = "Search Files...";
            this.searchFileToolStripMenuItem.Click += new System.EventHandler(this.searchFileToolStripMenuItem_Click);
            // 
            // openFileToolStripMenuItem
            // 
            this.openFileToolStripMenuItem.Name = "openFileToolStripMenuItem";
            this.openFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.openFileToolStripMenuItem.Text = "Open File...";
            this.openFileToolStripMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // openAsTextMenuItem
            // 
            this.openAsTextMenuItem.Name = "openAsTextMenuItem";
            this.openAsTextMenuItem.Size = new System.Drawing.Size(210, 22);
            this.openAsTextMenuItem.Text = "Open as Text";
            this.openAsTextMenuItem.Click += new System.EventHandler(this.openAsText_click);
            // 
            // replaceFileToolStripMenuItem
            // 
            this.replaceFileToolStripMenuItem.Name = "replaceFileToolStripMenuItem";
            this.replaceFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.replaceFileToolStripMenuItem.Text = "&Replace File...";
            this.replaceFileToolStripMenuItem.Click += new System.EventHandler(this.replaceFileToolStripMenuItem_Click);
            // 
            // deleteFileToolStripMenuItem
            // 
            this.deleteFileToolStripMenuItem.Name = "deleteFileToolStripMenuItem";
            this.deleteFileToolStripMenuItem.ShortcutKeyDisplayString = "Del";
            this.deleteFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.deleteFileToolStripMenuItem.Text = "Delete File";
            this.deleteFileToolStripMenuItem.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            this.renameToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.renameToolStripMenuItem.Text = "Rename";
            this.renameToolStripMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // updateDBFilesToolStripMenuItem
            // 
            this.updateDBFilesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.updateCurrentToolStripMenuItem,
            this.updateAllToolStripMenuItem});
            this.updateDBFilesToolStripMenuItem.Name = "updateDBFilesToolStripMenuItem";
            this.updateDBFilesToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(207, 6);
            // 
            // changePackTypeToolStripMenuItem
            // 
            this.changePackTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bootToolStripMenuItem,
            this.releaseToolStripMenuItem,
            this.patchToolStripMenuItem,
            this.modToolStripMenuItem,
            this.movieToolStripMenuItem});
            this.changePackTypeToolStripMenuItem.Name = "changePackTypeToolStripMenuItem";
            this.changePackTypeToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.changePackTypeToolStripMenuItem.Text = "Change Pack &Type";
            // 
            // bootToolStripMenuItem
            // 
            this.bootToolStripMenuItem.CheckOnClick = true;
            this.bootToolStripMenuItem.Name = "bootToolStripMenuItem";
            this.bootToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.bootToolStripMenuItem.Text = "Boot";
            this.bootToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // releaseToolStripMenuItem
            // 
            this.releaseToolStripMenuItem.CheckOnClick = true;
            this.releaseToolStripMenuItem.Name = "releaseToolStripMenuItem";
            this.releaseToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.releaseToolStripMenuItem.Text = "Release";
            this.releaseToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // patchToolStripMenuItem
            // 
            this.patchToolStripMenuItem.CheckOnClick = true;
            this.patchToolStripMenuItem.Name = "patchToolStripMenuItem";
            this.patchToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.patchToolStripMenuItem.Text = "Patch";
            this.patchToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // modToolStripMenuItem
            // 
            this.modToolStripMenuItem.Name = "modToolStripMenuItem";
            this.modToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.modToolStripMenuItem.Text = "Mod";
            this.modToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // movieToolStripMenuItem
            // 
            this.movieToolStripMenuItem.CheckOnClick = true;
            this.movieToolStripMenuItem.Name = "movieToolStripMenuItem";
            this.movieToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.movieToolStripMenuItem.Text = "Movie";
            this.movieToolStripMenuItem.Click += new System.EventHandler(this.packTypeToolStripMenuItem_Click);
            // 
            // packActionDropDownButton
            // 
            this.packActionDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.packActionDropDownButton.DropDown = this.packActionMenuStrip;
            this.packActionDropDownButton.Enabled = false;
            this.packActionDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.packActionDropDownButton.Margin = new System.Windows.Forms.Padding(5, 1, 250, 1);
            this.packActionDropDownButton.Name = "packActionDropDownButton";
            this.packActionDropDownButton.Size = new System.Drawing.Size(135, 23);
            this.packActionDropDownButton.Text = "Choose a Pack Action";
            // 
            // packOpenFileDialog
            // 
            this.packOpenFileDialog.Filter = "Package File|*.pack|Any File|*.*";
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
            this.splitContainer1.Panel1.Controls.Add(this.relativePathAnchorLabel);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.packActionToolStrip);
            this.splitContainer1.Panel1.Controls.Add(this.packTreeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pictureBox1);
            this.splitContainer1.Size = new System.Drawing.Size(909, 603);
            this.splitContainer1.SplitterDistance = 202;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 9;
            // 
            // relativePathAnchorLabel
            // 
            this.relativePathAnchorLabel.AutoSize = true;
            this.relativePathAnchorLabel.Location = new System.Drawing.Point(141, 33);
            this.relativePathAnchorLabel.Name = "relativePathAnchorLabel";
            this.relativePathAnchorLabel.Size = new System.Drawing.Size(0, 13);
            this.relativePathAnchorLabel.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Pack paths are relative to:";
            // 
            // packActionToolStrip
            // 
            this.packActionToolStrip.CanOverflow = false;
            this.packActionToolStrip.GripMargin = new System.Windows.Forms.Padding(0);
            this.packActionToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.packActionToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.packActionDropDownButton});
            this.packActionToolStrip.Location = new System.Drawing.Point(0, 0);
            this.packActionToolStrip.Name = "packActionToolStrip";
            this.packActionToolStrip.Size = new System.Drawing.Size(198, 25);
            this.packActionToolStrip.TabIndex = 1;
            this.packActionToolStrip.Text = "toolStrip1";
            this.packActionToolStrip.Resize += new System.EventHandler(this.packActionToolStrip_Resize);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(559, 600);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.updateToolStripMenuItem,
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
            this.toolStripSeparator,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator2,
            this.cAPacksAreReadOnlyToolStripMenuItem,
            this.createReadMeToolStripMenuItem,
            this.toolStripSeparator7,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpening);
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(198, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(198, 6);
            // 
            // cAPacksAreReadOnlyToolStripMenuItem
            // 
            this.cAPacksAreReadOnlyToolStripMenuItem.Checked = true;
            this.cAPacksAreReadOnlyToolStripMenuItem.CheckOnClick = true;
            this.cAPacksAreReadOnlyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cAPacksAreReadOnlyToolStripMenuItem.Name = "cAPacksAreReadOnlyToolStripMenuItem";
            this.cAPacksAreReadOnlyToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.cAPacksAreReadOnlyToolStripMenuItem.Text = "CA Packs Are Read Only";
            this.cAPacksAreReadOnlyToolStripMenuItem.ToolTipText = "If checked, the original pack files for the game can be viewed but not edited.";
            this.cAPacksAreReadOnlyToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.cAPacksAreReadOnlyToolStripMenuItem_CheckStateChanged);
            // 
            // createReadMeToolStripMenuItem
            // 
            this.createReadMeToolStripMenuItem.Enabled = false;
            this.createReadMeToolStripMenuItem.Name = "createReadMeToolStripMenuItem";
            this.createReadMeToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.createReadMeToolStripMenuItem.Text = "Create ReadMe";
            this.createReadMeToolStripMenuItem.Click += new System.EventHandler(this.createReadMeToolStripMenuItem_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(198, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
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
            this.updateOnStartupToolStripMenuItem,
            this.fromXsdFileToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.saveToDirectoryToolStripMenuItem});
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
            // updateOnStartupToolStripMenuItem
            // 
            this.updateOnStartupToolStripMenuItem.CheckOnClick = true;
            this.updateOnStartupToolStripMenuItem.Name = "updateOnStartupToolStripMenuItem";
            this.updateOnStartupToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.updateOnStartupToolStripMenuItem.Text = "Update on Startup";
            this.updateOnStartupToolStripMenuItem.Click += new System.EventHandler(this.updateOnStartupToolStripMenuItem_Click);
            // 
            // fromXsdFileToolStripMenuItem
            // 
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
            // 
            // saveToDirectoryToolStripMenuItem
            // 
            this.saveToDirectoryToolStripMenuItem.Name = "saveToDirectoryToolStripMenuItem";
            this.saveToDirectoryToolStripMenuItem.Size = new System.Drawing.Size(221, 22);
            this.saveToDirectoryToolStripMenuItem.Text = "Save to Directory";
            this.saveToDirectoryToolStripMenuItem.Click += new System.EventHandler(this.saveToDirectoryToolStripMenuItem_Click);
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
            // extractFolderBrowserDialog
            // 
            this.extractFolderBrowserDialog.Description = "Extract to what folder?";
            // 
            // choosePathAnchorFolderBrowserDialog
            // 
            this.choosePathAnchorFolderBrowserDialog.Description = "Make packed files relative to which directory?";
            // 
            // addDirectoryFolderBrowserDialog
            // 
            this.addDirectoryFolderBrowserDialog.Description = "Add which directory?";
            // 
            // openDBFileDialog
            // 
            this.openDBFileDialog.Filter = "Text CSV|*.txt|Any File|*.*";
            // 
            // exportUnknownToolStripMenuItem
            // 
            this.exportUnknownToolStripMenuItem.Name = "exportUnknownToolStripMenuItem";
            this.exportUnknownToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.exportUnknownToolStripMenuItem.Text = "Export Unknown...";
            this.exportUnknownToolStripMenuItem.Click += new System.EventHandler(this.exportUnknownToolStripMenuItem_Click);
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
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.packActionToolStrip.ResumeLayout(false);
            this.packActionToolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((this.handlePackFileChangesWithUserInput() != DialogResult.Cancel) && (this.choosePathAnchorFolderBrowserDialog.ShowDialog() == DialogResult.OK))
            {
                this.currentPackFile = new PackFile(Path.Combine(this.choosePathAnchorFolderBrowserDialog.SelectedPath, "Untitled.pack"));
                this.relativePathAnchorLabel.Text = this.currentPackFile.RelativePathAnchor;
                this.Refresh();
                this.currentPackFile.Modified += new EventHandler(this.currentPackFile_Modified);
            }
        }

        private void OpenExistingPackFile(string filepath)
        {
            try
            {
                this.currentPackFile = new PackFile(Path.GetDirectoryName(filepath));
                this.currentPackFile.HeaderLoaded += new EventHandler(this.currentPackFile_HeaderLoaded);
                this.currentPackFile.PackedFileLoaded += new EventHandler(this.currentPackFile_PackedFileLoaded);
                this.currentPackFile.Open(filepath);
                this.currentPackFile.HeaderLoaded -= new EventHandler(this.currentPackFile_HeaderLoaded);
                this.currentPackFile.PackedFileLoaded -= new EventHandler(this.currentPackFile_PackedFileLoaded);
                this.Refresh();
                this.currentPackFile.Modified += new EventHandler(this.currentPackFile_Modified);
                // DBReferenceMap.Instance.validateReferences(Path.GetDirectoryName(Application.ExecutablePath), currentPackFile);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        public void openExternal(PackedFile packedFile, string verb)
        {
            if (packedFile == null)
            {
                return;
            }
            this.openPackedFile = packedFile;
            this.openFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(packedFile.Filepath));
            if (!File.Exists(this.openFilePath))
            {
                using (FileStream stream = new FileStream(this.openFilePath, FileMode.Create))
                {
                    stream.Write(packedFile.Data, 0, (int)packedFile.Size);
                    stream.Close();
                }
            }
            if (verb == "openimage")
            {
                ImageViewerControl control = new ImageViewerControl {
                    Dock = DockStyle.Fill
                };
                this.imageViewerControl = control;
                this.splitContainer1.Panel2.Controls.Add(this.imageViewerControl);
                this.imageViewerControl.SetImage(this.openFilePath);
            }
            else
            {
                this.openWith(this.openFilePath, verb);
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.openExternal(this.packTreeView.SelectedNode.Tag as PackedFile, "openas");
        }

        private void openFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (this.openFileWatcher != null)
            {
                this.openFileWatcher.EnableRaisingEvents = false;
                this.openFileWatcher = null;
                this.openFileIsModified = true;
            }
        }

        private void OpenPackedFile(object tag)
        {
            PackedFile packedFile = tag as PackedFile;
            if (packedFile.Filepath == "readme.xml")
            {
                this.openReadMe(packedFile);
            }
            else if (packedFile.Filepath.EndsWith(".loc"))
            {
                this.locFileEditorControl = new LocFileEditorControl(packedFile);
                this.locFileEditorControl.Dock = DockStyle.Fill;
                this.splitContainer1.Panel2.Controls.Add(this.locFileEditorControl);
            }
            else if (packedFile.Filepath.Contains(".tga") || packedFile.Filepath.Contains(".dds") || packedFile.Filepath.Contains(".png") || packedFile.Filepath.Contains(".jpg") || packedFile.Filepath.Contains(".bmp") || packedFile.Filepath.Contains(".psd"))
            {
                this.openExternal(packedFile, "openimage");
            }
            else if (packedFile.Filepath.EndsWith(".atlas"))
            {
                AtlasFileEditorControl control = new AtlasFileEditorControl(packedFile) {
                    Dock = DockStyle.Fill
                };
                this.atlasFileEditorControl = control;
                this.splitContainer1.Panel2.Controls.Add(this.atlasFileEditorControl);
            }
            else if (packedFile.Filepath.EndsWith(".unit_variant"))
            {
                UnitVariantFileEditorControl control2 = new UnitVariantFileEditorControl(packedFile) {
                    Dock = DockStyle.Fill
                };
                this.unitVariantFileEditorControl = control2;
                this.splitContainer1.Panel2.Controls.Add(this.unitVariantFileEditorControl);
            }
            else if (packedFile.Filepath.Contains(".rigid"))
            {
                this.viewModel(packedFile);
            }
            else if (isTextFileType(packedFile))
            {
                openAsText(packedFile);
            }
            else if (packedFile.Filepath.Contains(@"db\"))
            {
                try
                {
                    this.dbFileEditorControl.Open(packedFile, currentPackFile);
                    this.splitContainer1.Panel2.Controls.Add(this.dbFileEditorControl);
                }
                catch (FileNotFoundException exception)
                {
                    MessageBox.Show(exception.Message, "DB Type not found", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
                catch (Exception x) {
                    MessageBox.Show(x.Message + "\n" + x.StackTrace, "Problem, sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void openAsText(PackedFile packedFile) {
            TextFileEditorControl control3 = new TextFileEditorControl(packedFile) {
                Dock = DockStyle.Fill
            };
            this.textFileEditorControl = control3;
            this.splitContainer1.Panel2.Controls.Add(this.textFileEditorControl);
        }

        private static bool isTextFileType(PackedFile file) {
            string[] extensions = {
                                      "txt", "lua", "csv", "fx", "fx_fragment", "h", "battle_script", "xml", 
                                      "tai", "xml.rigging", "placement", "hlsl"

                                  };
            bool result = false;
            foreach (string ext in extensions) {
                if (file.Filepath.EndsWith(ext)) {
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
                if (!(packedFile.Action is PackedFile.DeleteFilePackAction))
                {
                    this.readmeEditorControl = new ReadmeEditorControl();
                    this.readmeEditorControl.Dock = DockStyle.Fill;
                    this.readmeEditorControl.setPackedFile(packedFile);
                    this.splitContainer1.Panel2.Controls.Add(this.readmeEditorControl);
                }
            }
            catch (ConstraintException)
            {
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((this.handlePackFileChangesWithUserInput() != DialogResult.Cancel) && (this.packOpenFileDialog.ShowDialog() == DialogResult.OK))
            {
                this.OpenExistingPackFile(this.packOpenFileDialog.FileName);
            }
        }

        private void openWith(string file, string verb)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(file) {
                ErrorDialog = true
            };
            if (startInfo.Verbs.Length == 0)
            {
                startInfo.Verb = "openas";
            }
            else
            {
                startInfo.Verb = verb;
            }
            Process.Start(startInfo);
            this.openFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file));
            this.openFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.openFileWatcher.Changed += new FileSystemEventHandler(this.openFileWatcher_Changed);
            this.openFileWatcher.EnableRaisingEvents = true;
            this.openFileIsModified = false;
            base.Activated += new EventHandler(this.PackFileManagerForm_GotFocus);
        }

        private void packActionMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (this.currentPackFile == null)
            {
                e.Cancel = true;
            }
            else
            {
                bool currentPackFileIsReadOnly = this.CurrentPackFileIsReadOnly;
                this.addDirectoryToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                this.addFileToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                this.deleteFileToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                this.changePackTypeToolStripMenuItem.Enabled = !currentPackFileIsReadOnly;
                bool flag2 = this.packTreeView.SelectedNode != null;
                bool flag3 = flag2 && (this.packTreeView.SelectedNode.Nodes.Count == 0);
                this.extractSelectedToolStripMenuItem.Enabled = flag2;
                this.replaceFileToolStripMenuItem.Enabled = !currentPackFileIsReadOnly && flag3;
                this.renameToolStripMenuItem.Enabled = (!currentPackFileIsReadOnly && (flag2 || flag3)) && (this.packTreeView.SelectedNode != this.packTreeView.Nodes[0]);
            }
        }

        private void packActionToolStrip_Resize(object sender, EventArgs e)
        {
            Padding margin = this.packActionDropDownButton.Margin;
            margin.Left = (this.packActionToolStrip.Width - this.packActionDropDownButton.Width) / 2;
            this.packActionDropDownButton.Margin = margin;
        }

        private void PackFileManagerForm_Activated(object sender, EventArgs e)
        {
            if ((base.OwnedForms.Length > 0) && (this.search != null))
            {
                this.packTreeView.SelectedNode = this.search.nextNode;
            }
        }

        private void PackFileManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((((e.CloseReason != CloseReason.WindowsShutDown) && (e.CloseReason != CloseReason.TaskManagerClosing)) && (e.CloseReason != CloseReason.ApplicationExitCall)) && (this.handlePackFileChangesWithUserInput() == DialogResult.Cancel))
            {
                e.Cancel = true;
            }
        }

        private void PackFileManagerForm_GotFocus(object sender, EventArgs e)
        {
            base.Activated -= new EventHandler(this.PackFileManagerForm_GotFocus);
            if (this.openFileIsModified)
            {
                this.openFileIsModified = false;
                if (MessageBox.Show("Changes were made to the extracted file. Do you want to replace the packed file with the extracted file?", "Save changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.openPackedFile.ReplaceData(File.ReadAllBytes(this.openFilePath));
                }
            }
            while (File.Exists(this.openFilePath))
            {
                try
                {
                    File.Delete(this.openFilePath);
                }
                catch (IOException)
                {
                    if (MessageBox.Show("Unable to delete the temporary file; is it still in use by the external editor?\r\n\r\nClick Retry to try deleting it again or Cancel to leave it in the temporary directory.", "Temporary file in use", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                    {
                        break;
                    }
                }
            }
        }

        private void PackFileManagerForm_Load(object sender, EventArgs e)
        {
            base.TopMost = true;
        }

        private void PackFileManagerForm_Shown(object sender, EventArgs e)
        {
            base.TopMost = false;
        }

        private void packTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if ((e.Label == null) || (e.Label == e.Node.Text))
            {
                e.CancelEdit = true;
            }
            else
            {
                PackedFile tag;
                if (e.Node.Nodes.Count > 0)
                {
                    this.nodeRenamed = true;
                    List<TreeNode> list = this.getTreeViewBranch(e.Node.Nodes);
                    foreach (TreeNode node in list)
                    {
                        if (node.Tag is PackedFile)
                        {
                            tag = node.Tag as PackedFile;
                            StringBuilder builder = new StringBuilder();
                            TreeNode parent = node.Parent;
                            while (parent != e.Node)
                            {
                                builder.Insert(0, parent.Text + '/');
                                parent = parent.Parent;
                            }
                            builder.Insert(0, e.Label + '/');
                            for (parent = parent.Parent; parent != this.packTreeView.Nodes[0]; parent = parent.Parent)
                            {
                                builder.Insert(0, parent.Text + '/');
                            }
                            builder.Append(Path.GetFileName(tag.Filepath));
                            tag.Rename(builder.ToString());
                        }
                    }
                }
                else
                {
                    tag = e.Node.Tag as PackedFile;
                    tag.Rename(Path.GetDirectoryName(tag.Filepath) + '/' + e.Label);
                }
            }
        }

        private void packTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this.splitContainer1.Panel2.Controls.Contains(this.locFileEditorControl))
            {
                this.locFileEditorControl.updatePackedFile();
            }
            else if (this.splitContainer1.Panel2.Controls.Contains(this.readmeEditorControl))
            {
                this.readmeEditorControl.updatePackedFile();
            }
            else if (this.splitContainer1.Panel2.Controls.Contains(this.atlasFileEditorControl))
            {
                this.atlasFileEditorControl.updatePackedFile();
            }
            else if (this.splitContainer1.Panel2.Controls.Contains(this.unitVariantFileEditorControl))
            {
                this.unitVariantFileEditorControl.updatePackedFile();
            }
            else if (this.splitContainer1.Panel2.Controls.Contains(this.imageViewerControl))
            {
                this.imageViewerControl.CloseImageViewerControl();
            }
            else if (this.splitContainer1.Panel2.Controls.Contains(this.textFileEditorControl))
            {
                this.textFileEditorControl.CloseTextFileEditorControl();
            }
            this.splitContainer1.Panel2.Controls.Clear();
            this.refreshStatusLabel();
            if (this.packTreeView.SelectedNode != null)
            {
                this.packStatusLabel.Text = this.packStatusLabel.Text + " Viewing: " + this.packTreeView.SelectedNode.Text;
                this.packTreeView.LabelEdit = this.packTreeView.SelectedNode != this.packTreeView.Nodes[0];
                if (this.packTreeView.SelectedNode.Tag is PackedFile)
                {
                    this.OpenPackedFile(this.packTreeView.SelectedNode.Tag);
                }
                this.nodeText = this.packTreeView.SelectedNode.Text;
            }
        }

        private void packTreeView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeNode nodeAt = this.packTreeView.GetNodeAt(e.Location);
            if ((nodeAt != null) && (e.Button == MouseButtons.Right))
            {
                this.packTreeView.SelectedNode = nodeAt;
            }
            if ((this.packTreeView.SelectedNode != null) && (this.packTreeView.SelectedNode.Tag != null))
            {
                this.openExternal(this.packTreeView.SelectedNode.Tag as PackedFile, "open");
            }
        }

        private void packTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode nodeAt = this.packTreeView.GetNodeAt(e.Location);
            if ((nodeAt != null) && (e.Button == MouseButtons.Right))
            {
                this.packTreeView.SelectedNode = nodeAt;
            }
        }

        private void packTreeView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Insert:
                    if (!e.Shift)
                    {
                        this.addFileToolStripMenuItem_Click(this, EventArgs.Empty);
                        this.nodeRenamed = true;
                        break;
                    }
                    this.addDirectoryToolStripMenuItem_Click(this, EventArgs.Empty);
                    break;

                case Keys.Delete:
                    if (this.packTreeView.SelectedNode != null)
                    {
                        this.deleteFileToolStripMenuItem_Click(this, EventArgs.Empty);
                    }
                    break;

                case Keys.X:
                    if (e.Control)
                    {
                        this.extractSelectedToolStripMenuItem_Click(this, EventArgs.Empty);
                    }
                    break;
            }
        }

        private void packTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.bootToolStripMenuItem.Checked = object.ReferenceEquals(sender, this.bootToolStripMenuItem);
            this.releaseToolStripMenuItem.Checked = object.ReferenceEquals(sender, this.releaseToolStripMenuItem);
            this.patchToolStripMenuItem.Checked = object.ReferenceEquals(sender, this.patchToolStripMenuItem);
            this.movieToolStripMenuItem.Checked = object.ReferenceEquals(sender, this.movieToolStripMenuItem);
            this.modToolStripMenuItem.Checked = object.ReferenceEquals(sender, this.modToolStripMenuItem);
            if (this.bootToolStripMenuItem.Checked)
            {
                this.currentPackFile.Type = PackType.Boot;
            }
            else if (this.releaseToolStripMenuItem.Checked)
            {
                this.currentPackFile.Type = PackType.Release;
            }
            else if (this.patchToolStripMenuItem.Checked)
            {
                this.currentPackFile.Type = PackType.Patch;
            }
            else if (this.movieToolStripMenuItem.Checked)
            {
                this.currentPackFile.Type = PackType.Movie;
            }
            else if (this.modToolStripMenuItem.Checked)
            {
                this.currentPackFile.Type = PackType.Mod;
            }
        }

        public override void Refresh()
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            foreach (TreeNode node in this.getTreeViewBranch(this.packTreeView.Nodes))
            {
                dictionary[node.Text] = node.IsExpanded;
            }
            string str = (this.packTreeView.SelectedNode != null) ? this.packTreeView.SelectedNode.FullPath : "";
            this.packTreeView.Nodes.Clear();
            TreeNode node2 = this.packTreeView.Nodes.Add(Path.GetFileName(this.currentPackFile.Filepath));
            node2.Expand();
            foreach (PackedFile file in this.currentPackFile.FileList)
            {
                this.addTreeViewNodeByPath(node2, file);
            }
            foreach (TreeNode node in this.getTreeViewBranch(this.packTreeView.Nodes))
            {
                if (!(!dictionary.ContainsKey(node.Text) ? true : !dictionary[node.Text]))
                {
                    node.Expand();
                }
                if (node.FullPath == str)
                {
                    this.packTreeView.SelectedNode = node;
                }
            }
            this.packActionDropDownButton.Enabled = true;
            this.saveToolStripMenuItem.Enabled = true;
            this.saveAsToolStripMenuItem.Enabled = true;
            this.createReadMeToolStripMenuItem.Enabled = true;
            this.bootToolStripMenuItem.Checked = this.currentPackFile.Type == PackType.Boot;
            this.releaseToolStripMenuItem.Checked = this.currentPackFile.Type == PackType.Release;
            this.patchToolStripMenuItem.Checked = this.currentPackFile.Type == PackType.Patch;
            this.movieToolStripMenuItem.Checked = this.currentPackFile.Type == PackType.Movie;
            this.modToolStripMenuItem.Checked = this.currentPackFile.Type == PackType.Mod;
            this.refreshTitle();
            this.refreshColors();
            this.packTreeView_AfterSelect(this, new TreeViewEventArgs(this.packTreeView.SelectedNode));
            base.Refresh();
        }

        private void refreshColors()
        {
            foreach (TreeNode node in this.getTreeViewBranch(this.packTreeView.Nodes))
            {
                if ((node.Tag is PackedFile) && ((node.Tag as PackedFile).Action != null))
                {
                    PackedFile tag = node.Tag as PackedFile;
                    node.NodeFont = new Font(this.packTreeView.Font, FontStyle.Italic);
                    if (tag.Action is PackedFile.AddFilePackAction)
                    {
                        node.ForeColor = Color.Green;
                    }
                    else if (tag.Action is PackedFile.RenamePackAction)
                    {
                        node.ForeColor = Color.LimeGreen;
                    }
                    else if ((tag.Action is PackedFile.ReplaceFilePackAction) || (tag.Action is PackedFile.ReplaceDataPackAction))
                    {
                        node.ForeColor = Color.Red;
                    }
                    else if (tag.Action is PackedFile.DeleteFilePackAction)
                    {
                        node.ForeColor = Color.LightGray;
                    }
                }
            }
        }

        private void refreshStatusLabel()
        {
            this.packStatusLabel.Text = string.Format("Opening {0} ({1} of {1} files loaded)", Path.GetFileName(this.currentPackFile.Filepath), this.currentPackFile.FileCount);
        }

        private void refreshTitle()
        {
            this.Text = Path.GetFileName(this.currentPackFile.Filepath);
            if (this.currentPackFile.IsModified)
            {
                this.Text = this.Text + " (modified)";
            }
            this.Text = this.Text + string.Format(" - Pack File Manager {0} BETA (Total War - Shogun 2)", Application.ProductVersion);
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.nodeRenamed)
            {
                MessageBox.Show("Please save to continue.");
            }
            else
            {
                this.packTreeView.SelectedNode.BeginEdit();
                this.nodeRenamed = true;
            }
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog addReplaceOpenFileDialog = new OpenFileDialog();
            addReplaceOpenFileDialog.Multiselect = false;
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                PackedFile tag = this.packTreeView.SelectedNode.Tag as PackedFile;
                this.currentPackFile.Replace(tag, addReplaceOpenFileDialog.FileName);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.locFileEditorControl != null)
            {
                this.locFileEditorControl.updatePackedFile();
            }
            if (this.atlasFileEditorControl != null)
            {
                this.atlasFileEditorControl.updatePackedFile();
            }
            if (this.unitVariantFileEditorControl != null)
            {
                this.unitVariantFileEditorControl.updatePackedFile();
            }
            if (this.readmeEditorControl != null)
            {
                this.readmeEditorControl.updatePackedFile();
            }
            this.currentPackFile.FinishedLoading += new EventHandler(this.currentPackFile_FinishedLoading);
            SaveFileDialog dialog = new SaveFileDialog {
                AddExtension = true,
                Filter = "Pack File|*.pack"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.currentPackFile.SaveAs(dialog.FileName);
                this.nodeRenamed = false;
            }
            this.currentPackFile.FinishedLoading -= new EventHandler(this.currentPackFile_FinishedLoading);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.locFileEditorControl != null)
            {
                this.locFileEditorControl.updatePackedFile();
            }
            if (this.atlasFileEditorControl != null)
            {
                this.atlasFileEditorControl.updatePackedFile();
            }
            if (this.unitVariantFileEditorControl != null)
            {
                this.unitVariantFileEditorControl.updatePackedFile();
            }
            if (this.readmeEditorControl != null)
            {
                this.readmeEditorControl.updatePackedFile();
            }
            this.currentPackFile.FinishedLoading += new EventHandler(this.currentPackFile_FinishedLoading);
            if (this.currentPackFile.Filepath.EndsWith("Untitled.pack"))
            {
                SaveFileDialog dialog = new SaveFileDialog {
                    FileName = "Untitled.pack",
                    AddExtension = true,
                    Filter = "Pack File|*.pack"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    this.currentPackFile.SaveAs(dialog.FileName);
                    this.nodeRenamed = false;
                }
            }
            else
            {
                this.currentPackFile.Save();
                this.nodeRenamed = false;
            }
            this.currentPackFile.FinishedLoading -= new EventHandler(this.currentPackFile_FinishedLoading);
        }

        private void searchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.search = new customMessageBox();
            base.AddOwnedForm(this.search);
            this.search.lblMessage.Text = "Query:";
            this.search.Text = @"Search files\directories";
            this.findChild(this.packTreeView.Nodes[0]);
            this.search.Show();
        }

        private void viewModel(PackedFile packedFile)
        {
        }

        private bool CurrentPackFileIsReadOnly
        {
            get
            {
                return ((this.cAPacksAreReadOnlyToolStripMenuItem.Checked && (this.currentPackFile != null)) && PackFile.CAPackList.Contains(Path.GetFileName(this.currentPackFile.Filepath)));
            }
        }

        public ToolStripLabel StatusLabel
        {
            get
            {
                return this.packStatusLabel;
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs ev)
        {
            tryUpdate();
        }

        public static void tryUpdate(bool showSuccess = true)
        {
            try
            {
                string path = Path.GetDirectoryName(Application.ExecutablePath);
                string version = Application.ProductVersion;
                bool update = DBFileTypesUpdater.checkVersion(path, ref version);
                if (showSuccess)
                {
                    string message = update ? "DB File description updated." : "No update performed.";
                    MessageBox.Show(message, "Update result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (update)
                {
                    DBTypeMap.Instance.initializeTypeMap(path);
                    DBReferenceMap.Instance.load(path);
                }
                if (version != Application.ProductVersion)
                {
                    MessageBox.Show(string.Format("A new version of PFM is available ({0})", version), "New Software version available");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    string.Format("Update failed: \n{0}\n{1}", e.Message, e.StackTrace), "Problem, sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void fromXsdFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DBTypeMap.Instance.loadFromXsd(open.FileName);
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Path.GetDirectoryName(Application.ExecutablePath);
            DBTypeMap.Instance.initializeTypeMap(path);
            DBReferenceMap.Instance.load(path);
            MessageBox.Show("DB File Definitions reloaded.");
        }

        bool canShow(PackedFile packedFile, out string display)
        {
            bool result = true;
            string key = Path.GetFileName(Path.GetDirectoryName(packedFile.Filepath));
            if (key.Contains("_tables"))
            {
                key = key.Remove(key.LastIndexOf('_'), 7);
            }
            if (DBTypeMap.Instance.IsSupported(key))
            {
                try
                {
                    DBFile currentDBFile = new DBFile(packedFile, key, false);
                    if (currentDBFile.TotalwarHeaderVersion > DBTypeMap.Instance.MaxVersion(key))
                    {
                        display = string.Format("{0}: needs {1}, has {2}", key, currentDBFile.TotalwarHeaderVersion, DBTypeMap.Instance.MaxVersion(key));
                        result = false;
                    }
                    else
                    {
                        display = string.Format("Version: {0}", currentDBFile.TotalwarHeaderVersion);
                    }
                }
                catch (Exception x)
                {
                    display = string.Format("{0}: {1}", key, x.Message);
                }
            }
            else
            {
                display = string.Format("{0}: no definition available", key);
                result = false;
            }
            return result;
        }
        private static bool headerVersionObsolete(PackedFile packedFile)
        {
            int version = -1;
            List<TypeInfo> type = null;
            try
            {
                string key = DBFile.typename(packedFile.Filepath);
                // do we have a definition at all?
                if (DBTypeMap.Instance.IsSupported(key))
                {
                    DBFile currentDBFile = new DBFile(packedFile, key, false);
                    version = currentDBFile.TotalwarHeaderVersion;
                }
            }
            catch (Exception x) {
                Console.WriteLine(x);
            }
            return version != -1 && version < type.Count-1;
        }

        private void updateOnStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.UpdateOnStartup = this.updateOnStartupToolStripMenuItem.Checked;
            Settings.Default.Save();
        }

        private void updatePackedFile(PackedFile packedFile)
        {
            try
            {
                string key = DBFile.typename(packedFile.Filepath);
                if (DBTypeMap.Instance.IsSupported(key))
                {
                    DBFile dbFile = new DBFile(packedFile, key, false);
                    if (dbFile.TotalwarHeaderVersion <= DBTypeMap.Instance.MaxVersion(key))
                    {
                        // found a more recent db definition; read data from db file
                        DBFile updatedFile = new DBFile(packedFile, key, true);

                        // identify FieldInstances missing in db file
                        TypeInfo dbFileInfo = updatedFile.CurrentType;
                        TypeInfo targetInfo = DBTypeMap.Instance[key, DBTypeMap.Instance.MaxVersion(key)];
                        for (int i = dbFileInfo.fields.Count; i < targetInfo.fields.Count; i++)
                        {
                            foreach (List<FieldInstance> entry in updatedFile.Entries)
                            {
                                FieldInstance field = FieldInstance.createInstance(targetInfo.fields[i]);
                                if (field != null)
                                {
                                    entry.Add(field);
                                }
                                else
                                {
                                    Console.WriteLine("can't create: {0}", targetInfo.fields[i]);
                                }
                            }
                        }
                        updatedFile.TotalwarHeaderVersion = DBTypeMap.Instance.MaxVersion(key);
                        packedFile.ReplaceData(updatedFile.GetBytes());

                        if (dbFileEditorControl.currentPackedFile == packedFile)
                        {
                            dbFileEditorControl.Open(packedFile, currentPackFile);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                MessageBox.Show(string.Format("Could not update {0}: {1}", Path.GetFileName(packedFile.Filepath), x.Message));
            }
        }
    

        private void saveToDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (DBTypeMap.Instance.saveToFile(Path.GetDirectoryName(Application.ExecutablePath)))
                {
                    string message = "You just created a new directory for your own DB definitions.\n" +
                        "This means that these will be used instead of the ones received in updates from TWC.\n" +
                        "Once you have uploaded your changes and they have been integrated,\n" +
                        "please delete the folder DBFileTypes_user.";
                    MessageBox.Show(message, "New User DB Definitions created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception x)
            {
                MessageBox.Show(string.Format("Could not save user db descriptions: {0}\nUser Directory won't be used anymore. A backup has been made.", x.Message));
            }
        }

        private void updateAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentPackFile != null)
            {
                foreach (PackedFile packedFile in currentPackFile.FileList)
                {
                    updatePackedFile(packedFile);
                }
            }
        }

        private void updateCurrentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dbFileEditorControl.currentPackedFile != null)
            {
                updatePackedFile(dbFileEditorControl.currentPackedFile);
            }
        }

        private void openAsText_click(object sender, EventArgs e) {
            List<PackedFile> packedFiles = new List<PackedFile>();
            packedFiles.Add(this.packTreeView.SelectedNode.Tag as PackedFile);
            openAsText(packedFiles[0]);
        }

        private bool unknownDbFormat(PackedFile file) {
            bool result = file.Filepath.StartsWith("db");
            string buffer;
            result &= !canShow(file, out buffer);
            return result;
    }

        private void exportUnknownToolStripMenuItem_Click(object sender, EventArgs e) {
            List<PackedFile> packedFiles = new List<PackedFile>();
            foreach (TreeNode node in this.packTreeView.Nodes) {
                if (node.Nodes.Count > 0) {
                    this.getPackedFilesFromBranch(packedFiles, node.Nodes, unknownDbFormat);
                } else {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
            this.extractFiles(packedFiles);
        }
    }
}

