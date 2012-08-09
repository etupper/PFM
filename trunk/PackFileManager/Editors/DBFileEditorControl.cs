using Common;
using DataGridViewAutoFilter;
using PackFileManager.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PackFileManager {
    public class DBFileEditorControl : UserControl {
        private string referenceTarget = null;

        PackedFileDbCodec Codec;

        #region Members
        private ToolStripButton addNewRowButton;
        private CheckBox useFirstColumnAsRowHeader;
        private readonly IContainer components;
        private ToolStripButton copyToolStripButton;
        private DataSet currentDataSet;
        private DataTable currentDataTable;
        private DBFile EditedFile;
        public PackedFile CurrentPackedFile;
        private DataGridViewExtended dataGridView;
        private ToolStripButton exportButton;
        private ToolStripButton importButton;
        private ToolStripButton pasteToolStripButton;
        private ToolStrip toolStrip;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private TextBox unsupportedDBErrorTextBox;
        private CheckBox useComboBoxCells;
        private CheckBox showAllColumns;
        private bool TableColumnChanged;
        private List<List<FieldInstance>> copiedRows = new List<List<FieldInstance>>();
        private ToolStripButton cloneRowsButton;
        #endregion

        public DBFileEditorControl () {
            components = null;
            InitializeComponent ();
            dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dataGridView.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            try {
                useFirstColumnAsRowHeader.Checked = Settings.Default.UseFirstColumnAsRowHeader;
                useComboBoxCells.Checked = Settings.Default.UseComboboxCells;
                showAllColumns.Checked = Settings.Default.ShowAllColumns;
            } catch {
                // TODO: Should not need to swallow an exception.
            }
            dataGridView.KeyUp += copyPaste;
            dataGridView.SelectionChanged += new EventHandler(delegate(object sender, EventArgs args) 
                { cloneRowsButton.Enabled = dataGridView.SelectedRows.Count > 0; });

            dataGridView.DataError += new DataGridViewDataErrorEventHandler(CellErrorHandler);
            this.useComboBoxCells.CheckedChanged += new System.EventHandler(this.useComboBoxCells_CheckedChanged);
        }
        private void CellErrorHandler(object sender, DataGridViewDataErrorEventArgs args) {
            if (Settings.Default.UseComboboxCells) {
                MessageBox.Show("A table reference could not be resolved; disabling combo box cells");
                Settings.Default.UseComboboxCells = false;
            }
            args.ThrowException = true;
            useComboBoxCells.Checked = false;
        }

        private void copyPaste(object sender, KeyEventArgs arge) 
        {
            if (CurrentPackedFile != null) 
            {
                if (arge.Control) 
                {
                    if (arge.KeyCode == Keys.C) 
                    {
                        copyEvent();
                    } 
                    else if (arge.KeyCode == Keys.V) 
                    {
                        pasteEvent();
                    }
                }
            }
        }

        private void addNewRowButton_Click(object sender, EventArgs e) 
        {
            var newEntry = EditedFile.GetNewEntry();
            int insertAtColumn = dataGridView.Rows.Count;
            if (dataGridView.CurrentCell != null) 
            {
                insertAtColumn = dataGridView.CurrentCell.RowIndex + 1;
            }
            createRow(newEntry, insertAtColumn);
        }

        private void createRow(List<FieldInstance> newEntry, int index) 
        {
            var row = currentDataTable.NewRow();
            for (int i = 1; i < currentDataTable.Columns.Count; i++) 
            {
                int num2 = Convert.ToInt32(currentDataTable.Columns[i].ColumnName);
                row[i] = Convert.ChangeType(newEntry[num2].Value, currentDataTable.Columns[i].DataType);
            }
            EditedFile.Entries.Insert(index, newEntry);
            currentDataTable.Rows.InsertAt(row, index);
            dataGridView.FirstDisplayedScrollingRowIndex = index;
        }

        private void useFirstColumnAsRowHeader_CheckedChanged(object sender, EventArgs e) 
        {
            if (dataGridView.Columns.Count > 0) 
            {
                toggleFirstColumnAsRowHeader(useFirstColumnAsRowHeader.Checked);
                Settings.Default.UseFirstColumnAsRowHeader = useFirstColumnAsRowHeader.Checked;
                Settings.Default.Save();
            }
        }

        private void cloneCurrentRow_Click(object sender, EventArgs e) 
        {
            int num = (dataGridView.SelectedRows.Count == 1) ? dataGridView.SelectedRows[0].Index : dataGridView.SelectedCells[0].RowIndex;
            int index = currentDataTable.Rows.IndexOf((dataGridView.Rows[num].DataBoundItem as DataRowView).Row);

            DataRow row = currentDataTable.NewRow();
            List<FieldInstance> newEntry = EditedFile.GetNewEntry();
            List<FieldInstance> list2 = EditedFile.Entries[index];

            for (int i = 1; i < currentDataTable.Columns.Count; i++) 
            {
                int num4 = Convert.ToInt32(currentDataTable.Columns[i].ColumnName);
                newEntry[num4].Value = list2[num4].Value;
                if (!currentDataTable.Columns[i].DataType.ToString().Contains("string")) 
                {
                    row[i] = list2[num4].Value;
                }
            }

            EditedFile.Entries.Add(newEntry);
            currentDataTable.Rows.Add(row);
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
        }

        #region Copy/Paste
        private void copyEvent() 
        {
            if (EditedFile == null || dataGridView.SelectedCells.Count == 0) {
                return;
            }

            string encoded = "";
            DataGridViewSelectedCellCollection cells = dataGridView.SelectedCells;
            List<List<DataGridViewCell>> selected = SelectedCells(cells);
            for (int rowNum = 0; rowNum < selected.Count; rowNum++) {
                List<DataGridViewCell> row = selected[rowNum];
                string line = "";
                for (int colNum = 0; colNum < row.Count; colNum++) {
                    line += row[colNum].Value + "\t";
                }
                line.Remove(line.LastIndexOf("\t"));
                encoded += line + "\n";
            }

            Clipboard.SetText(encoded);
            pasteToolStripButton.Enabled = encoded.Length > 2;
        }

        private void pasteEvent() {
            string encoded = Clipboard.GetText();
            string[] lines = encoded.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[][] values = new string[lines.Length][];
            for (int i = 0; i < lines.Length; i++) {
                string[] line = lines[i].Split(new char[] { '\t' });
                values[i] = line;
            }
            DataGridViewSelectedCellCollection cells = dataGridView.SelectedCells;
            List<List<DataGridViewCell>> selected = SelectedCells(cells);
            for (int rowNum = 0; rowNum < selected.Count; rowNum++) {
                List<DataGridViewCell> row = selected[rowNum];
                for (int col = 0; col < row.Count; col++) {
                    try {
                        string setValue = values[rowNum][col];
                        row[col].Value = setValue;
                    } catch (Exception e) {
                        MessageBox.Show(string.Format("Could not paste {0}/{1}: {2}", rowNum, col, e), "Failed to paste");
                        return;
                    }
                }
            }

            CurrentPackedFile.Data = Codec.Encode(EditedFile);
            dataGridView.Refresh();
        }

        List<List<DataGridViewCell>> SelectedCells(DataGridViewSelectedCellCollection collection) {
            List<List<DataGridViewCell>> rows = new List<List<DataGridViewCell>>();
            bool ignoreColumnZero = !Settings.Default.UseFirstColumnAsRowHeader;
            foreach (DataGridViewCell cell in collection) {
                if (cell.ColumnIndex == 0 && ignoreColumnZero) {
                    continue;
                }
                int rowIndex = cell.RowIndex;
                List<DataGridViewCell> addTo;
                while (rowIndex >= rows.Count) {
                    rows.Add(new List<DataGridViewCell>());
                }
                addTo = rows[rowIndex];
                while (cell.ColumnIndex >= addTo.Count) {
                    addTo.Add(null);
                }
                if (cell.ColumnIndex == 0 && ignoreColumnZero) {
                    continue;
                }
                addTo[cell.ColumnIndex] = cell;
            }
            List<List<DataGridViewCell>> result = new List<List<DataGridViewCell>>();
            rows.ForEach(row => {
                if (row.Count > 0) {
                    List<DataGridViewCell> newRow = new List<DataGridViewCell>();
                    row.ForEach(cell => { if (cell != null) newRow.Add(cell); });
                    result.Add(newRow);
                }
            });
            return result;
        }
        #endregion

        private void copyToolStripButton_Click(object sender, EventArgs e) 
        {
            copyEvent();
        }

        private void currentDataTable_ColumnChanged(object sender, DataColumnChangeEventArgs e) 
        {
            if (((dataGridView.DataSource != null) && (e.Row.RowState != DataRowState.Detached)) && (Convert.ToInt32(e.Column.ColumnName) != -1)) 
            {
                object proposedValue = e.ProposedValue;
                int num = Convert.ToInt32(e.Column.ColumnName);
                List<FieldInstance> list = EditedFile.Entries[currentDataTable.Rows.IndexOf(e.Row)];
                FieldInstance instance = list[num];
                string str = (proposedValue == null) ? "" : proposedValue.ToString();
                instance.Value = str;
                CurrentPackedFile.Data = Codec.Encode(EditedFile);
            }
        }

        private void currentDataTable_RowDeleted(object sender, DataRowChangeEventArgs e) {
            if (e.Action != DataRowAction.Delete) {
                throw new InvalidDataException("wtf?");
            }
            EditedFile.Entries.RemoveAt(currentDataTable.Rows.IndexOf(e.Row));
            CurrentPackedFile.Data = Codec.Encode(EditedFile);
        }

        private void currentDataTable_TableNewRow(object sender, DataTableNewRowEventArgs e) {
            if (dataGridView.DataSource != null) {
                CurrentPackedFile.Data = (Codec.Encode(EditedFile));
            }
        }

        private void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var dataSource = dataGridView.DataSource as BindingSource;
            if (dataSource == null) throw new Exception("DataGridView has no DataSource");

            if ((((EditedFile.CurrentType.fields.Count > 1) && useFirstColumnAsRowHeader.Checked) && ((e.ColumnIndex == -1) && (e.RowIndex > -1))) && (e.RowIndex < dataSource.Count))
            {
                var dataRowView = dataSource[e.RowIndex] as DataRowView;
                if (dataRowView == null) throw new Exception(string.Format("No DataRowView for RowIndex {0}", e.RowIndex));

                e.PaintBackground(e.ClipBounds, false);
                string s = dataRowView[1].ToString();
                float num = Convert.ToSingle(e.CellBounds.Height - dataGridView.DefaultCellStyle.Font.Height) / 2f;
                RectangleF cellBounds = e.CellBounds;
                cellBounds.Inflate(0f, -num);
                cellBounds.X += 5f;
                cellBounds.Width -= 5f;
                using (Graphics graphics = e.Graphics) {
                    graphics.DrawString(s, dataGridView.DefaultCellStyle.Font, Brushes.Black, cellBounds);
                }
                e.Handled = true;
            }
        }

        private void dataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e) 
        {
            // TODO: Empty method?
        }

        private void DBFileEditorControl_Enter(object sender, EventArgs e) 
        {
            pasteToolStripButton.Enabled = copiedRows.Count != 0;
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

        private void exportButton_Click(object sender, EventArgs e) {
            List<PackedFile> files = new List<PackedFile>();
            files.Add(CurrentPackedFile);
            FileExtractor extractor = new FileExtractor(null, null) { Preprocessor = new TsvExtractionPreprocessor() };
            extractor.extractFiles(files);
            MessageBox.Show(string.Format("File exported to TSV."));
        }

        private void importButton_Click(object sender, EventArgs e) {
            OpenFileDialog openDBFileDialog = new OpenFileDialog {
                InitialDirectory = Settings.Default.ImportExportDirectory,
                FileName = Settings.Default.TsvFile(EditedFile.CurrentType.name)
            };

            if (openDBFileDialog.ShowDialog() == DialogResult.OK) {
                Settings.Default.ImportExportDirectory = Path.GetDirectoryName(openDBFileDialog.FileName);
                try {
                    using (var stream = new MemoryStream(File.ReadAllBytes(openDBFileDialog.FileName))) {
                        EditedFile.Import(new TextDbCodec().Decode(stream));
                    }

                } catch (DBFileNotSupportedException exception) {
                    showDBFileNotSupportedMessage(exception.Message);
                }

                CurrentPackedFile.Data = (Codec.Encode(EditedFile));
                Open(CurrentPackedFile);
            }
        }

        private void InitializeComponent() {
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.addNewRowButton = new System.Windows.Forms.ToolStripButton();
            this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.pasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exportButton = new System.Windows.Forms.ToolStripButton();
            this.importButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.unsupportedDBErrorTextBox = new System.Windows.Forms.TextBox();
            this.useFirstColumnAsRowHeader = new System.Windows.Forms.CheckBox();
            this.showAllColumns = new System.Windows.Forms.CheckBox();
            this.useComboBoxCells = new System.Windows.Forms.CheckBox();
            this.dataGridView = new PackFileManager.DataGridViewExtended();
            this.cloneRowsButton = new System.Windows.Forms.ToolStripButton();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewRowButton,
            this.cloneRowsButton,
            this.copyToolStripButton,
            this.pasteToolStripButton,
            this.toolStripSeparator1,
            this.exportButton,
            this.importButton,
            this.toolStripSeparator2});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(876, 25);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip";
            // 
            // addNewRowButton
            // 
            this.addNewRowButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.addNewRowButton.Enabled = false;
            this.addNewRowButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addNewRowButton.Name = "addNewRowButton";
            this.addNewRowButton.Size = new System.Drawing.Size(59, 22);
            this.addNewRowButton.Text = "Add Row";
            this.addNewRowButton.ToolTipText = "Add New Row";
            this.addNewRowButton.Click += new System.EventHandler(this.addNewRowButton_Click);
            // 
            // copyToolStripButton
            // 
            this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyToolStripButton.Enabled = true;
            this.copyToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.copyToolStripButton.Name = "copyToolStripButton";
            this.copyToolStripButton.Size = new System.Drawing.Size(39, 22);
            this.copyToolStripButton.Text = "&Copy";
            this.copyToolStripButton.ToolTipText = "Copy Current Row";
            this.copyToolStripButton.Click += new System.EventHandler(this.copyToolStripButton_Click);
            // 
            // pasteToolStripButton
            // 
            this.pasteToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.pasteToolStripButton.Enabled = false;
            this.pasteToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.pasteToolStripButton.Name = "pasteToolStripButton";
            this.pasteToolStripButton.Size = new System.Drawing.Size(39, 22);
            this.pasteToolStripButton.Text = "&Paste";
            this.pasteToolStripButton.ToolTipText = "Paste Row from Clipboard";
            this.pasteToolStripButton.Click += new System.EventHandler(this.pasteToolStripButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // exportButton
            // 
            this.exportButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.exportButton.Enabled = false;
            this.exportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(67, 22);
            this.exportButton.Text = "Export TSV";
            this.exportButton.ToolTipText = "Export to tab-separated values";
            this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
            // 
            // importButton
            // 
            this.importButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.importButton.Enabled = false;
            this.importButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.importButton.Name = "importButton";
            this.importButton.Size = new System.Drawing.Size(70, 22);
            this.importButton.Text = "Import TSV";
            this.importButton.ToolTipText = "Import from tab-separated values";
            this.importButton.Click += new System.EventHandler(this.importButton_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // unsupportedDBErrorTextBox
            // 
            this.unsupportedDBErrorTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.unsupportedDBErrorTextBox.Location = new System.Drawing.Point(0, 28);
            this.unsupportedDBErrorTextBox.Multiline = true;
            this.unsupportedDBErrorTextBox.Name = "unsupportedDBErrorTextBox";
            this.unsupportedDBErrorTextBox.ReadOnly = true;
            this.unsupportedDBErrorTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.unsupportedDBErrorTextBox.Size = new System.Drawing.Size(876, 641);
            this.unsupportedDBErrorTextBox.TabIndex = 3;
            this.unsupportedDBErrorTextBox.Visible = false;
            // 
            // useFirstColumnAsRowHeader
            // 
            this.useFirstColumnAsRowHeader.AutoSize = true;
            this.useFirstColumnAsRowHeader.Location = new System.Drawing.Point(422, 4);
            this.useFirstColumnAsRowHeader.Name = "useFirstColumnAsRowHeader";
            this.useFirstColumnAsRowHeader.Size = new System.Drawing.Size(183, 17);
            this.useFirstColumnAsRowHeader.TabIndex = 4;
            this.useFirstColumnAsRowHeader.Text = "Use First Column As Row Header";
            this.useFirstColumnAsRowHeader.UseVisualStyleBackColor = true;
            this.useFirstColumnAsRowHeader.CheckedChanged += new System.EventHandler(this.useFirstColumnAsRowHeader_CheckedChanged);
            // 
            // showAllColumns
            // 
            this.showAllColumns.AutoSize = true;
            this.showAllColumns.Location = new System.Drawing.Point(741, 4);
            this.showAllColumns.Name = "showAllColumns";
            this.showAllColumns.Size = new System.Drawing.Size(108, 17);
            this.showAllColumns.TabIndex = 6;
            this.showAllColumns.Text = "Show all columns";
            this.showAllColumns.UseVisualStyleBackColor = true;
            this.showAllColumns.CheckedChanged += new System.EventHandler(this.showAllColumns_CheckedChanged);
            // 
            // useComboBoxCells
            // 
            this.useComboBoxCells.AutoSize = true;
            this.useComboBoxCells.Checked = true;
            this.useComboBoxCells.CheckState = System.Windows.Forms.CheckState.Checked;
            this.useComboBoxCells.Location = new System.Drawing.Point(611, 4);
            this.useComboBoxCells.Name = "useComboBoxCells";
            this.useComboBoxCells.Size = new System.Drawing.Size(124, 17);
            this.useComboBoxCells.TabIndex = 5;
            this.useComboBoxCells.Text = "Use ComboBox Cells";
            this.useComboBoxCells.UseVisualStyleBackColor = true;
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new System.Drawing.Point(0, 28);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeadersWidth = 100;
            this.dataGridView.ShowCellErrors = false;
            this.dataGridView.ShowEditingIcon = false;
            this.dataGridView.ShowRowErrors = false;
            this.dataGridView.Size = new System.Drawing.Size(876, 641);
            this.dataGridView.TabIndex = 1;
            this.dataGridView.VirtualMode = true;
            // 
            // cloneRowsButton
            // 
            this.cloneRowsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cloneRowsButton.Enabled = false;
            this.cloneRowsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cloneRowsButton.Name = "cloneRowsButton";
            this.cloneRowsButton.Size = new System.Drawing.Size(81, 22);
            this.cloneRowsButton.Text = "Clone Row(s)";
            this.cloneRowsButton.ToolTipText = "Add New Row";
            this.cloneRowsButton.Click += new System.EventHandler(this.cloneRowsButton_Click);
            // 
            // DBFileEditorControl
            // 
            this.Controls.Add(this.showAllColumns);
            this.Controls.Add(this.useComboBoxCells);
            this.Controls.Add(this.useFirstColumnAsRowHeader);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.unsupportedDBErrorTextBox);
            this.Name = "DBFileEditorControl";
            this.Size = new System.Drawing.Size(876, 669);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        DataGridViewColumn createColumn(string columnName, FieldInfo fieldInfo, int fieldCount) 
        {
            DataGridViewColumn column = null;
            if (Settings.Default.UseComboboxCells)
            {
                try 
                {
                    ICollection<string> items;
                    items = DBReferenceMap.Instance.resolveReference(fieldInfo.ForeignReference);

                    if (items != null) 
                    {
                        column = new DataGridViewComboBoxColumn { DataPropertyName = columnName };
                        var cb = (DataGridViewComboBoxColumn)column;
                        cb.Items.Add(string.Empty);

                        foreach (string item in items)
                            cb.Items.Add(item);
                    }
                } 
                catch {
                    MessageBox.Show("A table reference could not be resolved.\nDisabling combo cells.");
                    Settings.Default.UseComboboxCells = false;
                    useComboBoxCells.CheckedChanged -= useComboBoxCells_CheckedChanged;
                    useComboBoxCells.CheckState = CheckState.Unchecked;
                    useComboBoxCells.CheckedChanged += useComboBoxCells_CheckedChanged;
                }
            }

            if (column == null) 
            {
                column = new DataGridViewAutoFilterTextBoxColumn {DataPropertyName = columnName };
            }
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
            column.HeaderText = fieldInfo.Name + (Settings.Default.IsColumnIgnored(CurrentPackedFile.FullPath, fieldInfo.Name) ? "*" : "");
            column.Tag = fieldInfo;
            column.Visible = !Settings.Default.IsColumnIgnored(CurrentPackedFile.FullPath, fieldInfo.Name);

            if (column.Visible) 
            {
                int visibleColumnCount = Math.Min(fieldCount, 10);
                int columnWidth = (Width - dataGridView.Columns[0].Width) / visibleColumnCount;
                column.Width = columnWidth;
            }
            return column;
        }

        public void Open(PackedFile packedFile) {
            copiedRows.Clear();
            copyToolStripButton.Enabled = true;
            pasteToolStripButton.Enabled = false;
            string key = DBFile.typename(packedFile.FullPath);

            if (!DBTypeMap.Instance.IsSupported(key)) {
                showDBFileNotSupportedMessage("Sorry, this db file isn't supported yet.\r\n\r\nCurrently supported files:\r\n");
                if (Settings.Default.ShowDecodeToolOnError) {
                    var decoder = new DecodeTool.DecodeTool { TypeName = key, Bytes = packedFile.Data };
                    decoder.ShowDialog();
                    if (!DBTypeMap.Instance.IsSupported(key)) {
                        return;
                    }
                } else {
                    return;
                }
            }
            try {
                EditedFile = PackedFileDbCodec.Decode(packedFile);
            } catch {
                if (Settings.Default.ShowDecodeToolOnError) {
                    var decoder = new DecodeTool.DecodeTool { TypeName = key, Bytes = packedFile.Data };
                    decoder.ShowDialog();
                    try {
                        EditedFile = PackedFileDbCodec.Decode(packedFile);
                    } catch {
                        return;
                    }
                } else {
                    return;
                }
            }
            dataGridView.DataSource = null;
            CurrentPackedFile = packedFile;
            Codec = PackedFileDbCodec.FromFilename(packedFile.FullPath);
            TypeInfo info = EditedFile.CurrentType;
            currentDataSet = new DataSet(info.name + "_DataSet");
            currentDataTable = new DataTable(info.name + "_DataTable");
            currentDataTable.Columns.Add(new DataColumn("#", Type.GetType("System.Int32")));
            var dataGridViewColumn = new DataGridViewTextBoxColumn { DataPropertyName = "#", Visible = false };
            dataGridView.Columns.Add(dataGridViewColumn);

            int num;
            for (num = 0; num < info.fields.Count; num++) {
                string columnName = num.ToString();
                var column = new DataColumn(columnName) {
                    DataType =
                        info.fields[num].TypeCode == TypeCode.Empty
                            ? Type.GetType("System.String")
                            : Type.GetType("System." + info.fields[num].TypeCode)
                };
                currentDataTable.Columns.Add(column);
                dataGridView.Columns.Add(createColumn(columnName, info.fields[num], info.fields.Count));
            }

            currentDataSet.Tables.Add(currentDataTable);
            currentDataTable.ColumnChanged += currentDataTable_ColumnChanged;
            currentDataTable.RowDeleting += currentDataTable_RowDeleted;
            currentDataTable.TableNewRow += currentDataTable_TableNewRow;

            for (num = 0; num < EditedFile.Entries.Count; num++) {
                DataRow row = currentDataTable.NewRow();
                row[0] = num;
                for (int i = 1; i < currentDataTable.Columns.Count; i++) {
                    int num3 = Convert.ToInt32(currentDataTable.Columns[i].ColumnName);
                    row[i] = EditedFile.Entries[num][num3].Value;
                }
                currentDataTable.Rows.Add(row);
            }

            dataGridView.DataSource = new BindingSource(currentDataSet, info.name + "_DataTable");
            addNewRowButton.Enabled = true;
            importButton.Enabled = true;
            exportButton.Enabled = true;
            dataGridView.Visible = true;
            unsupportedDBErrorTextBox.Visible = false;
            toggleFirstColumnAsRowHeader(Settings.Default.UseFirstColumnAsRowHeader);
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e) 
        {
            pasteEvent();
        }

        private void showDBFileNotSupportedMessage(string message) 
        {
            dataGridView.Visible = false;
            unsupportedDBErrorTextBox.Visible = true;
            unsupportedDBErrorTextBox.Text = string.Format("{0}{1}", message, string.Join("\r\n", DBTypeMap.Instance.DBFileTypes));
            // unsupportedDBErrorTextBox.Text = message;
            addNewRowButton.Enabled = false;
            importButton.Enabled = false;
            exportButton.Enabled = false;
        }

        private void toggleFirstColumnAsRowHeader(bool isChecked) 
        {
            dataGridView.Columns[0].Frozen = isChecked;
            if (dataGridView.ColumnCount > 1)
                dataGridView.Columns[1].Frozen = isChecked;

            if (isChecked) 
            {
                dataGridView.TopLeftHeaderCell.Value = EditedFile.Entries[0][0].Info.Name;
                dataGridView.RowHeadersVisible = false;
            } 
            else 
            {
                dataGridView.TopLeftHeaderCell.Value = "";
                dataGridView.RowHeadersVisible = true;
            }
        }

        private void useComboBoxCells_CheckedChanged(object sender, EventArgs e) 
        {
            Settings.Default.UseComboboxCells = useComboBoxCells.Checked;
            if (CurrentPackedFile != null) 
            {
                // rebuild table
                Open(CurrentPackedFile);
            }
        }

        private void promptHeaderDescription(DataGridViewColumn newColumn)
        {
            var info = (FieldInfo)newColumn.Tag;
            var box = new InputBox { Text = "Enter new description", Input = info.Name };
            if (box.ShowDialog() == DialogResult.OK) 
            {
                info.Name = box.Input;
                newColumn.HeaderText = info.Name;

                if (!TableColumnChanged) 
                {
                    TableColumnChanged = true;
                    MessageBox.Show("Don't forget to save your changes (DB Definitions->Save to Directory)");
                }
            }
        }

        private void setReferenceTarget(DataGridViewColumn column) {
            var info = (FieldInfo)column.Tag;
            string tableName = EditedFile.CurrentType.name;
            if (!tableName.EndsWith("_tables")) {
                tableName += "_tables";
            }
            referenceTarget = string.Format("{0}.{1}", tableName, info.Name);
        }

        private void applyReferenceTarget(DataGridViewColumn column) {
            var info = (FieldInfo)column.Tag;
            if (referenceTarget != null) {
                info.ForeignReference = referenceTarget;
                info.Name = string.Format("{0}Ref", referenceTarget.Substring(referenceTarget.LastIndexOf('.')+1));
            }
            // rebuild table to see combo boxes if applicable
            Open(CurrentPackedFile);
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            DataGridViewColumn newColumn = dataGridView.Columns[e.ColumnIndex];

            if (e.Button == MouseButtons.Right) {
                var menu = new ContextMenu();
                var item = new MenuItem("Change Column Description", delegate {
                                                                             promptHeaderDescription(newColumn);
                                                                         });
                menu.MenuItems.Add(item);
                var info = newColumn.Tag as FieldInfo;

                // edit number items
                if (info != null && (info.TypeCode == TypeCode.Int16 || info.TypeCode == TypeCode.UInt32 || info.TypeCode == TypeCode.Single)) {
                    item = new MenuItem("Edit...");
                    item.MenuItems.Add(new MenuItem("Add value to All", delegate {
                        AddToAll(e.ColumnIndex);
                    }));
                    item.MenuItems.Add(new MenuItem("Renumber...", delegate {
                        RenumberFrom(e.ColumnIndex);
                    }));
                    menu.MenuItems.Add(item);
                }

                item = new MenuItem("Set as Reference Target", delegate {
                    setReferenceTarget(newColumn);
                });
                menu.MenuItems.Add(item);

                if (referenceTarget != null) {
                    item = new MenuItem(string.Format("Apply Reference Target ({0})", referenceTarget), delegate {
                        applyReferenceTarget(newColumn);
                    });
                    menu.MenuItems.Add(item);
                }

                if (info != null && info.ForeignReference != "") {
                    item = new MenuItem(string.Format("Clear Reference Target ({0})", info.ForeignReference), delegate {
                        info.ForeignReference = "";
                        // rebuild table to remove combo boxes if applicable
                        Open(CurrentPackedFile);
                    });
                    menu.MenuItems.Add(item);
                }

                string ignoreField = ((FieldInfo)newColumn.Tag).Name;
                bool ignored = Settings.Default.IsColumnIgnored(CurrentPackedFile.FullPath, ignoreField);
                string itemText = ignored ? "Show Column" : "Hide Column";

                item = new MenuItem(itemText, delegate {
                                                      if (ignored) {
                                                          Settings.Default.UnignoreColumn(CurrentPackedFile.FullPath, ignoreField);
                                                      } else {
                                                          Settings.Default.IgnoreColumn(CurrentPackedFile.FullPath, ignoreField);
                                                      }
                                                      Settings.Default.Save();
                                                      applyColumnVisibility();
                                                  });

                menu.MenuItems.Add(item);

                item = new MenuItem("Clear Hide list for this table", delegate {
                                                                              Settings.Default.ResetIgnores(CurrentPackedFile.FullPath);
                                                                              Settings.Default.Save();
                                                                              applyColumnVisibility();
                                                                          });

                menu.MenuItems.Add(item);
                item = new MenuItem("Clear Hide list for all tables", delegate {
                                                                              Settings.Default.IgnoreColumns = "";
                                                                              Settings.Default.Save();
                                                                              applyColumnVisibility();
                                                                          });
                menu.MenuItems.Add(item);
                menu.Show(dataGridView, e.Location);
                return;
            }
        }

        private void applyColumnVisibility() 
        {
            if (CurrentPackedFile == null)
                return;

            for (int i = 0; i < dataGridView.ColumnCount; i++)
            {
                DataGridViewColumn column = dataGridView.Columns[i];
                if (column != null && column.Tag != null) 
                {
                    var info = ((FieldInfo)column.Tag);
                    bool show = !Settings.Default.IsColumnIgnored(CurrentPackedFile.FullPath, info.Name);
                    column.Visible = (Settings.Default.ShowAllColumns || show);
                    column.HeaderText = info.Name + (show ? "" : "*");
                } 
                else 
                {
                    // TODO: Why would we be writing to the console?
                    Console.WriteLine("no column?");
                }
            }
        }

        private void showAllColumns_CheckedChanged(object sender, EventArgs e) 
        {
            Settings.Default.ShowAllColumns = showAllColumns.Checked;
            Settings.Default.Save();
            if (CurrentPackedFile != null) 
            {
                applyColumnVisibility();
            }
        }

        private void AddToAll(int columnIndex) {
            InputBox box = new InputBox { Text = "Enter number to add", Input = "0" };
            if (box.ShowDialog() == DialogResult.OK) {
                try {
                    float addValue = float.Parse(box.Input);
                    for (int i = 0; i < dataGridView.RowCount; i++) {
                        DataRow row = currentDataTable.Rows[i];
                        float newValue = float.Parse(row[columnIndex].ToString()) + addValue;
                        row[columnIndex] = newValue;
                    }
                } catch (Exception ex) {
                    MessageBox.Show(string.Format("Could not apply value: {0}", ex.Message), "You fail!");
                }
            }
        }

        private void RenumberFrom(int columnIndex) {
            InputBox box = new InputBox { Text = "Enter number to start from", Input = "1" };
            if (box.ShowDialog() == DialogResult.OK) {
                try {
                    int setValue = int.Parse(box.Input);
                    for (int i = 0; i < dataGridView.RowCount; i++) {
                        DataRow row = currentDataTable.Rows[i];
                        int newValue = setValue + i;
                        row[columnIndex] = newValue;
                    }
                } catch (Exception ex) {
                    MessageBox.Show(string.Format("Could not apply values: {0}", ex.Message), "You fail!");
                }
            }
        }

        private void cloneRowsButton_Click(object sender, EventArgs e) {
            DataGridViewSelectedRowCollection selectedRows = dataGridView.SelectedRows;
            if (selectedRows.Count != 0) {
                foreach (DataGridViewRow row in selectedRows) {
                    List<FieldInstance> toCopy = EditedFile.Entries[row.Index];
                    var copy = new List<FieldInstance>(toCopy.Count);
                    toCopy.ForEach(field => copy.Add(new FieldInstance(field.Info, field.Value)));
                    createRow(copy, row.Index);
                }
            } else {
                MessageBox.Show("Please select the Row(s) to Clone!", "Please select Rows", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
