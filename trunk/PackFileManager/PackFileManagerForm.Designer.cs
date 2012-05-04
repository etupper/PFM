namespace PackFileManager
{
    partial class PackFileManagerForm {
        private System.ComponentModel.IContainer components;

        #region Members
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
        private bool openFileIsModified;
        private string openFilePath;
        private System.IO.FileSystemWatcher openFileWatcher;
        private System.Windows.Forms.ToolStripMenuItem openCAToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip packActionMenuStrip;
        private System.Windows.Forms.ToolStripProgressBar packActionProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel packStatusLabel;
        public System.Windows.Forms.TreeView packTreeView;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private Common.ReadmeEditorControl readmeEditorControl;
        private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private customMessageBox search;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.StatusStrip statusStrip;
        private TextFileEditorControl textFileEditorControl;
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
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem openFileToolStripMenuItem;
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
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem openExternalToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem openTextToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem extractToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem extractSelectedToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem extractAllToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem extractUnknownToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem emptyDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem13;
        private System.Windows.Forms.ToolStripMenuItem dBFileFromTSVToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem14;
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
        private UnitVariantFileEditorControl unitVariantFileEditorControl;
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                Common.Utilities.DisposeHandlers(this);
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
            this.deleteToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.openExternalToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.openTextToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.extractToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.extractSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractSelectedToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.extractUnknownToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.openToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openAsTextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.toolStripMenuItem1,
            this.deleteToolStripMenuItem2,
            this.renameToolStripMenuItem2,
            this.toolStripSeparator10,
            this.toolStripMenuItem6,
            this.extractToolStripMenuItem2});
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
            this.deleteToolStripMenuItem2.Name = "toolStripMenuItem4";
            this.deleteToolStripMenuItem2.ShortcutKeyDisplayString = "Del";
            this.deleteToolStripMenuItem2.Size = new System.Drawing.Size(131, 22);
            this.deleteToolStripMenuItem2.Text = "Delete";
            this.deleteToolStripMenuItem2.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.renameToolStripMenuItem2.Name = "toolStripMenuItem5";
            this.renameToolStripMenuItem2.Size = new System.Drawing.Size(131, 22);
            this.renameToolStripMenuItem2.Text = "Rename";
            this.renameToolStripMenuItem2.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(128, 6);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openExternalToolStripMenuItem2,
            this.openTextToolStripMenuItem2});
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(131, 22);
            this.toolStripMenuItem6.Text = "Open";
            // 
            // toolStripMenuItem7
            // 
            this.openExternalToolStripMenuItem2.Name = "toolStripMenuItem7";
            this.openExternalToolStripMenuItem2.Size = new System.Drawing.Size(156, 22);
            this.openExternalToolStripMenuItem2.Text = "Open External...";
            this.openExternalToolStripMenuItem2.Click += new System.EventHandler(this.openFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem8
            // 
            this.openTextToolStripMenuItem2.Name = "toolStripMenuItem8";
            this.openTextToolStripMenuItem2.Size = new System.Drawing.Size(156, 22);
            this.openTextToolStripMenuItem2.Text = "Open as Text";
            this.openTextToolStripMenuItem2.Click += new System.EventHandler(this.openAsTextMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            this.extractToolStripMenuItem2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.extractSelectedToolStripMenuItem,
            this.extractAllToolStripMenuItem,
            this.extractUnknownToolStripMenuItem});
            this.extractToolStripMenuItem2.Name = "toolStripMenuItem9";
            this.extractToolStripMenuItem2.Size = new System.Drawing.Size(131, 22);
            this.extractToolStripMenuItem2.Text = "Extract";
            // 
            // toolStripMenuItem10
            // 
            this.extractSelectedToolStripMenuItem2.Name = "toolStripMenuItem10";
            this.extractSelectedToolStripMenuItem2.ShortcutKeyDisplayString = "Ctl+X";
            this.extractSelectedToolStripMenuItem2.Size = new System.Drawing.Size(202, 22);
            this.extractSelectedToolStripMenuItem2.Text = "Extract &Selected...";
            this.extractSelectedToolStripMenuItem2.Click += new System.EventHandler(this.extractSelectedToolStripMenuItem_Click);
            // 
            // toolStripMenuItem11
            // 
            this.extractAllToolStripMenuItem2.Name = "toolStripMenuItem11";
            this.extractAllToolStripMenuItem2.Size = new System.Drawing.Size(202, 22);
            this.extractAllToolStripMenuItem2.Text = "Extract &All...";
            this.extractAllToolStripMenuItem2.Click += new System.EventHandler(this.extractAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem12
            // 
            this.extractUnknownToolStripMenuItem.Name = "toolStripMenuItem12";
            this.extractUnknownToolStripMenuItem.Size = new System.Drawing.Size(202, 22);
            this.extractUnknownToolStripMenuItem.Text = "Extract Unknown...";
            this.extractUnknownToolStripMenuItem.Click += new System.EventHandler(this.exportUnknownToolStripMenuItem_Click);
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
            // openCaToolStripMenuItem
            // 
            this.openCAToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openCAToolStripMenuItem.Name = "openCAToolStripMenuItem";
            this.openCAToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.openCAToolStripMenuItem.Text = "Open CA pack...";
            this.openCAToolStripMenuItem.Enabled = false;
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
            // installModMenuItem
            // 
            this.installModMenuItem.Name = "installModMenuItem";
            this.installModMenuItem.Size = new System.Drawing.Size(152, 22);
            this.installModMenuItem.Text = "Install Current";
            this.installModMenuItem.Click += new System.EventHandler(this.installModMenuItem_Click);
            // 
            // uninstallModMenuItem
            // 
            this.uninstallModMenuItem.Name = "uninstallModMenuItem";
            this.uninstallModMenuItem.Size = new System.Drawing.Size(152, 22);
            this.uninstallModMenuItem.Text = "Uninstall Current";
            this.uninstallModMenuItem.Click += new System.EventHandler(this.uninstallModMenuItem_Click);
            // 
            // deleteCurrentToolStripMenuItem
            // 
            this.deleteCurrentToolStripMenuItem.Name = "deleteCurrentToolStripMenuItem";
            this.deleteCurrentToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.deleteCurrentToolStripMenuItem.Text = "Delete Current";
            this.deleteCurrentToolStripMenuItem.Click += new System.EventHandler(this.deleteCurrentToolStripMenuItem_Click);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(149, 6);
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
            this.exportUnknownToolStripMenuItem,
            this.extractAllTsv});
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

    }
}