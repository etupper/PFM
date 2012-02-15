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

namespace PackFileManager
{
    public class PackFileManagerForm : Form {
        #region Members
        private ToolStripMenuItem aboutToolStripMenuItem;
        private FolderBrowserDialog addDirectoryFolderBrowserDialog;
        private FolderBrowserDialog choosePathAnchorFolderBrowserDialog;
        private FolderBrowserDialog extractFolderBrowserDialog;
        private IContainer components;
        private ToolStripMenuItem contentsToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private PackFile currentPackFile = null;
        private ToolStripMenuItem cutToolStripMenuItem;

        private AtlasFileEditorControl atlasFileEditorControl;
        private DBFileEditorControl dbFileEditorControl;
        private ImageViewerControl imageViewerControl;
        private LocFileEditorControl locFileEditorControl;

        private MenuStrip menuStrip;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem indexToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private bool nodeRenamed;
        public OpenFileDialog openDBFileDialog;
        private bool openFileIsModified;
        private string openFilePath;
        private FileSystemWatcher openFileWatcher;
        private PackedFile openPackedFile;
        private ToolStripMenuItem openToolStripMenuItem;
        private ContextMenuStrip packActionMenuStrip;
        private ToolStripProgressBar packActionProgressBar;
        public OpenFileDialog packOpenFileDialog;
        private ToolStripStatusLabel packStatusLabel;
        public TreeView packTreeView;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ReadmeEditorControl readmeEditorControl;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private SaveFileDialog saveFileDialog;
        private ToolStripMenuItem saveToolStripMenuItem;
        private customMessageBox search;
        private ToolStripMenuItem searchToolStripMenuItem;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private SplitContainer splitContainer1;
        private StatusStrip statusStrip;
        private TextFileEditorControl textFileEditorControl;
        private ToolStripSeparator toolStripSeparator;
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
        private ToolStripMenuItem saveToDirectoryToolStripMenuItem;
        private ToolStripMenuItem filesMenu;
        private ToolStripMenuItem changePackTypeToolStripMenuItem;
        private ToolStripMenuItem bootToolStripMenuItem;
        private ToolStripMenuItem releaseToolStripMenuItem;
        private ToolStripMenuItem patchToolStripMenuItem;
        private ToolStripMenuItem modToolStripMenuItem;
        private ToolStripMenuItem movieToolStripMenuItem;
        private ToolStripMenuItem addToolStripMenuItem;
        private ToolStripMenuItem addFileToolStripMenuItem;
        private ToolStripMenuItem addDirectoryToolStripMenuItem;
        private ToolStripMenuItem deleteFileToolStripMenuItem;
        private ToolStripMenuItem replaceFileToolStripMenuItem;
        private ToolStripMenuItem renameToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem openToolStripMenuItem1;
        private ToolStripMenuItem openFileToolStripMenuItem;
        private ToolStripMenuItem openAsTextMenuItem;
        private ToolStripMenuItem extractToolStripMenuItem;
        private ToolStripMenuItem extractSelectedToolStripMenuItem;
        private ToolStripMenuItem extractAllToolStripMenuItem;
        private ToolStripMenuItem exportUnknownToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator8;
        private ToolStripMenuItem searchFileToolStripMenuItem;
        private ToolStripMenuItem updateDBFilesToolStripMenuItem;
        private ToolStripMenuItem updateCurrentToolStripMenuItem;
        private ToolStripMenuItem updateAllToolStripMenuItem;
        private ToolStripMenuItem exportFileListToolStripMenuItem;
        private ToolStripMenuItem createReadMeToolStripMenuItem;
        private ToolStripMenuItem extrasToolStripMenuItem;
        private ToolStripMenuItem cAPacksAreReadOnlyToolStripMenuItem;
        private ToolStripMenuItem updateOnStartupToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator9;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem toolStripMenuItem3;
        private ToolStripMenuItem toolStripMenuItem4;
        private ToolStripMenuItem toolStripMenuItem5;
        private ToolStripSeparator toolStripSeparator10;
        private ToolStripMenuItem toolStripMenuItem6;
        private ToolStripMenuItem toolStripMenuItem7;
        private ToolStripMenuItem toolStripMenuItem8;
        private ToolStripMenuItem toolStripMenuItem9;
        private ToolStripMenuItem toolStripMenuItem10;
        private ToolStripMenuItem toolStripMenuItem11;
        private ToolStripMenuItem toolStripMenuItem12;
        private ToolStripMenuItem emptyDirectoryToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem13;
        private ToolStripMenuItem dBFileFromTSVToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem14;
        private UnitVariantFileEditorControl unitVariantFileEditorControl;
        #endregion

