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
    public class PackFileManagerForm : Form {

        #region Members
        private ToolStripMenuItem aboutToolStripMenuItem;
        private IContainer components;
        private ToolStripMenuItem contentsToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private PackFile currentPackFile;
        private ToolStripMenuItem cutToolStripMenuItem;

        private AtlasFileEditorControl atlasFileEditorControl;
        private readonly DBFileEditorControl dbFileEditorControl;
        private ImageViewerControl imageViewerControl;
        private LocFileEditorControl locFileEditorControl;
        private EditEsfComponent esfEditor = new EditEsfComponent {
            Dock = DockStyle.Fill
        };

        private MenuStrip menuStrip;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem indexToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private bool openFileIsModified;
        private string openFilePath;
        private FileSystemWatcher openFileWatcher;
        private PackedFile openPackedFile;
        private ToolStripMenuItem openToolStripMenuItem;
        private ContextMenuStrip packActionMenuStrip;
        private ToolStripProgressBar packActionProgressBar;
        private ToolStripStatusLabel packStatusLabel;
        public TreeView packTreeView;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ReadmeEditorControl readmeEditorControl;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
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
        private ToolStripMenuItem showDecodeToolOnErrorToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem modsToolStripMenuItem;
        private ToolStripMenuItem newModMenuItem;
        private ToolStripSeparator toolStripSeparator11;
        private ToolStripMenuItem editModMenuItem;
        private ToolStripMenuItem deleteCurrentToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator12;
        private UnitVariantFileEditorControl unitVariantFileEditorControl;
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

            ModManager.Instance.ModNames.ForEach(name => modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(name)));
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
                        string addBase = "" + Path.DirectorySeparatorChar;
                        string modDir = ModManager.Instance.CurrentModDirectory;
                        if (!string.IsNullOrEmpty(modDir) && file.StartsWith(modDir)) {
                            Uri baseUri = new Uri(ModManager.Instance.CurrentModDirectory);
                            Uri createPath = baseUri.MakeRelativeUri(new Uri(file));
                            addBase = createPath.ToString().Replace('/', Path.DirectorySeparatorChar);
                            addBase = addBase.Remove(0, addBase.IndexOf(Path.DirectorySeparatorChar)+1);
                        }
                        AddTo.Add(addBase, new PackedFile(file));
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
            FolderBrowserDialog addDirectoryFolderBrowserDialog = new FolderBrowserDialog() {
                Description = "Add which directory?",
                SelectedPath = Settings.Default.ImportExportDirectory
            };
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
            this.components = new System.ComponentModel.Container();
            this.packTreeView = new System.Windows.Forms.TreeView();
            this.packActionMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem14 = new System.Windows.Forms.ToolStripMenuItem();
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
            this.emptyDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dBFileFromTSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.modsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newModMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.editModMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteCurrentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
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
            this.showDecodeToolOnErrorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.indexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.packStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.packActionProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
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
            this.packTreeView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.packTreeView_ItemDrag);
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
            this.packActionMenuStrip.Size = new System.Drawing.Size(132, 120);
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
            this.toolStripMenuItem1.Size = new System.Drawing.Size(131, 22);
            this.toolStripMenuItem1.Text = "Add";
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
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.ShortcutKeyDisplayString = "Ins";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem2.Text = "&File(s)...";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem14
            // 
            this.toolStripMenuItem14.Name = "toolStripMenuItem14";
            this.toolStripMenuItem14.Size = new System.Drawing.Size(185, 22);
            this.toolStripMenuItem14.Text = "DB file from TSV";
            this.toolStripMenuItem14.Click += new System.EventHandler(this.dBFileFromTSVToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.ShortcutKeyDisplayString = "Del";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(131, 22);
            this.toolStripMenuItem4.Text = "Delete";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(131, 22);
            this.toolStripMenuItem5.Text = "Rename";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(128, 6);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem7,
            this.toolStripMenuItem8});
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(131, 22);
            this.toolStripMenuItem6.Text = "Open";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(156, 22);
            this.toolStripMenuItem7.Text = "Open External...";
            this.toolStripMenuItem7.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(156, 22);
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
            this.toolStripMenuItem9.Size = new System.Drawing.Size(131, 22);
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
            // dBFileFromTSVToolStripMenuItem
            // 
            this.dBFileFromTSVToolStripMenuItem.Name = "dBFileFromTSVToolStripMenuItem";
            this.dBFileFromTSVToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.dBFileFromTSVToolStripMenuItem.Text = "DB file from TSV";
            this.dBFileFromTSVToolStripMenuItem.Click += new System.EventHandler(this.dBFileFromTSVToolStripMenuItem_Click);
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
            this.toolStripSeparator1,
            this.modsToolStripMenuItem,
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(169, 6);
            // 
            // modsToolStripMenuItem
            // 
            this.modsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newModMenuItem,
            this.toolStripSeparator11,
            this.editModMenuItem,
            this.deleteCurrentToolStripMenuItem,
            this.toolStripSeparator12});
            this.modsToolStripMenuItem.Name = "modsToolStripMenuItem";
            this.modsToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.modsToolStripMenuItem.Text = "Mods";
            // 
            // newModMenuItem
            // 
            this.newModMenuItem.Name = "newModMenuItem";
            this.newModMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newModMenuItem.Text = "New";
            this.newModMenuItem.Click += new System.EventHandler(this.newModMenuItem_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(149, 6);
            // 
            // editModMenuItem
            // 
            this.editModMenuItem.Name = "editModMenuItem";
            this.editModMenuItem.Size = new System.Drawing.Size(152, 22);
            this.editModMenuItem.Text = "Edit Current";
            this.editModMenuItem.Visible = false;
            // 
            // deleteCurrentToolStripMenuItem
            // 
            this.deleteCurrentToolStripMenuItem.Name = "deleteCurrentToolStripMenuItem";
            this.deleteCurrentToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.deleteCurrentToolStripMenuItem.Text = "Delete Current";
            this.deleteCurrentToolStripMenuItem.Click += new System.EventHandler(this.deleteCurrentToolStripMenuItem_Click);
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
            this.openFileToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.openFileToolStripMenuItem.Text = "Open External...";
            this.openFileToolStripMenuItem.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
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
            this.showDecodeToolOnErrorToolStripMenuItem});
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
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(149, 6);
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
            extractFiles(packedFiles);
        }

        private void extractFiles(List<PackedFile> packedFiles)
        {
            string exportDirectory = null;
            if (Settings.Default.CurrentMod == "") {
                FolderBrowserDialog extractFolderBrowserDialog = new FolderBrowserDialog {
                    Description = "Extract to what folder?",
                    SelectedPath = Settings.Default.LastPackDirectory
                };
                exportDirectory =
                    extractFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                    ? extractFolderBrowserDialog.SelectedPath : "";

            } else {
                exportDirectory = ModManager.Instance.CurrentModDirectory;
            }
            if (!string.IsNullOrEmpty(exportDirectory))
            {
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
                    string path = Path.Combine(exportDirectory, file.FullPath);
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
            NewMod("Untitled.pack");
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

        private void newModMenuItem_Click(object sender, EventArgs e) {
            List<string> oldMods = ModManager.Instance.ModNames;
            ModManager.Instance.AddMod();
            if (Settings.Default.CurrentMod != "") {
                ToolStrip strip = editModMenuItem.GetCurrentParent();
                if (!oldMods.Contains(Settings.Default.CurrentMod)) {
                    modsToolStripMenuItem.DropDownItems.Add(new ModMenuItem(Settings.Default.CurrentMod));
                    string packName = string.Format("{0}.pack", Settings.Default.CurrentMod);
                    NewMod(Path.Combine(ModManager.Instance.CurrentModDirectory, packName));
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

