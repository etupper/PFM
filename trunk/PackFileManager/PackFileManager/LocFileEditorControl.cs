namespace PackFileManager
{
    using Common;
    using DataGridViewAutoFilter;
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;
    using System.Xml;

    public class LocFileEditorControl : UserControl
    {
        private ToolStripButton addNewRowButton;
        private BindingSource bindingSource;
        private ToolStripButton cloneCurrentRow;
        private string[] columnHeaders = new string[] { "Tag", "Localised String", "Unknown" };
        private string[] columnNames = new string[] { "tag", "localised", "tooltip" };
        private int[] columnTypes;
        private int[] columnWidths;
        private IContainer components;
        private DataTable currentDataTable;
        private PackedFile currentPackedFile;
        public bool dataChanged;
        private DataGridView dataGridView;
        private ToolStripButton deleteCurrentRow;
        private ToolStripButton exportButton;
        private ToolStripButton importButton;
        private LocFile locFile;
        public OpenFileDialog openLocFileDialog;
        private ToolStrip toolStrip;
        private ToolStripSeparator toolStripSeparator1;

        public LocFileEditorControl(PackedFile packedFile)
        {
            int[] numArray = new int[3];
            numArray[2] = 1;
            this.columnTypes = numArray;
            this.columnWidths = new int[] { 200, 400, 20 };
            this.components = null;
            this.InitializeComponent();
            for (int i = 0; i < this.columnNames.Length; i++)
            {
                DataGridViewColumn column;
                int num2 = this.columnTypes[i];
                if (num2 == 1)
                {
                    column = new DataGridViewCheckBoxColumn();
                }
                else
                {
                    column = new DataGridViewAutoFilterTextBoxColumn();
                }
                column.DataPropertyName = this.columnNames[i];
                column.HeaderText = this.columnHeaders[i];
                column.Width = this.columnWidths[i];
                this.dataGridView.Columns.Add(column);
            }
            this.currentPackedFile = packedFile;
            this.locFile = new LocFile();
            this.locFile.setPackedFile(this.currentPackedFile);
            this.bindingSource = new BindingSource();
            this.currentDataTable = this.getData();
            this.bindingSource.DataSource = this.currentDataTable;
            this.dataGridView.DataSource = this.bindingSource;
        }

        private void addNewRowButton_Click(object sender, EventArgs e)
        {
            DataRow row = this.currentDataTable.NewRow();
            row[0] = "tag";
            row[1] = "localised string";
            row[2] = false;
            this.currentDataTable.Rows.Add(row);
            this.dataGridView.FirstDisplayedScrollingRowIndex = this.dataGridView.RowCount - 1;
            this.dataGridView.Rows[this.dataGridView.Rows.Count - 2].Selected = true;
            this.dataChanged = true;
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
        }

        private void cloneCurrentRow_Click(object sender, EventArgs e)
        {
            DataRow row = this.currentDataTable.NewRow();
            int num = (this.dataGridView.SelectedRows.Count == 1) ? this.dataGridView.SelectedRows[0].Index : this.dataGridView.SelectedCells[0].RowIndex;
            row[0] = this.dataGridView.Rows[num].Cells[0].Value;
            row[1] = this.dataGridView.Rows[num].Cells[1].Value;
            row[2] = this.dataGridView.Rows[num].Cells[2].Value;
            this.currentDataTable.Rows.Add(row);
            this.dataGridView.FirstDisplayedScrollingRowIndex = this.dataGridView.RowCount - 1;
            this.dataGridView.Rows[this.dataGridView.Rows.Count - 2].Selected = true;
            this.dataChanged = true;
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
        }

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            this.dataChanged = true;
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            this.cloneCurrentRow.Enabled = this.cloneCurrentRow.Enabled = (this.dataGridView.SelectedRows.Count == 1) || (this.dataGridView.SelectedCells.Count == 1);
            this.deleteCurrentRow.Enabled = this.cloneCurrentRow.Enabled = (this.dataGridView.SelectedRows.Count == 1) || (this.dataGridView.SelectedCells.Count == 1);
        }

        private void dataGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            this.dataChanged = true;
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
        }

        private void deleteCurrentRow_Click(object sender, EventArgs e)
        {
            int index = (this.dataGridView.SelectedRows.Count == 1) ? this.dataGridView.SelectedRows[0].Index : this.dataGridView.SelectedCells[0].RowIndex;
            this.currentDataTable.Rows.RemoveAt(index);
            this.dataChanged = true;
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
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

        private void exportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = this.locFile.name + ".tsv"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(dialog.FileName))
                {
                    this.locFile.Export(writer);
                }
            }
        }

        private DataTable getData()
        {
            int num;
            if (this.locFile == null)
            {
                return null;
            }
            DataTable table = new DataTable("locTable");
            for (num = 0; num < this.columnNames.Length; num++)
            {
                DataColumn column = new DataColumn(this.columnNames[num]);
                int num2 = this.columnTypes[num];
                if (num2 == 1)
                {
                    column.DataType = System.Type.GetType("System.Boolean");
                }
                else
                {
                    column.DataType = System.Type.GetType("System.String");
                }
                table.Columns.Add(column);
            }
            for (num = 0; num < this.locFile.numEntries; num++)
            {
                DataRow row = table.NewRow();
                row[0] = this.locFile.Entries[num].Tag;
                row[1] = this.locFile.Entries[num].Localised;
                row[2] = this.locFile.Entries[num].Tooltip;
                table.Rows.Add(row);
            }
            return table;
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            this.openLocFileDialog.FileName = this.locFile.name + ".tsv";
            if (this.openLocFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(this.openLocFileDialog.FileName))
                {
                    this.locFile.Import(reader);
                    this.currentDataTable = this.getData();
                    this.bindingSource = new BindingSource();
                    this.bindingSource.DataSource = this.currentDataTable;
                    this.dataGridView.DataSource = this.bindingSource;
                    this.dataChanged = true;
                }
            }
            this.dataChanged = true;
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
        }

        private void InitializeComponent()
        {
            ComponentResourceManager manager = new ComponentResourceManager(typeof(LocFileEditorControl));
            this.dataGridView = new DataGridView();
            this.toolStrip = new ToolStrip();
            this.addNewRowButton = new ToolStripButton();
            this.cloneCurrentRow = new ToolStripButton();
            this.deleteCurrentRow = new ToolStripButton();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.exportButton = new ToolStripButton();
            this.importButton = new ToolStripButton();
            this.openLocFileDialog = new OpenFileDialog();
            ((ISupportInitialize) this.dataGridView).BeginInit();
            this.toolStrip.SuspendLayout();
            base.SuspendLayout();
            this.dataGridView.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            this.dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new Point(0, 0x1c);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeadersWidth = 100;
            this.dataGridView.ShowCellErrors = false;
            this.dataGridView.ShowEditingIcon = false;
            this.dataGridView.ShowRowErrors = false;
            this.dataGridView.Size = new Size(0x36c, 0x281);
            this.dataGridView.TabIndex = 1;
            this.dataGridView.VirtualMode = true;
            this.dataGridView.UserDeletingRow += new DataGridViewRowCancelEventHandler(this.dataGridView_UserDeletingRow);
            this.dataGridView.CellEndEdit += new DataGridViewCellEventHandler(this.dataGridView_CellEndEdit);
            this.dataGridView.SelectionChanged += new EventHandler(this.dataGridView_SelectionChanged);
            this.toolStrip.Items.AddRange(new ToolStripItem[] { this.addNewRowButton, this.cloneCurrentRow, this.deleteCurrentRow, this.toolStripSeparator1, this.exportButton, this.importButton });
            this.toolStrip.Location = new Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new Size(0x36c, 0x19);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip";
            this.addNewRowButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.addNewRowButton.Image = (Image) manager.GetObject("addNewRowButton.Image");
            this.addNewRowButton.ImageTransparentColor = Color.Magenta;
            this.addNewRowButton.Name = "addNewRowButton";
            this.addNewRowButton.Size = new Size(0x56, 0x16);
            this.addNewRowButton.Text = "Add New Row";
            this.addNewRowButton.Click += new EventHandler(this.addNewRowButton_Click);
            this.cloneCurrentRow.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.cloneCurrentRow.Enabled = false;
            this.cloneCurrentRow.Image = (Image) manager.GetObject("cloneCurrentRow.Image");
            this.cloneCurrentRow.ImageTransparentColor = Color.Magenta;
            this.cloneCurrentRow.Name = "cloneCurrentRow";
            this.cloneCurrentRow.Size = new Size(0x6f, 0x16);
            this.cloneCurrentRow.Text = "Clone Current Row";
            this.cloneCurrentRow.ToolTipText = "Clone Current Row";
            this.cloneCurrentRow.Click += new EventHandler(this.cloneCurrentRow_Click);
            this.deleteCurrentRow.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.deleteCurrentRow.Enabled = false;
            this.deleteCurrentRow.ImageTransparentColor = Color.Magenta;
            this.deleteCurrentRow.Name = "deleteCurrentRow";
            this.deleteCurrentRow.Size = new Size(0x71, 0x16);
            this.deleteCurrentRow.Text = "Delete Current Row";
            this.deleteCurrentRow.Click += new EventHandler(this.deleteCurrentRow_Click);
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new Size(6, 0x19);
            this.exportButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.exportButton.Image = (Image) manager.GetObject("exportButton.Image");
            this.exportButton.ImageTransparentColor = Color.Magenta;
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new Size(0x43, 0x16);
            this.exportButton.Text = "Export TSV";
            this.exportButton.ToolTipText = "Export to tab-separated values";
            this.exportButton.Click += new EventHandler(this.exportButton_Click);
            this.importButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.importButton.Image = (Image) manager.GetObject("importButton.Image");
            this.importButton.ImageTransparentColor = Color.Magenta;
            this.importButton.Name = "importButton";
            this.importButton.Size = new Size(70, 0x16);
            this.importButton.Text = "Import TSV";
            this.importButton.ToolTipText = "Import from tab-separated values";
            this.importButton.Click += new EventHandler(this.importButton_Click);
            this.openLocFileDialog.Filter = "Text TSV|*.tsv|Any File|*.*";
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.toolStrip);
            base.Controls.Add(this.dataGridView);
            base.Name = "LocFileEditorControl";
            base.Size = new Size(0x36c, 0x29d);
            ((ISupportInitialize) this.dataGridView).EndInit();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        private void setData()
        {
            this.locFile.resetEntries();
            for (int i = 0; i < (this.dataGridView.Rows.Count - 1); i++)
            {
                string tag = this.dataGridView.Rows[i].Cells[0].Value.ToString();
                string localised = this.dataGridView.Rows[i].Cells[1].Value.ToString();
                bool tooltip = Convert.ToBoolean(this.dataGridView.Rows[i].Cells[2].Value);
                LocEntry newEntry = new LocEntry(tag, localised, tooltip);
                this.locFile.add(newEntry);
            }
            this.currentPackedFile.ReplaceData(this.locFile.GetBytes());
        }

        public void updatePackedFile()
        {
            if (this.dataChanged)
            {
                this.setData();
            }
            this.dataChanged = false;
        }
    }
}