        delegate bool FileFilter (PackedFile file);

        public PackFileManagerForm(string[] args)
        {
            this.InitializeComponent();

			string ShogunTotalWarDirectory = null;
			try {
				if (Settings.Default.UpdateOnStartup) {
                tryUpdate(false);
            }

				ShogunTotalWarDirectory = IOFunctions.GetShogunTotalWarDirectory ();
			} catch {
				ShogunTotalWarDirectory = ".";
			}
            if (string.IsNullOrEmpty(ShogunTotalWarDirectory))
            {
                if ((args.Length != 1) || !File.Exists(args[0]))
                {
                    if (this.choosePathAnchorFolderBrowserDialog.ShowDialog() != DialogResult.OK)
                    {
                        throw new InvalidDataException("unable to determine path to \"Total War : Shogun 2\" directory");
                    }
                    this.extractFolderBrowserDialog.SelectedPath = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
                }
                else
                {
                    this.choosePathAnchorFolderBrowserDialog.SelectedPath = Path.GetDirectoryName(args[0]);
                    this.extractFolderBrowserDialog.SelectedPath = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
                }
            }
            else
            {
                this.choosePathAnchorFolderBrowserDialog.SelectedPath = ShogunTotalWarDirectory + @"\data";
                this.extractFolderBrowserDialog.SelectedPath = ShogunTotalWarDirectory + @"\data";
            }
            this.saveFileDialog.InitialDirectory = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
            this.addDirectoryFolderBrowserDialog.SelectedPath = this.choosePathAnchorFolderBrowserDialog.SelectedPath;
            DBFileEditorControl control = new DBFileEditorControl {
                Dock = DockStyle.Fill
            };
            this.dbFileEditorControl = control;
            this.nodeRenamed = false;
            Text = string.Format("Pack File Manager {0}", Application.ProductVersion);
            if (args.Length == 1)
            {
                if (!File.Exists(args[0]))
                {
                    throw new ArgumentException("path is not a file or path does not exist");
                }
                this.OpenExistingPackFile(args[0]);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
			Form form = new Form {
                Text = "About Pack File Manager " + Application.ProductVersion,
                Size = new Size (0x177, 0xe1),
                WindowState = FormWindowState.Normal,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };
			Label label = new Label {
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

        private void currentPackFile_Modified()
        {
            this.refreshTitle();
            EnableMenuItems();
        }

        #region File Add/Delete
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

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e) {
            if (AddTo == null) {
                return;
            }
            OpenFileDialog addReplaceOpenFileDialog = new OpenFileDialog();
            addReplaceOpenFileDialog.Multiselect = true;
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK) {
                try {
                    foreach (string file in addReplaceOpenFileDialog.FileNames) {
                        AddTo.Add(new PackedFile(file));
                    }
                    this.nodeRenamed = true;
                } catch (Exception x) {
                    MessageBox.Show(x.Message, "Problem, Sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                file.Deleted = true;
            }
        }

        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e) {
            if (AddTo != null && addDirectoryFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    // AddTo.Add(addDirectoryFolderBrowserDialog.SelectedPath);
                } catch (Exception x) {
                    MessageBox.Show("Failed to add " + addDirectoryFolderBrowserDialog.SelectedPath + ": " + x.Message, "Failed to add directory");
                }
            }
        }

        private void createReadMeToolStripMenuItem_Click(object sender, EventArgs e) {
            PackedFile readme = new PackedFile() { Name = "readme.xml", Data = new byte[0] };
            this.currentPackFile.Add("readme.xml", readme);
            this.openReadMe(readme);
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.nodeRenamed) {
                MessageBox.Show("Please save to continue.");
            } else {
                this.packTreeView.SelectedNode.BeginEdit();
                this.nodeRenamed = true;
            }
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog addReplaceOpenFileDialog = new OpenFileDialog();
            addReplaceOpenFileDialog.Multiselect = false;
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK) {
                PackedFile tag = this.packTreeView.SelectedNode.Tag as PackedFile;
                tag.Source = new FileSystemSource(addReplaceOpenFileDialog.FileName);
            }
        }

        #endregion

