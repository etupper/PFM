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
        private PackFile currentPackFile;
        private ToolStripMenuItem cutToolStripMenuItem;

        private AtlasFileEditorControl atlasFileEditorControl;
        private readonly DBFileEditorControl dbFileEditorControl;
        private ImageViewerControl imageViewerControl;
        private LocFileEditorControl locFileEditorControl;

        private MenuStrip menuStrip;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem indexToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
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
        private ToolStripMenuItem bootXToolStripMenuItem;
        private ToolStripMenuItem releaseToolStripMenuItem;
        private ToolStripMenuItem patchToolStripMenuItem;
        private ToolStripMenuItem modToolStripMenuItem;
        private ToolStripMenuItem movieToolStripMenuItem;
        private ToolStripMenuItem shaderToolStripMenuItem;
        private ToolStripMenuItem shader2ToolStripMenuItem;
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

        public PackFileManagerForm (string[] args) {
            InitializeComponent ();

            try {
                if (Settings.Default.UpdateOnStartup) {
                    tryUpdate (false);
                }
            } catch {
            }

            InitializeBrowseDialogs (args);

            Text = string.Format("Pack File Manager {0}", Application.ProductVersion);
            if (args.Length == 1) {
                if (!File.Exists(args[0])) {
                    throw new ArgumentException("path is not a file or path does not exist");
                }
                OpenExistingPackFile(args[0]);
            }
            var control = new DBFileEditorControl() {
                Dock = DockStyle.Fill
            };
            dbFileEditorControl = control;
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

        private void cAPacksAreReadOnlyToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if (cAPacksAreReadOnlyToolStripMenuItem.CheckState == CheckState.Unchecked)
            {
                var advisory = new caFileEditAdvisory();
                cAPacksAreReadOnlyToolStripMenuItem.CheckState = advisory.DialogResult == DialogResult.Yes ? CheckState.Unchecked : CheckState.Checked;
            }
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

        private void addFileToolStripMenuItem_Click(object sender, EventArgs e) {
            if (AddTo == null) {
                return;
            }
            var addReplaceOpenFileDialog = new OpenFileDialog();
            addReplaceOpenFileDialog.Multiselect = true;
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK) {
                try {
                    foreach (string file in addReplaceOpenFileDialog.FileNames) {
                        AddTo.Add(new PackedFile(file));
                    }
                } catch (Exception x) {
                    MessageBox.Show(x.Message, "Problem, Sir!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
            if (AddTo != null && addDirectoryFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    AddTo.Add(addDirectoryFolderBrowserDialog.SelectedPath);
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
            OpenFileDialog addReplaceOpenFileDialog = new OpenFileDialog();
            addReplaceOpenFileDialog.Multiselect = false;
            if (addReplaceOpenFileDialog.ShowDialog() == DialogResult.OK) {
                PackedFile tag = packTreeView.SelectedNode.Tag as PackedFile;
                tag.Source = new FileSystemSource(addReplaceOpenFileDialog.FileName);
            }
        }
        
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
            var dir = packTreeView.SelectedNode.Tag as VirtualDirectory;
            if (dir != null) {
                if (openDBFileDialog.ShowDialog() == DialogResult.OK) {
                    try {
                        using (FileStream filestream = File.OpenRead(openDBFileDialog.FileName)) {
                            string filename = Path.GetFileNameWithoutExtension(openDBFileDialog.FileName);
                            DBFile file = new TextDbCodec().readDbFile(filestream);
                            byte[] data;
                            using (var stream = new MemoryStream()) {
                                PackedFileDbCodec.Instance.Encode(stream, file);
                                data = stream.ToArray();
                            }
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                Utilities.DisposeHandlers(this);
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent() {
            components = new System.ComponentModel.Container ();
            packTreeView = new System.Windows.Forms.TreeView ();
            packActionMenuStrip = new System.Windows.Forms.ContextMenuStrip (components);
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem13 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem14 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator ();
            toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripMenuItem12 = new System.Windows.Forms.ToolStripMenuItem ();
            addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            emptyDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            addDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            dBFileFromTSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            packOpenFileDialog = new System.Windows.Forms.OpenFileDialog ();
            splitContainer1 = new System.Windows.Forms.SplitContainer ();
            menuStrip = new System.Windows.Forms.MenuStrip ();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator = new System.Windows.Forms.ToolStripSeparator ();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator ();
            changePackTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            bootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            bootXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            releaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            patchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            modToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            movieToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            shaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            shader2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            exportFileListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator ();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            filesMenu = new System.Windows.Forms.ToolStripMenuItem ();
            deleteFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            replaceFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator ();
            openToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem ();
            openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            openAsTextMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            extractSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            exportUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator ();
            createReadMeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            searchFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator ();
            cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator ();
            selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            updateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            searchForUpdateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            fromXsdFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            saveToDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            updateDBFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            updateCurrentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            updateAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            extrasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            cAPacksAreReadOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            updateOnStartupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            indexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator ();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
            statusStrip = new System.Windows.Forms.StatusStrip ();
            packStatusLabel = new System.Windows.Forms.ToolStripStatusLabel ();
            packActionProgressBar = new System.Windows.Forms.ToolStripProgressBar ();
            extractFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog ();
            choosePathAnchorFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog ();
            saveFileDialog = new System.Windows.Forms.SaveFileDialog ();
            addDirectoryFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog ();
            openDBFileDialog = new System.Windows.Forms.OpenFileDialog ();
            packActionMenuStrip.SuspendLayout ();
            // ((System.ComponentModel.ISupportInitialize)(splitContainer1)).BeginInit();
            splitContainer1.Panel1.SuspendLayout ();
            splitContainer1.SuspendLayout ();
            menuStrip.SuspendLayout ();
            statusStrip.SuspendLayout ();
            SuspendLayout ();
            // 
            // packTreeView
            // 
            packTreeView.ContextMenuStrip = packActionMenuStrip;
            packTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            packTreeView.ForeColor = System.Drawing.SystemColors.WindowText;
            packTreeView.HideSelection = false;
            packTreeView.Indent = 19;
            packTreeView.Location = new System.Drawing.Point (0, 0);
            packTreeView.Name = "packTreeView";
            packTreeView.Size = new System.Drawing.Size (198, 599);
            packTreeView.TabIndex = 2;
            packTreeView.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler (packTreeView_AfterLabelEdit);
            packTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler (packTreeView_ItemDrag);
            packTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler (packTreeView_AfterSelect);
            packTreeView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler (packTreeView_MouseDoubleClick);
            packTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler (packTreeView_MouseDown);
            packTreeView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler (packTreeView_PreviewKeyDown);
            // 
            // packActionMenuStrip
            // 
            packActionMenuStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem[] {
            toolStripMenuItem1,
            toolStripMenuItem4,
            toolStripMenuItem5,
            toolStripSeparator10,
            toolStripMenuItem6,
            toolStripMenuItem9});
            packActionMenuStrip.Name = "packActionMenuStrip";
            packActionMenuStrip.Size = new System.Drawing.Size (153, 142);
            packActionMenuStrip.Opening += new System.ComponentModel.CancelEventHandler (packActionMenuStrip_Opening);
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            toolStripMenuItem3,
            toolStripMenuItem13,
            toolStripMenuItem2,
            toolStripMenuItem14});
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size (152, 22);
            toolStripMenuItem1.Text = "Add";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.ShortcutKeyDisplayString = "Shift+Ins";
            toolStripMenuItem3.Size = new System.Drawing.Size (185, 22);
            toolStripMenuItem3.Text = "&Directory...";
            toolStripMenuItem3.Click += new System.EventHandler (addDirectoryToolStripMenuItem_Click);
            // 
            // toolStripMenuItem13
            // 
            toolStripMenuItem13.Name = "toolStripMenuItem13";
            toolStripMenuItem13.Size = new System.Drawing.Size (185, 22);
            toolStripMenuItem13.Text = "Empty Directory";
            toolStripMenuItem13.Click += new System.EventHandler (emptyDirectoryToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.ShortcutKeyDisplayString = "Ins";
            toolStripMenuItem2.Size = new System.Drawing.Size (185, 22);
            toolStripMenuItem2.Text = "&File(s)...";
            toolStripMenuItem2.Click += new System.EventHandler (addFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem14
            // 
            toolStripMenuItem14.Name = "toolStripMenuItem14";
            toolStripMenuItem14.Size = new System.Drawing.Size (185, 22);
            toolStripMenuItem14.Text = "DB file from TSV";
            toolStripMenuItem14.Click += new System.EventHandler (dBFileFromTSVToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.ShortcutKeyDisplayString = "Del";
            toolStripMenuItem4.Size = new System.Drawing.Size (152, 22);
            toolStripMenuItem4.Text = "Delete";
            toolStripMenuItem4.Click += new System.EventHandler (deleteFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new System.Drawing.Size (152, 22);
            toolStripMenuItem5.Text = "Rename";
            toolStripMenuItem5.Click += new System.EventHandler (renameToolStripMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            toolStripSeparator10.Name = "toolStripSeparator10";
            toolStripSeparator10.Size = new System.Drawing.Size (149, 6);
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            toolStripMenuItem7,
            toolStripMenuItem8});
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new System.Drawing.Size (152, 22);
            toolStripMenuItem6.Text = "Open";
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            toolStripMenuItem7.Size = new System.Drawing.Size (199, 22);
            toolStripMenuItem7.Text = "Open External...";
            toolStripMenuItem7.Click += new System.EventHandler (openFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem8
            // 
            toolStripMenuItem8.Name = "toolStripMenuItem8";
            toolStripMenuItem8.Size = new System.Drawing.Size (199, 22);
            toolStripMenuItem8.Text = "Open as Text";
            toolStripMenuItem8.Click += new System.EventHandler (openAsTextMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            toolStripMenuItem9.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            toolStripMenuItem10,
            toolStripMenuItem11,
            toolStripMenuItem12});
            toolStripMenuItem9.Name = "toolStripMenuItem9";
            toolStripMenuItem9.Size = new System.Drawing.Size (152, 22);
            toolStripMenuItem9.Text = "Extract";
            // 
            // toolStripMenuItem10
            // 
            toolStripMenuItem10.Name = "toolStripMenuItem10";
            toolStripMenuItem10.ShortcutKeyDisplayString = "Ctl+X";
            toolStripMenuItem10.Size = new System.Drawing.Size (202, 22);
            toolStripMenuItem10.Text = "Extract &Selected...";
            toolStripMenuItem10.Click += new System.EventHandler (extractSelectedToolStripMenuItem_Click);
            // 
            // toolStripMenuItem11
            // 
            toolStripMenuItem11.Name = "toolStripMenuItem11";
            toolStripMenuItem11.Size = new System.Drawing.Size (202, 22);
            toolStripMenuItem11.Text = "Extract &All...";
            toolStripMenuItem11.Click += new System.EventHandler (extractAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem12
            // 
            toolStripMenuItem12.Name = "toolStripMenuItem12";
            toolStripMenuItem12.Size = new System.Drawing.Size (202, 22);
            toolStripMenuItem12.Text = "Extract Unknown...";
            toolStripMenuItem12.Click += new System.EventHandler (exportUnknownToolStripMenuItem_Click);
            // 
            // addToolStripMenuItem
            // 
            addToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            emptyDirectoryToolStripMenuItem,
            addDirectoryToolStripMenuItem,
            addFileToolStripMenuItem,
            dBFileFromTSVToolStripMenuItem});
            addToolStripMenuItem.Name = "addToolStripMenuItem";
            addToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            addToolStripMenuItem.Text = "Add";
            // 
            // emptyDirectoryToolStripMenuItem
            // 
            emptyDirectoryToolStripMenuItem.Name = "emptyDirectoryToolStripMenuItem";
            emptyDirectoryToolStripMenuItem.Size = new System.Drawing.Size (185, 22);
            emptyDirectoryToolStripMenuItem.Text = "Empty Directory";
            emptyDirectoryToolStripMenuItem.Click += new System.EventHandler (emptyDirectoryToolStripMenuItem_Click);
            // 
            // addDirectoryToolStripMenuItem
            // 
            addDirectoryToolStripMenuItem.Name = "addDirectoryToolStripMenuItem";
            addDirectoryToolStripMenuItem.ShortcutKeyDisplayString = "Shift+Ins";
            addDirectoryToolStripMenuItem.Size = new System.Drawing.Size (185, 22);
            addDirectoryToolStripMenuItem.Text = "&Directory...";
            addDirectoryToolStripMenuItem.Click += new System.EventHandler (addDirectoryToolStripMenuItem_Click);
            // 
            // addFileToolStripMenuItem
            // 
            addFileToolStripMenuItem.Name = "addFileToolStripMenuItem";
            addFileToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
            addFileToolStripMenuItem.Size = new System.Drawing.Size (185, 22);
            addFileToolStripMenuItem.Text = "&File(s)...";
            addFileToolStripMenuItem.Click += new System.EventHandler (addFileToolStripMenuItem_Click);
            // 
            // dBFileFromTSVToolStripMenuItem
            // 
            dBFileFromTSVToolStripMenuItem.Name = "dBFileFromTSVToolStripMenuItem";
            dBFileFromTSVToolStripMenuItem.Size = new System.Drawing.Size (185, 22);
            dBFileFromTSVToolStripMenuItem.Text = "DB file from TSV";
            dBFileFromTSVToolStripMenuItem.Click += new System.EventHandler (dBFileFromTSVToolStripMenuItem_Click);
            // 
            // packOpenFileDialog
            // 
            packOpenFileDialog.Filter = "Package File|*.pack|Any File|*.*";
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            splitContainer1.Location = new System.Drawing.Point (-2, 27);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add (packTreeView);
            splitContainer1.Size = new System.Drawing.Size (909, 603);
            splitContainer1.SplitterDistance = 202;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 9;
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem[] {
            fileToolStripMenuItem,
            filesMenu,
            editToolStripMenuItem,
            updateToolStripMenuItem,
            extrasToolStripMenuItem,
            helpToolStripMenuItem});
            menuStrip.Location = new System.Drawing.Point (0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new System.Drawing.Size (906, 24);
            menuStrip.TabIndex = 10;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            newToolStripMenuItem,
            openToolStripMenuItem,
            toolStripSeparator,
            saveToolStripMenuItem,
            saveAsToolStripMenuItem,
            toolStripSeparator2,
            changePackTypeToolStripMenuItem,
            toolStripSeparator9,
            exportFileListToolStripMenuItem,
            toolStripSeparator7,
            exitToolStripMenuItem});
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size (37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            newToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            newToolStripMenuItem.Text = "&New";
            newToolStripMenuItem.Click += new System.EventHandler (newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            openToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            openToolStripMenuItem.Text = "&Open...";
            openToolStripMenuItem.Click += new System.EventHandler (openToolStripMenuItem_Click);
            // 
            // toolStripSeparator
            // 
            toolStripSeparator.Name = "toolStripSeparator";
            toolStripSeparator.Size = new System.Drawing.Size (169, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            saveToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            saveToolStripMenuItem.Text = "&Save";
            saveToolStripMenuItem.Click += new System.EventHandler (saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            saveAsToolStripMenuItem.Text = "Save &As...";
            saveAsToolStripMenuItem.Click += new System.EventHandler (saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size (169, 6);
            // 
            // changePackTypeToolStripMenuItem
            // 
            changePackTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            bootToolStripMenuItem,
                bootXToolStripMenuItem,
            releaseToolStripMenuItem,
            patchToolStripMenuItem,
            modToolStripMenuItem,
            movieToolStripMenuItem,
                shaderToolStripMenuItem,
                shader2ToolStripMenuItem
            });
            changePackTypeToolStripMenuItem.Name = "changePackTypeToolStripMenuItem";
            changePackTypeToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            changePackTypeToolStripMenuItem.Text = "Change Pack &Type";
            changePackTypeToolStripMenuItem.Enabled = false;
            // 
            // bootToolStripMenuItem
            // 
            bootToolStripMenuItem.CheckOnClick = true;
            bootToolStripMenuItem.Name = "bootToolStripMenuItem";
            bootToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            bootToolStripMenuItem.Text = "Boot";
            bootToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            //
            // bootXToolStripMenuItem
            //
            bootXToolStripMenuItem.CheckOnClick = true;
            bootXToolStripMenuItem.Name = "bootXToolStripMenuItem";
            bootXToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            bootXToolStripMenuItem.Text = "BootX";
            bootXToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            //
            // releaseToolStripMenuItem
            // 
            releaseToolStripMenuItem.CheckOnClick = true;
            releaseToolStripMenuItem.Name = "releaseToolStripMenuItem";
            releaseToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            releaseToolStripMenuItem.Text = "Release";
            releaseToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            // 
            // patchToolStripMenuItem
            // 
            patchToolStripMenuItem.CheckOnClick = true;
            patchToolStripMenuItem.Name = "patchToolStripMenuItem";
            patchToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            patchToolStripMenuItem.Text = "Patch";
            patchToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            // 
            // modToolStripMenuItem
            // 
            modToolStripMenuItem.Name = "modToolStripMenuItem";
            modToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            modToolStripMenuItem.Text = "Mod";
            modToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            // 
            // movieToolStripMenuItem
            // 
            movieToolStripMenuItem.CheckOnClick = true;
            movieToolStripMenuItem.Name = "movieToolStripMenuItem";
            movieToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            movieToolStripMenuItem.Text = "Movie";
            movieToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            //
            // shaderToolStripMenuItem
            //
            shaderToolStripMenuItem.CheckOnClick = true;
            shaderToolStripMenuItem.Name = "shaderToolStripMenuItem";
            shaderToolStripMenuItem.Size = new System.Drawing.Size (113, 22);
            shaderToolStripMenuItem.Text = "Shader";
            shaderToolStripMenuItem.Click += new System.EventHandler (packTypeToolStripMenuItem_Click);
            //
            // shader2ToolStripMenuItem
            //
            shader2ToolStripMenuItem.CheckOnClick = true;
            shader2ToolStripMenuItem.Name = "shader2ToolStripMenuItem";
            shader2ToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            shader2ToolStripMenuItem.Text = "Shader2";
            shader2ToolStripMenuItem.Click += new System.EventHandler(packTypeToolStripMenuItem_Click);
            //
            // toolStripSeparator9
            // 
            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new System.Drawing.Size (169, 6);
            // 
            // exportFileListToolStripMenuItem
            // 
            exportFileListToolStripMenuItem.Name = "exportFileListToolStripMenuItem";
            exportFileListToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            exportFileListToolStripMenuItem.Text = "Export File &List...";
            exportFileListToolStripMenuItem.Click += new System.EventHandler (exportFileListToolStripMenuItem_Click);
            exportFileListToolStripMenuItem.Enabled = false;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new System.Drawing.Size (169, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size (172, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += new System.EventHandler (exitToolStripMenuItem_Click);
            // 
            // filesMenu
            // 
            filesMenu.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            addToolStripMenuItem,
            deleteFileToolStripMenuItem,
            replaceFileToolStripMenuItem,
            renameToolStripMenuItem,
            toolStripSeparator4,
            openToolStripMenuItem1,
            extractToolStripMenuItem,
            toolStripSeparator8,
            createReadMeToolStripMenuItem,
            searchFileToolStripMenuItem});
            filesMenu.Enabled = false;
            filesMenu.Name = "filesMenu";
            filesMenu.Size = new System.Drawing.Size (42, 20);
            filesMenu.Text = "Files";
            // 
            // deleteFileToolStripMenuItem
            // 
            deleteFileToolStripMenuItem.Name = "deleteFileToolStripMenuItem";
            deleteFileToolStripMenuItem.ShortcutKeyDisplayString = "Del";
            deleteFileToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            deleteFileToolStripMenuItem.Text = "Delete";
            deleteFileToolStripMenuItem.Click += new System.EventHandler (deleteFileToolStripMenuItem_Click);
            // 
            // replaceFileToolStripMenuItem
            // 
            replaceFileToolStripMenuItem.Name = "replaceFileToolStripMenuItem";
            replaceFileToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            replaceFileToolStripMenuItem.Text = "&Replace File...";
            replaceFileToolStripMenuItem.Click += new System.EventHandler (replaceFileToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem
            // 
            renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            renameToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            renameToolStripMenuItem.Text = "Rename";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new System.Drawing.Size (151, 6);
            // 
            // openToolStripMenuItem1
            // 
            openToolStripMenuItem1.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            openFileToolStripMenuItem,
            openAsTextMenuItem});
            openToolStripMenuItem1.Name = "openToolStripMenuItem1";
            openToolStripMenuItem1.Size = new System.Drawing.Size (154, 22);
            openToolStripMenuItem1.Text = "Open";
            // 
            // openFileToolStripMenuItem
            // 
            openFileToolStripMenuItem.Name = "openFileToolStripMenuItem";
            openFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            openFileToolStripMenuItem.Size = new System.Drawing.Size (199, 22);
            openFileToolStripMenuItem.Text = "Open External...";
            openFileToolStripMenuItem.Click += new System.EventHandler (openFileToolStripMenuItem_Click);
            // 
            // openAsTextMenuItem
            // 
            openAsTextMenuItem.Name = "openAsTextMenuItem";
            openAsTextMenuItem.Size = new System.Drawing.Size (199, 22);
            openAsTextMenuItem.Text = "Open as Text";
            openAsTextMenuItem.Click += new System.EventHandler (openAsTextMenuItem_Click);
            // 
            // extractToolStripMenuItem
            // 
            extractToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            extractSelectedToolStripMenuItem,
            extractAllToolStripMenuItem,
            exportUnknownToolStripMenuItem});
            extractToolStripMenuItem.Name = "extractToolStripMenuItem";
            extractToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            extractToolStripMenuItem.Text = "Extract";
            // 
            // extractSelectedToolStripMenuItem
            // 
            extractSelectedToolStripMenuItem.Name = "extractSelectedToolStripMenuItem";
            extractSelectedToolStripMenuItem.ShortcutKeyDisplayString = "Ctl+X";
            extractSelectedToolStripMenuItem.Size = new System.Drawing.Size (202, 22);
            extractSelectedToolStripMenuItem.Text = "Extract &Selected...";
            extractSelectedToolStripMenuItem.Click += new System.EventHandler (extractSelectedToolStripMenuItem_Click);
            // 
            // extractAllToolStripMenuItem
            // 
            extractAllToolStripMenuItem.Name = "extractAllToolStripMenuItem";
            extractAllToolStripMenuItem.Size = new System.Drawing.Size (202, 22);
            extractAllToolStripMenuItem.Text = "Extract &All...";
            extractAllToolStripMenuItem.Click += new System.EventHandler (extractAllToolStripMenuItem_Click);
            // 
            // exportUnknownToolStripMenuItem
            // 
            exportUnknownToolStripMenuItem.Name = "exportUnknownToolStripMenuItem";
            exportUnknownToolStripMenuItem.Size = new System.Drawing.Size (202, 22);
            exportUnknownToolStripMenuItem.Text = "Extract Unknown...";
            exportUnknownToolStripMenuItem.Click += new System.EventHandler (exportUnknownToolStripMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new System.Drawing.Size (151, 6);
            // 
            // createReadMeToolStripMenuItem
            // 
            createReadMeToolStripMenuItem.Enabled = false;
            createReadMeToolStripMenuItem.Name = "createReadMeToolStripMenuItem";
            createReadMeToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            createReadMeToolStripMenuItem.Text = "Create ReadMe";
            createReadMeToolStripMenuItem.Click += new System.EventHandler (createReadMeToolStripMenuItem_Click);
            // 
            // searchFileToolStripMenuItem
            // 
            searchFileToolStripMenuItem.Name = "searchFileToolStripMenuItem";
            searchFileToolStripMenuItem.Size = new System.Drawing.Size (154, 22);
            searchFileToolStripMenuItem.Text = "Search Files...";
            searchFileToolStripMenuItem.Click += new System.EventHandler (searchFileToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            undoToolStripMenuItem,
            redoToolStripMenuItem,
            toolStripSeparator3,
            cutToolStripMenuItem,
            copyToolStripMenuItem,
            pasteToolStripMenuItem,
            toolStripSeparator6,
            selectAllToolStripMenuItem});
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new System.Drawing.Size (39, 20);
            editToolStripMenuItem.Text = "&Edit";
            editToolStripMenuItem.Visible = false;
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            undoToolStripMenuItem.Size = new System.Drawing.Size (144, 22);
            undoToolStripMenuItem.Text = "&Undo";
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            redoToolStripMenuItem.Size = new System.Drawing.Size (144, 22);
            redoToolStripMenuItem.Text = "&Redo";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size (141, 6);
            // 
            // cutToolStripMenuItem
            // 
            cutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            cutToolStripMenuItem.Size = new System.Drawing.Size (144, 22);
            cutToolStripMenuItem.Text = "Cu&t";
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            copyToolStripMenuItem.Size = new System.Drawing.Size (144, 22);
            copyToolStripMenuItem.Text = "&Copy";
            // 
            // pasteToolStripMenuItem
            // 
            pasteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
            pasteToolStripMenuItem.Size = new System.Drawing.Size (144, 22);
            pasteToolStripMenuItem.Text = "&Paste";
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new System.Drawing.Size (141, 6);
            // 
            // selectAllToolStripMenuItem
            // 
            selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            selectAllToolStripMenuItem.Size = new System.Drawing.Size (144, 22);
            selectAllToolStripMenuItem.Text = "Select &All";
            // 
            // updateToolStripMenuItem
            // 
            updateToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            searchForUpdateToolStripMenuItem,
            fromXsdFileToolStripMenuItem,
            reloadToolStripMenuItem,
            saveToDirectoryToolStripMenuItem,
            updateDBFilesToolStripMenuItem});
            updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            updateToolStripMenuItem.Size = new System.Drawing.Size (102, 20);
            updateToolStripMenuItem.Text = "DB Descriptions";
            // 
            // searchForUpdateToolStripMenuItem
            // 
            searchForUpdateToolStripMenuItem.Name = "searchForUpdateToolStripMenuItem";
            searchForUpdateToolStripMenuItem.Size = new System.Drawing.Size (221, 22);
            searchForUpdateToolStripMenuItem.Text = "Search for Update";
            searchForUpdateToolStripMenuItem.Click += new System.EventHandler (updateToolStripMenuItem_Click);
            // 
            // fromXsdFileToolStripMenuItem
            // 
            fromXsdFileToolStripMenuItem.Enabled = false;
            fromXsdFileToolStripMenuItem.Name = "fromXsdFileToolStripMenuItem";
            fromXsdFileToolStripMenuItem.Size = new System.Drawing.Size (221, 22);
            fromXsdFileToolStripMenuItem.Text = "Load from xsd File";
            fromXsdFileToolStripMenuItem.Visible = false;
            fromXsdFileToolStripMenuItem.Click += new System.EventHandler (fromXsdFileToolStripMenuItem_Click);
            // 
            // reloadToolStripMenuItem
            // 
            reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            reloadToolStripMenuItem.Size = new System.Drawing.Size (221, 22);
            reloadToolStripMenuItem.Text = "Reload from Local Directory";
            reloadToolStripMenuItem.Visible = false;
            reloadToolStripMenuItem.Click += new System.EventHandler (reloadToolStripMenuItem_Click);
            // 
            // saveToDirectoryToolStripMenuItem
            // 
            saveToDirectoryToolStripMenuItem.Enabled = false;
            saveToDirectoryToolStripMenuItem.Name = "saveToDirectoryToolStripMenuItem";
            saveToDirectoryToolStripMenuItem.Size = new System.Drawing.Size (221, 22);
            saveToDirectoryToolStripMenuItem.Text = "Save to Directory";
            saveToDirectoryToolStripMenuItem.Click += new System.EventHandler (saveToDirectoryToolStripMenuItem_Click);
            // 
            // updateDBFilesToolStripMenuItem
            // 
            updateDBFilesToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            updateCurrentToolStripMenuItem,
            updateAllToolStripMenuItem});
            updateDBFilesToolStripMenuItem.Name = "updateDBFilesToolStripMenuItem";
            updateDBFilesToolStripMenuItem.Size = new System.Drawing.Size (221, 22);
            updateDBFilesToolStripMenuItem.Text = "Update DB Files";
            // 
            // updateCurrentToolStripMenuItem
            // 
            updateCurrentToolStripMenuItem.Name = "updateCurrentToolStripMenuItem";
            updateCurrentToolStripMenuItem.Size = new System.Drawing.Size (155, 22);
            updateCurrentToolStripMenuItem.Text = "Update Current";
            updateCurrentToolStripMenuItem.Click += new System.EventHandler (updateCurrentToolStripMenuItem_Click);
            // 
            // updateAllToolStripMenuItem
            // 
            updateAllToolStripMenuItem.Name = "updateAllToolStripMenuItem";
            updateAllToolStripMenuItem.Size = new System.Drawing.Size (155, 22);
            updateAllToolStripMenuItem.Text = "Update All";
            updateAllToolStripMenuItem.Click += new System.EventHandler (updateAllToolStripMenuItem_Click);
            // 
            // extrasToolStripMenuItem
            // 
            extrasToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            cAPacksAreReadOnlyToolStripMenuItem,
            updateOnStartupToolStripMenuItem});
            extrasToolStripMenuItem.Name = "extrasToolStripMenuItem";
            extrasToolStripMenuItem.Size = new System.Drawing.Size (61, 20);
            extrasToolStripMenuItem.Text = "Options";
            // 
            // cAPacksAreReadOnlyToolStripMenuItem
            // 
            cAPacksAreReadOnlyToolStripMenuItem.Checked = true;
            cAPacksAreReadOnlyToolStripMenuItem.CheckOnClick = true;
            cAPacksAreReadOnlyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            cAPacksAreReadOnlyToolStripMenuItem.Name = "cAPacksAreReadOnlyToolStripMenuItem";
            cAPacksAreReadOnlyToolStripMenuItem.Size = new System.Drawing.Size (201, 22);
            cAPacksAreReadOnlyToolStripMenuItem.Text = "CA Packs Are Read Only";
            cAPacksAreReadOnlyToolStripMenuItem.ToolTipText = "If checked, the original pack files for the game can be viewed but not edited.";
            // 
            // updateOnStartupToolStripMenuItem
            // 
            updateOnStartupToolStripMenuItem.CheckOnClick = true;
            updateOnStartupToolStripMenuItem.Name = "updateOnStartupToolStripMenuItem";
            updateOnStartupToolStripMenuItem.Size = new System.Drawing.Size (201, 22);
            updateOnStartupToolStripMenuItem.Text = "Update on Startup";
            updateOnStartupToolStripMenuItem.Click += new System.EventHandler (updateOnStartupToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem[] {
            contentsToolStripMenuItem,
            indexToolStripMenuItem,
            searchToolStripMenuItem,
            toolStripSeparator5,
            aboutToolStripMenuItem});
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size (44, 20);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // contentsToolStripMenuItem
            // 
            contentsToolStripMenuItem.Name = "contentsToolStripMenuItem";
            contentsToolStripMenuItem.Size = new System.Drawing.Size (122, 22);
            contentsToolStripMenuItem.Text = "&Contents";
            contentsToolStripMenuItem.Visible = false;
            // 
            // indexToolStripMenuItem
            // 
            indexToolStripMenuItem.Name = "indexToolStripMenuItem";
            indexToolStripMenuItem.Size = new System.Drawing.Size (122, 22);
            indexToolStripMenuItem.Text = "&Index";
            indexToolStripMenuItem.Visible = false;
            // 
            // searchToolStripMenuItem
            // 
            searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            searchToolStripMenuItem.Size = new System.Drawing.Size (122, 22);
            searchToolStripMenuItem.Text = "&Search";
            searchToolStripMenuItem.Visible = false;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new System.Drawing.Size (119, 6);
            toolStripSeparator5.Visible = false;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size (122, 22);
            aboutToolStripMenuItem.Text = "&About...";
            aboutToolStripMenuItem.Click += new System.EventHandler (aboutToolStripMenuItem_Click);
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem[] {
            packStatusLabel,
            packActionProgressBar});
            statusStrip.Location = new System.Drawing.Point (0, 628);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new System.Drawing.Size (906, 22);
            statusStrip.TabIndex = 11;
            statusStrip.Text = "statusStrip1";
            // 
            // packStatusLabel
            // 
            packStatusLabel.Name = "packStatusLabel";
            packStatusLabel.Size = new System.Drawing.Size (769, 17);
            packStatusLabel.Spring = true;
            packStatusLabel.Text = "Use the File menu to create a new pack file or open an existing one.";
            packStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // packActionProgressBar
            // 
            packActionProgressBar.Name = "packActionProgressBar";
            packActionProgressBar.Size = new System.Drawing.Size (120, 16);
            // 
            // extractFolderBrowserDialog
            // 
            extractFolderBrowserDialog.Description = "Extract to what folder?";
            // 
            // choosePathAnchorFolderBrowserDialog
            // 
            choosePathAnchorFolderBrowserDialog.Description = "Make packed files relative to which directory?";
            // 
            // addDirectoryFolderBrowserDialog
            // 
            addDirectoryFolderBrowserDialog.Description = "Add which directory?";
            // 
            // openDBFileDialog
            // 
            openDBFileDialog.Filter = "Text CSV|*.txt|Any File|*.*";
            // 
            // PackFileManagerForm
            // 
            AutoScroll = true;
            BackColor = System.Drawing.SystemColors.ButtonFace;
            ClientSize = new System.Drawing.Size (906, 650);
            Controls.Add (statusStrip);
            Controls.Add (splitContainer1);
            Controls.Add (menuStrip);
            Font = new System.Drawing.Font ("Microsoft Sans Serif", 8F);
            Location = new System.Drawing.Point (192, 114);
            MainMenuStrip = menuStrip;
            Name = "PackFileManagerForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Pack File Manager 10.0.40219.1 BETA (Total War - Shogun 2)";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            Activated += new System.EventHandler (PackFileManagerForm_Activated);
            FormClosing += new System.Windows.Forms.FormClosingEventHandler (PackFileManagerForm_FormClosing);
            Load += new System.EventHandler (PackFileManagerForm_Load);
            Shown += new System.EventHandler (PackFileManagerForm_Shown);
            packActionMenuStrip.ResumeLayout (false);
            splitContainer1.Panel1.ResumeLayout (false);
            // ((System.ComponentModel.ISupportInitialize)(splitContainer1)).EndInit();
            splitContainer1.ResumeLayout (false);
            menuStrip.ResumeLayout (false);
            menuStrip.PerformLayout ();
            statusStrip.ResumeLayout (false);
            statusStrip.PerformLayout ();
            ResumeLayout (false);
            PerformLayout ();

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

            // default: current directory
            string initialDialog = Directory.GetCurrentDirectory ();
            try {

                // try to determine the shogun install path and use the data directory below
                initialDialog = IOFunctions.GetShogunTotalWarDirectory ();
                if (!string.IsNullOrEmpty (initialDialog)) {
                    initialDialog = Path.Combine (initialDialog, "data");
                } else {
                    // go through the arguments (interpreted as file names)
                    // and use the first for which the directory exists
                    foreach (string file in args) {
                        string dir = Path.GetDirectoryName (file);
                        if (File.Exists (dir)) {
                            initialDialog = dir;
                            break;
                        }
                    }
                }
            } catch {
                // we have not set an invalid path along the way; should still be current dir here
            }
            // set to the dialogs
            saveFileDialog.InitialDirectory = initialDialog;
            addDirectoryFolderBrowserDialog.SelectedPath = initialDialog;
            extractFolderBrowserDialog.SelectedPath = initialDialog;
        }


        private void PackFileManagerForm_Load(object sender, EventArgs e) {
            base.TopMost = true;
        }

        private void PackFileManagerForm_Shown(object sender, EventArgs e) {
            base.TopMost = false;
        }
        #endregion

        private void exportFileListToolStripMenuItem_Click(object sender, EventArgs e) {
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(currentPackFile.Filepath) + ".pack-file-list.txt";
            if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName)) {
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
            extractFiles(packedFiles);
        }

        private void extractFiles(List<PackedFile> packedFiles)
        {
            if (extractFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = extractFolderBrowserDialog.SelectedPath;
                FileAlreadyExistsDialog.DefaultAction ask = FileAlreadyExistsDialog.DefaultAction.Ask;
                packStatusLabel.Text = string.Format("Extracting file (0 of {0} files extracted, 0 skipped)", packedFiles.Count);
                packActionProgressBar.Visible = true;
                packActionProgressBar.Minimum = 0;
                packActionProgressBar.Maximum = packedFiles.Count;
                packActionProgressBar.Step = 1;
                packActionProgressBar.Value = 0;
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
                                    packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                                    packActionProgressBar.PerformStep();
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
                                packStatusLabel.Text = "Extraction cancelled.";
                                packActionProgressBar.Visible = false;
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
                                packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                                packActionProgressBar.PerformStep();
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
                    packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                    Application.DoEvents();
                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        stream.Write(file.Data, 0, (int) file.Size);
                    }
                    num++;
                    packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                    packActionProgressBar.PerformStep();
                    Application.DoEvents();
                }
            }
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
            extractFiles(packedFiles);
        }
        #endregion

        protected void EnableMenuItems() {
            saveToDirectoryToolStripMenuItem.Enabled = currentPackFile != null && !CanWriteCurrentPack && CurrentPackFile.IsModified;
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
            var header = new PFHeader("PFH3") {
                Type = PackType.Mod,
                Version = 0,
                FileCount = 0,
                ReplacedPackFileName = "",
                DataStart = 0x20
            };
            CurrentPackFile = new PackFile("Untitled.pack", header);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            if ((handlePackFileChangesWithUserInput() != DialogResult.Cancel) && (packOpenFileDialog.ShowDialog() == DialogResult.OK)) {
                OpenExistingPackFile(packOpenFileDialog.FileName);
            }
        }

        private void OpenExistingPackFile(string filepath)
        {
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
                locFileEditorControl.updatePackedFile();
            }
            if (atlasFileEditorControl != null) {
                atlasFileEditorControl.updatePackedFile();
            }
            if (unitVariantFileEditorControl != null) {
                unitVariantFileEditorControl.updatePackedFile();
            }
            if (readmeEditorControl != null) {
                readmeEditorControl.updatePackedFile();
            }
            if (textFileEditorControl != null) {
                textFileEditorControl.updatePackedFile();
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
                    AddExtension = true,
                    Filter = "Pack File|*.pack"
                };
                if (dialog.ShowDialog() == DialogResult.OK) {
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
            Settings.Default.UpdateOnStartup = updateOnStartupToolStripMenuItem.Checked;
            Settings.Default.Save();
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
                    int maxVersion = DBTypeMap.Instance.MaxVersion(key);
                    DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
                    if (header.Version < maxVersion) {
                        // found a more recent db definition; read data from db file
                        DBFile updatedFile = new PackedFileDbCodec().readDbFile(packedFile);

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
            var packedFiles = new List<PackedFile>();
            foreach (TreeNode node in packTreeView.Nodes) {
                if (node.Nodes.Count > 0) {
                    getPackedFilesFromBranch(packedFiles, node.Nodes, unknownDbFormat);
                } else {
                    packedFiles.Add(node.Tag as PackedFile);
                }
            }
            extractFiles(packedFiles);
        }
        #endregion


        private void packTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Proceed with the drag-and-drop, passing the selected items for 
            if (e.Button == MouseButtons.Left && e.Item is TreeNode && e.Item != null &&
                ((TreeNode) e.Item).Tag is PackedFile && ((TreeNode) e.Item).Tag != null)
            {
                var file = ((TreeNode) e.Item).Tag as PackedFile;
                if (file != null)
                {
                    var dataObject = new DataObject();
                    var filesInfo = new DragFileInfo(file.FullPath, file.Size);

                    using (MemoryStream infoStream = DragDropHelper.GetFileDescriptor(filesInfo),
                                        contentStream = DragDropHelper.GetFileContents(file.Data))
                    {
                        dataObject.SetData(DragDropHelper.CFSTR_FILEDESCRIPTORW, infoStream);
                        dataObject.SetData(DragDropHelper.CFSTR_FILECONTENTS, contentStream);
                        dataObject.SetData(DragDropHelper.CFSTR_PERFORMEDDROPEFFECT, null);

                        DoDragDrop(dataObject, DragDropEffects.All);
                    }
                }
            }
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