        #region Form Management
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            base.Close();
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

        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.packTreeView = new System.Windows.Forms.TreeView();
            this.packActionMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.emptyDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dBFileFromTSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.changePackTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.releaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.movieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.exportFileListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filesMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.openToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openAsTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.toolStripMenuItem14 = new System.Windows.Forms.ToolStripMenuItem();
            this.packActionMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
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
            this.packTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.packTreeView_AfterSelect);
            this.packTreeView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.packTreeView_MouseDoubleClick);
            this.packTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.packTreeView_MouseDown);
            this.packTreeView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.packTreeView_PreviewKeyDown);
            // 
            // packActionMenuStrip
            // 
            this.packActionMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripSeparator10,
            this.toolStripMenuItem6,
            this.toolStripMenuItem9});
            this.packActionMenuStrip.Name = "packActionMenuStrip";
            this.packActionMenuStrip.Size = new System.Drawing.Size(153, 142);
            this.packActionMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.packActionMenuStrip_Opening);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem3,
            this.toolStripMenuItem13,
            this.toolStripMenuItem2,
            this.toolStripMenuItem14});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem1.Text = "Add";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.ShortcutKeyDisplayString = "Ins";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem2.Text = "&File(s)...";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.ShortcutKeyDisplayString = "Shift+Ins";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem3.Text = "&Directory...";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.addDirectoryToolStripMenuItem_Click);
            // 
            // toolStripMenuItem13
            // 
            this.toolStripMenuItem13.Name = "toolStripMenuItem13";
            this.toolStripMenuItem13.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem13.Text = "Empty Directory";
            this.toolStripMenuItem13.Click += new System.EventHandler(this.emptyDirectoryToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.ShortcutKeyDisplayString = "Del";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem4.Text = "Delete";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem5.Text = "Rename";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(149, 6);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem7,
            this.toolStripMenuItem8});
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem6.Text = "Open";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.toolStripMenuItem7.Size = new System.Drawing.Size(201, 22);
            this.toolStripMenuItem7.Text = "Open External...";
            this.toolStripMenuItem7.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(201, 22);
            this.toolStripMenuItem8.Text = "Open as Text";
            this.toolStripMenuItem8.Click += new System.EventHandler(this.openAsTextMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem10,
            this.toolStripMenuItem11,
            this.toolStripMenuItem12});
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.Size = new System.Drawing.Size(152, 22);
            this.toolStripMenuItem9.Text = "Extract";
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            this.toolStripMenuItem10.ShortcutKeyDisplayString = "Ctl+X";
            this.toolStripMenuItem10.Size = new System.Drawing.Size(202, 22);
            this.toolStripMenuItem10.Text = "Extract &Selected...";
            this.toolStripMenuItem10.Click += new System.EventHandler(this.extractSelectedToolStripMenuItem_Click);
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            this.toolStripMenuItem11.Size = new System.Drawing.Size(202, 22);
            this.toolStripMenuItem11.Text = "Extract &All...";
            this.toolStripMenuItem11.Click += new System.EventHandler(this.extractAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem12
            // 
            this.toolStripMenuItem12.Name = "toolStripMenuItem12";
            this.toolStripMenuItem12.Size = new System.Drawing.Size(202, 22);
            this.toolStripMenuItem12.Text = "Extract Unknown...";
            this.toolStripMenuItem12.Click += new System.EventHandler(this.exportUnknownToolStripMenuItem_Click);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.emptyDirectoryToolStripMenuItem,
            this.addDirectoryToolStripMenuItem,
            this.addFileToolStripMenuItem,
            this.dBFileFromTSVToolStripMenuItem});
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.addToolStripMenuItem.Text = "Add";
            // 
            // addFileToolStripMenuItem
            // 
            this.addFileToolStripMenuItem.Name = "addFileToolStripMenuItem";
            this.addFileToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
            this.addFileToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.addFileToolStripMenuItem.Text = "&File(s)...";
            this.addFileToolStripMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // addDirectoryToolStripMenuItem
            // 
            this.addDirectoryToolStripMenuItem.Name = "addDirectoryToolStripMenuItem";
            this.addDirectoryToolStripMenuItem.ShortcutKeyDisplayString = "Shift+Ins";
            this.addDirectoryToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.addDirectoryToolStripMenuItem.Text = "&Directory...";
            this.addDirectoryToolStripMenuItem.Click += new System.EventHandler(this.addDirectoryToolStripMenuItem_Click);
            // 
            // emptyDirectoryToolStripMenuItem
            // 
            this.emptyDirectoryToolStripMenuItem.Name = "emptyDirectoryToolStripMenuItem";
            this.emptyDirectoryToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.emptyDirectoryToolStripMenuItem.Text = "Empty Directory";
            this.emptyDirectoryToolStripMenuItem.Click += new System.EventHandler(this.emptyDirectoryToolStripMenuItem_Click);
            // 
            // dBFileFromTSVToolStripMenuItem
            // 
            this.dBFileFromTSVToolStripMenuItem.Name = "dBFileFromTSVToolStripMenuItem";
            this.dBFileFromTSVToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.dBFileFromTSVToolStripMenuItem.Text = "DB file from TSV";
            this.dBFileFromTSVToolStripMenuItem.Click += new System.EventHandler(this.dBFileFromTSVToolStripMenuItem_Click);
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
            this.toolStripSeparator,
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
            // toolStripSeparator
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(169, 6);
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
            this.releaseToolStripMenuItem,
            this.patchToolStripMenuItem,
            this.modToolStripMenuItem,
            this.movieToolStripMenuItem});
            this.changePackTypeToolStripMenuItem.Name = "changePackTypeToolStripMenuItem";
            this.changePackTypeToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
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
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(169, 6);
            // 
            // exportFileListToolStripMenuItem
            // 
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
            // filesMenu
            // 
            this.filesMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.deleteFileToolStripMenuItem,
            this.replaceFileToolStripMenuItem,
            this.renameToolStripMenuItem,
            this.toolStripSeparator4,
            this.openToolStripMenuItem1,
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
            this.openToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFileToolStripMenuItem,
            this.openAsTextMenuItem});
            this.openToolStripMenuItem1.Name = "openToolStripMenuItem1";
            this.openToolStripMenuItem1.Size = new System.Drawing.Size(154, 22);
            this.openToolStripMenuItem1.Text = "Open";
            // 
            // openFileToolStripMenuItem
            // 
            this.openFileToolStripMenuItem.Name = "openFileToolStripMenuItem";
            this.openFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openFileToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.openFileToolStripMenuItem.Text = "Open External...";
            this.openFileToolStripMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // openAsTextMenuItem
            // 
            this.openAsTextMenuItem.Name = "openAsTextMenuItem";
            this.openAsTextMenuItem.Size = new System.Drawing.Size(201, 22);
            this.openAsTextMenuItem.Text = "Open as Text";
            this.openAsTextMenuItem.Click += new System.EventHandler(this.openAsTextMenuItem_Click);
            // 
            // extractToolStripMenuItem
            // 
            this.extractToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.extractSelectedToolStripMenuItem,
            this.extractAllToolStripMenuItem,
            this.exportUnknownToolStripMenuItem});
            this.extractToolStripMenuItem.Name = "extractToolStripMenuItem";
            this.extractToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.extractToolStripMenuItem.Text = "Extract";
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
            // exportUnknownToolStripMenuItem
            // 
            this.exportUnknownToolStripMenuItem.Name = "exportUnknownToolStripMenuItem";
            this.exportUnknownToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.exportUnknownToolStripMenuItem.Text = "Extract Unknown...";
            this.exportUnknownToolStripMenuItem.Click += new System.EventHandler(this.exportUnknownToolStripMenuItem_Click);
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
            this.saveToDirectoryToolStripMenuItem.Enabled = false;
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
            this.updateOnStartupToolStripMenuItem});
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
            this.cAPacksAreReadOnlyToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.cAPacksAreReadOnlyToolStripMenuItem.Text = "CA Packs Are Read Only";
            this.cAPacksAreReadOnlyToolStripMenuItem.ToolTipText = "If checked, the original pack files for the game can be viewed but not edited.";
            // 
            // updateOnStartupToolStripMenuItem
            // 
            this.updateOnStartupToolStripMenuItem.CheckOnClick = true;
            this.updateOnStartupToolStripMenuItem.Name = "updateOnStartupToolStripMenuItem";
            this.updateOnStartupToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.updateOnStartupToolStripMenuItem.Text = "Update on Startup";
            this.updateOnStartupToolStripMenuItem.Click += new System.EventHandler(this.updateOnStartupToolStripMenuItem_Click);
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
            // toolStripMenuItem14
            // 
            this.toolStripMenuItem14.Name = "toolStripMenuItem14";
            this.toolStripMenuItem14.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem14.Text = "DB file from TSV";
            this.toolStripMenuItem14.Click += new System.EventHandler(this.dBFileFromTSVToolStripMenuItem_Click);
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
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void PackFileManagerForm_Activated(object sender, EventArgs e) {
            if ((base.OwnedForms.Length > 0) && (this.search != null)) {
                this.packTreeView.SelectedNode = this.search.nextNode;
            }
        }

        private void PackFileManagerForm_FormClosing(object sender, FormClosingEventArgs e) {
            if ((((e.CloseReason != CloseReason.WindowsShutDown) && (e.CloseReason != CloseReason.TaskManagerClosing)) && (e.CloseReason != CloseReason.ApplicationExitCall)) && (this.handlePackFileChangesWithUserInput() == DialogResult.Cancel)) {
                e.Cancel = true;
            }
        }

        private void PackFileManagerForm_GotFocus(object sender, EventArgs e) {
            base.Activated -= new EventHandler(this.PackFileManagerForm_GotFocus);
            if (this.openFileIsModified) {
                this.openFileIsModified = false;
                if (MessageBox.Show("Changes were made to the extracted file. Do you want to replace the packed file with the extracted file?", "Save changes?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    this.openPackedFile.Data = (File.ReadAllBytes(this.openFilePath));
                }
            }
            while (File.Exists(this.openFilePath)) {
                try {
                    File.Delete(this.openFilePath);
                } catch (IOException) {
                    if (MessageBox.Show("Unable to delete the temporary file; is it still in use by the external editor?\r\n\r\nClick Retry to try deleting it again or Cancel to leave it in the temporary directory.", "Temporary file in use", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel) {
                        break;
                    }
                }
            }
        }


        private void PackFileManagerForm_Load(object sender, EventArgs e) {
            base.TopMost = true;
        }

        private void PackFileManagerForm_Shown(object sender, EventArgs e) {
            base.TopMost = false;
        }
        #endregion

        private void exportFileListToolStripMenuItem_Click(object sender, EventArgs e) {
            this.saveFileDialog.FileName = Path.GetFileNameWithoutExtension(this.currentPackFile.Filepath) + ".pack-file-list.txt";
            if (this.saveFileDialog.ShowDialog() == DialogResult.OK) {
                using (StreamWriter writer = new StreamWriter(this.saveFileDialog.FileName)) {
                    foreach (PackedFile file in this.currentPackFile.Files) {
                        writer.WriteLine(file.FullPath);
                    }
                }
            }
        }

        #region Extract
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
                    string path = Path.Combine(selectedPath, file.FullPath);
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
                                    this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
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
                                this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
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
                    this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                    Application.DoEvents();
                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        stream.Write(file.Data, 0, (int) file.Size);
                    }
                    num++;
                    this.packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
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
        #endregion

        protected void EnableMenuItems() {
            saveToDirectoryToolStripMenuItem.Enabled = currentPackFile != null && !CurrentPackFileIsReadOnly && CurrentPackFile.IsModified;
            createReadMeToolStripMenuItem.Enabled = !CurrentPackFileIsReadOnly;
        }

        #region Packed from Tree
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
        #endregion

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
				Refresh ();
			}
		}

        #region Open Pack
        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            PFHeader header = new PFHeader("PFH3") {
                Type = PackType.Mod,
                Version = 0,
                FileCount = 0,
                ReplacedPackFileName = "",
                DataStart = 0x20
            };
            CurrentPackFile = new PackFile("Untitled.pack", header);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            if ((this.handlePackFileChangesWithUserInput() != DialogResult.Cancel) && (this.packOpenFileDialog.ShowDialog() == DialogResult.OK)) {
                this.OpenExistingPackFile(this.packOpenFileDialog.FileName);
            }
        }

        private void OpenExistingPackFile(string filepath)
        {
            try
            {
                PackFileCodec codec = new PackFileCodec();
                new LoadUpdater(codec, filepath, this.packStatusLabel, packActionProgressBar);
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
            packedFiles.Add(this.packTreeView.SelectedNode.Tag as PackedFile);
            openAsText(packedFiles[0]);
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e) {
            this.openExternal(this.packTreeView.SelectedNode.Tag as PackedFile, "openas");
        }

        public void openExternal(PackedFile packedFile, string verb)
        {
            if (packedFile == null)
            {
                return;
            }
            this.openPackedFile = packedFile;
            this.openFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(packedFile.FullPath));
            if (verb == "openimage")
            {
                ImageViewerControl control = new ImageViewerControl {
                    Dock = DockStyle.Fill
                };
                this.imageViewerControl = control;
                this.splitContainer1.Panel2.Controls.Add(this.imageViewerControl);
                this.imageViewerControl.SetImage(packedFile.Data, this.openFilePath);
            }
            else
            {
                File.WriteAllBytes(this.openFilePath, packedFile.Data);
                this.openWith(this.openFilePath, verb);
            }
        }

        private void OpenPackedFile(object tag) {
            PackedFile packedFile = tag as PackedFile;
            if (packedFile.FullPath == "readme.xml") {
                this.openReadMe(packedFile);
            } else if (packedFile.FullPath.EndsWith(".loc")) {
                this.locFileEditorControl = new LocFileEditorControl(packedFile);
                this.locFileEditorControl.Dock = DockStyle.Fill;
                this.splitContainer1.Panel2.Controls.Add(this.locFileEditorControl);
            } else if (packedFile.FullPath.Contains(".tga") || packedFile.FullPath.Contains(".dds") || packedFile.FullPath.Contains(".png") || packedFile.FullPath.Contains(".jpg") || packedFile.FullPath.Contains(".bmp") || packedFile.FullPath.Contains(".psd")) {
                this.openExternal(packedFile, "openimage");
            } else if (packedFile.FullPath.EndsWith(".atlas")) {
                AtlasFileEditorControl control = new AtlasFileEditorControl(packedFile) {
                    Dock = DockStyle.Fill
                };
                this.atlasFileEditorControl = control;
                this.splitContainer1.Panel2.Controls.Add(this.atlasFileEditorControl);
            } else if (packedFile.FullPath.EndsWith(".unit_variant")) {
                UnitVariantFileEditorControl control2 = new UnitVariantFileEditorControl(packedFile) {
                    Dock = DockStyle.Fill
                };
                this.unitVariantFileEditorControl = control2;
                this.splitContainer1.Panel2.Controls.Add(this.unitVariantFileEditorControl);
            } else if (packedFile.FullPath.Contains(".rigid")) {
                // this.viewModel(packedFile);
            } else if (isTextFileType(packedFile)) {
                openAsText(packedFile);
            } else if (packedFile.FullPath.StartsWith("db")) {
                try {
                    this.dbFileEditorControl.Open(packedFile, currentPackFile);
                    this.splitContainer1.Panel2.Controls.Add(this.dbFileEditorControl);
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
                this.openFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file));
                this.openFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                this.openFileWatcher.Changed += new FileSystemEventHandler(this.openFileWatcher_Changed);
                this.openFileWatcher.EnableRaisingEvents = true;
                this.openFileIsModified = false;
                base.Activated += new EventHandler(this.PackFileManagerForm_GotFocus);
            } catch (Exception x) {
                MessageBox.Show("Problem opening " + file + ": " + x.Message, "Could not open file");
            }
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
        #endregion

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
            this.splitContainer1.Panel2.Controls.Clear();
			
            if (this.packTreeView.SelectedNode != null)
            {
                this.packStatusLabel.Text = " Viewing: " + this.packTreeView.SelectedNode.Text;
                this.packTreeView.LabelEdit = this.packTreeView.SelectedNode != this.packTreeView.Nodes[0];
                if (this.packTreeView.SelectedNode.Tag is PackedFile)
                {
                    this.OpenPackedFile(this.packTreeView.SelectedNode.Tag);
                }
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
        #endregion

        public override void Refresh() {
            List<string> expandedNodes = new List<string>();
            foreach (TreeNode node in this.getTreeViewBranch(this.packTreeView.Nodes)) {
                if (node.IsExpanded && node is PackEntryNode) {
                    expandedNodes.Add((node.Tag as PackEntry).FullPath);
                }
            }
            string str = (this.packTreeView.SelectedNode != null) ? (this.packTreeView.SelectedNode.Tag as PackEntry).FullPath : "";
            this.packTreeView.Nodes.Clear();
            if (currentPackFile == null) {
                return;
            }
            TreeNode node2 = new DirEntryNode(CurrentPackFile.Root);
            packTreeView.Nodes.Add(node2);

            foreach (TreeNode node in this.getTreeViewBranch(this.packTreeView.Nodes)) {
                string path = (node.Tag as PackEntry).FullPath;
                if (expandedNodes.Contains(path)) {
                    node.Expand();
                }
				if (path == str) {
                    this.packTreeView.SelectedNode = node;
                }
            }
            this.filesMenu.Enabled = true;
            this.saveToolStripMenuItem.Enabled = true;
            this.saveAsToolStripMenuItem.Enabled = true;
            this.createReadMeToolStripMenuItem.Enabled = true;
            this.bootToolStripMenuItem.Checked = this.currentPackFile.Header.Type == PackType.Boot;
            this.releaseToolStripMenuItem.Checked = this.currentPackFile.Header.Type == PackType.Release;
            this.patchToolStripMenuItem.Checked = this.currentPackFile.Header.Type == PackType.Patch;
            this.movieToolStripMenuItem.Checked = this.currentPackFile.Header.Type == PackType.Movie;
            this.modToolStripMenuItem.Checked = this.currentPackFile.Header.Type == PackType.Mod;
            this.packTreeView_AfterSelect(this, new TreeViewEventArgs(this.packTreeView.SelectedNode));
            this.refreshTitle();
            base.Refresh();
        }

        private void refreshTitle()
        {
            this.Text = Path.GetFileName(this.currentPackFile.Filepath);
            if (this.currentPackFile.IsModified)
            {
                this.Text = this.Text + " (modified)";
            }
            this.Text = this.Text + string.Format(" - Pack File Manager {0}", Application.ProductVersion);
        }

        private void closeEditors() {
            if (this.locFileEditorControl != null) {
                this.locFileEditorControl.updatePackedFile();
            }
            if (this.atlasFileEditorControl != null) {
                this.atlasFileEditorControl.updatePackedFile();
            }
            if (this.unitVariantFileEditorControl != null) {
                this.unitVariantFileEditorControl.updatePackedFile();
            }
            if (this.readmeEditorControl != null) {
                this.readmeEditorControl.updatePackedFile();
            }
        }

        #region Save Pack
        private bool CurrentPackFileIsReadOnly {
            get {
                bool result = false;
                if (cAPacksAreReadOnlyToolStripMenuItem.Checked) {
                    switch (currentPackFile.Type) {
                        case PackType.Mod:
                            result = true;
                            break;
                        case PackType.Movie:
                            Regex caMovieRe = new Regex("(patch_)?movies([0-9]*).pack");
                            result = caMovieRe.IsMatch(Path.GetFileName(currentPackFile.Filepath));
                            break;
                        default:
                            result = false;
                            break;
                    }
                }
                return result;
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeEditors();
            SaveFileDialog dialog = new SaveFileDialog {
                AddExtension = true,
                Filter = "Pack File|*.pack"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                new PackFileCodec().writeToFile(dialog.FileName, currentPackFile);
                OpenExistingPackFile(dialog.FileName);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.currentPackFile.Filepath.EndsWith("Untitled.pack")) {
                saveAsToolStripMenuItem_Click(null, null);
            } else if (CurrentPackFileIsReadOnly) {
                MessageBox.Show("Won't save CA file with current Setting.");
            } else {
                closeEditors();
                string tempFile = Path.GetTempFileName();
                new PackFileCodec().writeToFile(tempFile, currentPackFile);
                File.Delete(currentPackFile.Filepath);
                File.Move(tempFile, currentPackFile.Filepath);
                OpenExistingPackFile(currentPackFile.Filepath);
            }
        }
        #endregion

        private void searchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.search = new customMessageBox();
            base.AddOwnedForm(this.search);
            this.search.lblMessage.Text = "Query:";
            this.search.Text = @"Search files\directories";
            this.findChild(this.packTreeView.Nodes[0]);
            this.search.Show();
        }

        private void findChild(TreeNode tnChild) {
            foreach (TreeNode node in tnChild.Nodes) {
                this.search.tnList.Add(node);
                this.findChild(node);
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

        #region DB Management
        private void fromXsdFileToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog open = new OpenFileDialog();
            open.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                DBTypeMap.Instance.loadFromXsd(open.FileName);
            }
        }

        private void saveToDirectoryToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                //                if (DBTypeMap.Instance.saveToFile(Path.GetDirectoryName(Application.ExecutablePath)))
                //                {
                //                    string message = "You just created a new directory for your own DB definitions.\n" +
                //                        "This means that these will be used instead of the ones received in updates from TWC.\n" +
                //                        "Once you have uploaded your changes and they have been integrated,\n" +
                //                        "please delete the folder DBFileTypes_user.";
                //                    MessageBox.Show(message, "New User DB Definitions created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //                }
            } catch (Exception x) {
                MessageBox.Show(string.Format("Could not save user db descriptions: {0}\nUser Directory won't be used anymore. A backup has been made.", x.Message));
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
            if (dbFileEditorControl.currentPackedFile != null) {
                updatePackedFile(dbFileEditorControl.currentPackedFile);
            }
        }

        private void updateOnStartupToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.UpdateOnStartup = this.updateOnStartupToolStripMenuItem.Checked;
            Settings.Default.Save();
        }
        
        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            string path = Path.GetDirectoryName(Application.ExecutablePath);
            DBTypeMap.Instance.initializeTypeMap(path);
            MessageBox.Show("DB File Definitions reloaded.");
        }

        public static void tryUpdate(bool showSuccess = true) {
			try {
				string path = Path.GetDirectoryName (Application.ExecutablePath);
				string version = Application.ProductVersion;
				bool update = DBFileTypesUpdater.checkVersion (path, ref version);
				if (showSuccess) {
					string message = update ? "DB File description updated." : "No update performed.";
					MessageBox.Show (message, "Update result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
				if (update) {
					DBTypeMap.Instance.initializeTypeMap (path);
                }
				if (version != Application.ProductVersion) {
					MessageBox.Show (string.Format ("A new version of PFM is available ({0})", version), "New Software version available");
                }
			} catch (Exception e) {
				MessageBox.Show (
                    string.Format ("Update failed: \n{0}\n{1}", e.Message, e.StackTrace), 
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
                    int maxVersion = DBTypeMap.Instance.MaxVersion(key);
                    DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
                    if (header.Version < maxVersion) {
                        // found a more recent db definition; read data from db file
                        DBFile updatedFile = new PackedFileDbCodec().readDbFile(packedFile);

                        // identify FieldInstances missing in db file
                        TypeInfo dbFileInfo = updatedFile.CurrentType;
                        TypeInfo targetInfo = DBTypeMap.Instance[key, maxVersion]; ;
                        for (int i = dbFileInfo.fields.Count; i < targetInfo.fields.Count; i++) {
                            foreach (List<FieldInstance> entry in updatedFile.Entries) {
                                FieldInstance field = new FieldInstance(targetInfo.fields[i], targetInfo.fields[i].DefaultValue);
                                if (field != null) {
                                    entry.Add(field);
                                } else {
                                    Console.WriteLine("can't create: {0}", targetInfo.fields[i]);
                                }
                            }
                        }
                        updatedFile.Header.Version = maxVersion;
                        packedFile.Data = PackedFileDbCodec.Encode(updatedFile);

                        if (dbFileEditorControl.currentPackedFile == packedFile) {
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
        #endregion

        private void emptyDirectoryToolStripMenuItem_Click(object sender, EventArgs e) {
            VirtualDirectory dir = packTreeView.SelectedNode.Tag as VirtualDirectory;
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
            VirtualDirectory dir = packTreeView.SelectedNode.Tag as VirtualDirectory;
            if (dir != null) {
                if (openDBFileDialog.ShowDialog() == DialogResult.OK) {
                    using (FileStream filestream = File.OpenRead(openDBFileDialog.FileName)) {
                        string filename = Path.GetFileNameWithoutExtension(openDBFileDialog.FileName);
                        DBFile file = new TextDbCodec().readDbFile(filestream);
                        byte[] data;
                        using (MemoryStream stream = new MemoryStream()) {
                            PackedFileDbCodec.Instance.Encode(stream, file);
                            data = stream.ToArray();
                        }
                        dir.Add(new PackedFile() { Data = data, Name = filename, Parent = dir });
                    }
                }
            } else {
                MessageBox.Show("Select a directory to add to");
    }
        }
	}

    class LoadUpdater {
        private string file;
        private int currentCount = 0;
        private uint count;
        private ToolStripLabel label;
        private ToolStripProgressBar progress;
        PackFileCodec currentCodec;
        public LoadUpdater(PackFileCodec codec, string f, ToolStripLabel l, ToolStripProgressBar bar) {
            file = Path.GetFileName(f);
            label = l;
            progress = bar;
            bar.Minimum = 0;
            bar.Value = 0;
            bar.Step = 10;
            Connect(codec);
                }
        public void Connect(PackFileCodec codec) {
            codec.HeaderLoaded += HeaderLoaded;
            codec.PackedFileLoaded += PackedFileLoaded;
            codec.PackFileLoaded += PackFileLoaded;
            currentCodec = codec;
            }
        public void Disconnect() {
            if (currentCodec != null) {
                currentCodec.HeaderLoaded -= HeaderLoaded;
                currentCodec.PackedFileLoaded -= PackedFileLoaded;
                currentCodec.PackFileLoaded -= PackFileLoaded;
                currentCodec = null;
        }
    }
        public void HeaderLoaded(PFHeader header) {
            count = header.FileCount;
            progress.Maximum = (int) header.FileCount;
            label.Text = string.Format("Loading {0}: 0 of {0} files loaded", file, header.FileCount);
            Application.DoEvents();
        }
        public void PackedFileLoaded(PackedFile file) {
            currentCount++;
            if (currentCount % 10 <= 0) {
                label.Text = string.Format("Opening {0} ({1} of {2} files loaded)",
                    file, currentCount, count);
                progress.PerformStep();
                Application.DoEvents();
            }
        }
        public void PackFileLoaded(PackFile file) {
            label.Text = string.Format("Finished opening {0} - {1} files loaded", file, count);
            progress.Maximum = 0;
            Disconnect();
        }
    }
}

