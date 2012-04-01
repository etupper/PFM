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
    public class DBFileEditorControl : UserControl 
    {
        private enum COPIED_TYPE { NONE, ROWS, CELLS }
        private string referenceTarget = null;

        #region Members
        private ToolStripButton addNewRowButton;
        private CheckBox useFirstColumnAsRowHeader;
        private ToolStripButton cloneCurrentRow;
        private readonly IContainer components;
        private ToolStripButton copyToolStripButton;
        private DataSet currentDataSet;
        private DataTable currentDataTable;
        private DBFile currentDBFile;
        public PackedFile currentPackedFile;
        private DataGridViewExtended dataGridView;
        private ToolStripButton exportButton;
        private ToolStripButton importButton;
        public OpenFileDialog openDBFileDialog;
        private ToolStripButton pasteToolStripButton;
        private ToolStrip toolStrip;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private TextBox unsupportedDBErrorTextBox;
        private CheckBox useComboBoxCells;
        private CheckBox showAllColumns;
        private bool dataChanged;
        private List<List<FieldInstance>> copiedRows = new List<List<FieldInstance>>();
        private COPIED_TYPE lastCopy = COPIED_TYPE.NONE;
        #endregion

        public DBFileEditorControl () {
            components = null;
            InitializeComponent ();
            initTypeMap (Path.GetDirectoryName (Application.ExecutablePath));
            dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dataGridView.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            try {
                useFirstColumnAsRowHeader.Checked = Settings.Default.UseFirstColumnAsRowHeader;
                showAllColumns.Checked = Settings.Default.ShowAllColumns;
            } catch {
                // TODO: Should not need to swallow an exception.
            }
            dataGridView.KeyUp += copyPaste;
            openDBFileDialog.Filter = "TSV Files|*.tsv|CSV Files|*.csv|All Files|*.*";
        }

        private void copyPaste(object sender, KeyEventArgs arge) 
        {
            if (currentPackedFile != null) 
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
            var newEntry = currentDBFile.GetNewEntry();
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
            currentDBFile.Entries.Insert(index, newEntry);
            currentDataTable.Rows.InsertAt(row, index);
            dataGridView.FirstDisplayedScrollingRowIndex = index;
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e) 
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
            List<FieldInstance> newEntry = currentDBFile.GetNewEntry();
            List<FieldInstance> list2 = currentDBFile.Entries[index];

            for (int i = 1; i < currentDataTable.Columns.Count; i++) 
            {
                int num4 = Convert.ToInt32(currentDataTable.Columns[i].ColumnName);
                newEntry[num4].Value = list2[num4].Value;
                if (!currentDataTable.Columns[i].DataType.ToString().Contains("string")) 
                {
                    row[i] = list2[num4].Value;
                }
            }

            currentDBFile.Entries.Add(newEntry);
            currentDataTable.Rows.Add(row);
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
        }

        private void initTypeMap(string path) {
            try {
                DBTypeMap.Instance.initializeTypeMap(path);
            } catch (Exception e) {
                if (MessageBox.Show(string.Format("Could not initialize type map: {0}.\nTry autoupdate?", e.Message),
                    "Initialize failed", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes) {
                    PackFileManagerForm.tryUpdate();
                }
            }
        }

        private void copyEvent() 
        {
            if (currentDBFile == null)
                return;

            copiedRows = new List<List<FieldInstance>>();
            if (dataGridView.SelectedRows.Count != 0) 
            {
                DataGridViewSelectedRowCollection selected = dataGridView.SelectedRows;
                foreach (DataGridViewRow row in selected) 
                {
                    List<FieldInstance> toCopy = currentDBFile.Entries[row.Index];
                    var copy = new List<FieldInstance>(toCopy.Count);
                    toCopy.ForEach(field => copy.Add(new FieldInstance(field.Info, field.Value)));
                    copiedRows.Add(copy);
                    lastCopy = COPIED_TYPE.ROWS;
                }
            } 
            else 
            {
                DataGridViewSelectedCellCollection cells = dataGridView.SelectedCells;
                copiedRows = new List<List<FieldInstance>>();
                int minColumn = dataGridView.ColumnCount;
                int maxColumn = -1;
                int minRow = dataGridView.RowCount;
                int maxRow = -1;

                foreach (DataGridViewCell cell in cells) 
                {
                    minColumn = Math.Min(minColumn, cell.ColumnIndex);
                    maxColumn = Math.Max(maxColumn, cell.ColumnIndex);
                    minRow = Math.Min(minRow, cell.RowIndex);
                    maxRow = Math.Max(maxRow, cell.RowIndex);
                }

                for (int j = minRow; j <= maxRow; j++) 
                {
                    var dataRowView = dataGridView.Rows[j].DataBoundItem as DataRowView;
                    var copy = new List<FieldInstance>(maxColumn - minColumn);

                    for (int i = minColumn; i <= maxColumn; i++) 
                    {
                        var info = (FieldInfo)dataGridView.Columns[i].Tag;
                        if (dataRowView != null)
                        {
                            Console.WriteLine("{1}: {0}", dataRowView[i], info.TypeName);
                            copy.Add(new FieldInstance(info, dataRowView[i].ToString()));
                        }
                    }
                    copiedRows.Add(copy);
                }
                lastCopy = COPIED_TYPE.CELLS;
            }
            pasteToolStripButton.Enabled = copiedRows.Count != 0;
        }

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
                List<FieldInstance> list = currentDBFile.Entries[currentDataTable.Rows.IndexOf(e.Row)];
                FieldInstance instance = list[num];
                string str = (proposedValue == null) ? "" : proposedValue.ToString();
                instance.Value = str;
                currentPackedFile.Data = PackedFileDbCodec.Encode(currentDBFile);
            }
        }

        private void currentDataTable_RowDeleted(object sender, DataRowChangeEventArgs e) {
            if (e.Action != DataRowAction.Delete) {
                throw new InvalidDataException("wtf?");
            }
            currentDBFile.Entries.RemoveAt(currentDataTable.Rows.IndexOf(e.Row));
            currentPackedFile.Data = PackedFileDbCodec.Encode(currentDBFile);
        }

        private void currentDataTable_TableNewRow(object sender, DataTableNewRowEventArgs e) {
            if (dataGridView.DataSource != null) {
                currentPackedFile.Data = (PackedFileDbCodec.Encode(currentDBFile));
            }
        }

        private void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var dataSource = dataGridView.DataSource as BindingSource;
            if (dataSource == null) throw new Exception("DataGridView has no DataSource");

            if ((((currentDBFile.CurrentType.fields.Count > 1) && useFirstColumnAsRowHeader.Checked) && ((e.ColumnIndex == -1) && (e.RowIndex > -1))) && (e.RowIndex < dataSource.Count))
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

        private void dataGridView_SelectionChanged(object sender, EventArgs e) 
        {
            cloneCurrentRow.Enabled = (dataGridView.SelectedRows.Count == 1) || (dataGridView.SelectedCells.Count == 1);
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
            var dialog = new SaveFileDialog {
                FileName = currentDBFile.CurrentType.name + ".tsv",
                Filter = "TSV file|*.tsv|CSV file|*.csv|All Files|*.*"
            };

            if (dialog.ShowDialog () == DialogResult.OK) {
                Stream stream = new FileStream (dialog.FileName, FileMode.Create);
                try {
                    TextDbCodec.Instance.Encode (stream, currentDBFile);
                    stream.Close ();
                } catch (DBFileNotSupportedException exception) {
                    showDBFileNotSupportedMessage (exception.Message);
                } finally {
                    stream.Dispose ();
                }
            }
        }

        private void importButton_Click(object sender, EventArgs e) {
            openDBFileDialog.FileName = currentDBFile.CurrentType.name + ".tsv";

            if (openDBFileDialog.ShowDialog() == DialogResult.OK) {
                try {
                    using (var stream = new MemoryStream(File.ReadAllBytes(openDBFileDialog.FileName))) {
                        currentDBFile.Import(new TextDbCodec().readDbFile(stream));
                    }

                } catch (DBFileNotSupportedException exception) {
                    showDBFileNotSupportedMessage(exception.Message);
                }

                currentPackedFile.Data = (PackedFileDbCodec.Encode(currentDBFile));
                Open(currentPackedFile);
            }
        }

        private void InitializeComponent() {
            var settings3 = new Settings();
            dataGridView = new DataGridViewExtended();
            toolStrip = new ToolStrip();
            addNewRowButton = new ToolStripButton();
            cloneCurrentRow = new ToolStripButton();
            copyToolStripButton = new ToolStripButton();
            pasteToolStripButton = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            exportButton = new ToolStripButton();
            importButton = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            openDBFileDialog = new OpenFileDialog();
            unsupportedDBErrorTextBox = new TextBox();
            useFirstColumnAsRowHeader = new CheckBox();
            showAllColumns = new CheckBox();
            useComboBoxCells = new CheckBox();
            ((ISupportInitialize)(dataGridView)).BeginInit();
            toolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView
            // 
            dataGridView.AllowUserToAddRows = false;
            dataGridView.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right;
            dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.Location = new Point(0, 28);
            dataGridView.Name = "dataGridView";
            dataGridView.RowHeadersWidth = 100;
            dataGridView.ShowCellErrors = false;
            dataGridView.ShowEditingIcon = false;
            dataGridView.ShowRowErrors = false;
            dataGridView.Size = new Size(876, 641);
            dataGridView.TabIndex = 1;
            dataGridView.VirtualMode = true;
            dataGridView.CellPainting += dataGridView_CellPainting;
            dataGridView.DataBindingComplete += dataGridView_DataBindingComplete;
            dataGridView.SelectionChanged += dataGridView_SelectionChanged;
            //dataGridView.KeyPress += dataGridView_KeyPress;
            // 
            // toolStrip
            // 
            toolStrip.Items.AddRange(new ToolStripItem[] {
            addNewRowButton,
            cloneCurrentRow,
            copyToolStripButton,
            pasteToolStripButton,
            toolStripSeparator1,
            exportButton,
            importButton,
            toolStripSeparator2});
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(876, 25);
            toolStrip.TabIndex = 2;
            toolStrip.Text = "toolStrip";
            // 
            // addNewRowButton
            // 
            addNewRowButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            addNewRowButton.Enabled = false;
            addNewRowButton.ImageTransparentColor = Color.Magenta;
            addNewRowButton.Name = "addNewRowButton";
            addNewRowButton.Size = new Size(59, 22);
            addNewRowButton.Text = "Add Row";
            addNewRowButton.ToolTipText = "Add New Row";
            addNewRowButton.Click += addNewRowButton_Click;
            // 
            // cloneCurrentRow
            // 
            cloneCurrentRow.DisplayStyle = ToolStripItemDisplayStyle.Text;
            cloneCurrentRow.Enabled = false;
            cloneCurrentRow.ImageTransparentColor = Color.Magenta;
            cloneCurrentRow.Name = "cloneCurrentRow";
            cloneCurrentRow.Size = new Size(68, 22);
            cloneCurrentRow.Text = "Clone Row";
            cloneCurrentRow.ToolTipText = "Clone Current Row";
            cloneCurrentRow.Click += cloneCurrentRow_Click;
            // 
            // copyToolStripButton
            // 
            copyToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            copyToolStripButton.Enabled = false;
            copyToolStripButton.ImageTransparentColor = Color.Magenta;
            copyToolStripButton.Name = "copyToolStripButton";
            copyToolStripButton.Size = new Size(39, 22);
            copyToolStripButton.Text = "&Copy";
            copyToolStripButton.ToolTipText = "Copy Current Row";
            copyToolStripButton.Click += copyToolStripButton_Click;
            // 
            // pasteToolStripButton
            // 
            pasteToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            pasteToolStripButton.Enabled = false;
            pasteToolStripButton.ImageTransparentColor = Color.Magenta;
            pasteToolStripButton.Name = "pasteToolStripButton";
            pasteToolStripButton.Size = new Size(39, 22);
            pasteToolStripButton.Text = "&Paste";
            pasteToolStripButton.ToolTipText = "Paste Row from Clipboard";
            pasteToolStripButton.Click += pasteToolStripButton_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // exportButton
            // 
            exportButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            exportButton.Enabled = false;
            exportButton.ImageTransparentColor = Color.Magenta;
            exportButton.Name = "exportButton";
            exportButton.Size = new Size(67, 22);
            exportButton.Text = "Export TSV";
            exportButton.ToolTipText = "Export to tab-separated values";
            exportButton.Click += exportButton_Click;
            // 
            // importButton
            // 
            importButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            importButton.Enabled = false;
            importButton.ImageTransparentColor = Color.Magenta;
            importButton.Name = "importButton";
            importButton.Size = new Size(70, 22);
            importButton.Text = "Import TSV";
            importButton.ToolTipText = "Import from tab-separated values";
            importButton.Click += importButton_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // openDBFileDialog
            // 
            openDBFileDialog.Filter = "Tab separated values (TSV)|*.tsv,*.csv|Any File|*.*";
            // 
            // unsupportedDBErrorTextBox
            // 
            unsupportedDBErrorTextBox.Anchor = ((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right;
            unsupportedDBErrorTextBox.Location = new Point(0, 28);
            unsupportedDBErrorTextBox.Multiline = true;
            unsupportedDBErrorTextBox.Name = "unsupportedDBErrorTextBox";
            unsupportedDBErrorTextBox.ReadOnly = true;
            unsupportedDBErrorTextBox.ScrollBars = ScrollBars.Vertical;
            unsupportedDBErrorTextBox.Size = new Size(876, 641);
            unsupportedDBErrorTextBox.TabIndex = 3;
            unsupportedDBErrorTextBox.Visible = false;
            // 
            // useFirstColumnAsRowHeader
            // 
            useFirstColumnAsRowHeader.AutoSize = true;
            useFirstColumnAsRowHeader.Location = new Point(422, 4);
            useFirstColumnAsRowHeader.Name = "useFirstColumnAsRowHeader";
            useFirstColumnAsRowHeader.Size = new Size(183, 17);
            useFirstColumnAsRowHeader.TabIndex = 4;
            useFirstColumnAsRowHeader.Text = "Use First Column As Row Header";
            useFirstColumnAsRowHeader.UseVisualStyleBackColor = true;
            useFirstColumnAsRowHeader.CheckedChanged += checkBox_CheckedChanged;
            // 
            // showAllColumns
            // 
            showAllColumns.AutoSize = true;
            showAllColumns.Location = new Point(741, 4);
            showAllColumns.Name = "showAllColumns";
            showAllColumns.Size = new Size(108, 17);
            showAllColumns.TabIndex = 6;
            showAllColumns.Text = "Show all columns";
            showAllColumns.UseVisualStyleBackColor = true;
            showAllColumns.CheckedChanged += showAllColumns_CheckedChanged;
            // 
            // useComboBoxCells
            // 
            useComboBoxCells.AutoSize = true;
            settings3.IgnoreColumns = "";
            settings3.SettingsKey = "";
            settings3.ShowAllColumns = false;
            settings3.TwcThreadId = "10595000";
            settings3.UpdateOnStartup = false;
            settings3.UseComboboxCells = true;
            settings3.UseFirstColumnAsRowHeader = false;
            useComboBoxCells.Checked = settings3.UseComboboxCells;
            useComboBoxCells.CheckState = CheckState.Checked;
            useComboBoxCells.Location = new Point(611, 4);
            useComboBoxCells.Name = "useComboBoxCells";
            useComboBoxCells.Size = new Size(124, 17);
            useComboBoxCells.TabIndex = 5;
            useComboBoxCells.Text = "Use ComboBox Cells";
            useComboBoxCells.UseVisualStyleBackColor = true;
            useComboBoxCells.CheckedChanged += useComboBoxCells_CheckedChanged;
            // 
            // DBFileEditorControl
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(showAllColumns);
            Controls.Add(useComboBoxCells);
            Controls.Add(useFirstColumnAsRowHeader);
            Controls.Add(toolStrip);
            Controls.Add(dataGridView);
            Controls.Add(unsupportedDBErrorTextBox);
            Name = "DBFileEditorControl";
            Size = new Size(876, 669);
            Enter += DBFileEditorControl_Enter;
            ((ISupportInitialize)(dataGridView)).EndInit();
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        DataGridViewColumn createColumn(string columnName, FieldInfo fieldInfo, PackFile packFile, int fieldCount) 
        {
            DataGridViewColumn column = null;
            if (Settings.Default.UseComboboxCells)
            {
                try 
                {
                    SortedSet<string> items;
                    items = DBReferenceMap.Instance.resolveFromPackFile(fieldInfo.ForeignReference, packFile);

                    if (items != null) 
                    {
                        column = new DataGridViewComboBoxColumn { DataPropertyName = columnName };
                        var cb = (DataGridViewComboBoxColumn)column;
                        cb.Items.Add(string.Empty);

                        foreach (string item in items)
                            cb.Items.Add(item);
                    }
                } 
                catch (Exception x) 
                {
                    Console.WriteLine(x);
                }
            }

            if (column == null) 
            {
                column = new DataGridViewAutoFilterTextBoxColumn {DataPropertyName = columnName };
            }
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
            column.HeaderText = fieldInfo.Name + (Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, fieldInfo.Name) ? "*" : "");
            column.Tag = fieldInfo;
            column.Visible = !Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, fieldInfo.Name);

            if (column.Visible) 
            {
                int visibleColumnCount = Math.Min(fieldCount, 10);
                int columnWidth = (Width - dataGridView.Columns[0].Width) / visibleColumnCount;
                column.Width = columnWidth;
            }
            return column;
        }

        public void Open(PackedFile packedFile, PackFile packFile = null) {
            copiedRows.Clear();
            copyToolStripButton.Enabled = true;
            pasteToolStripButton.Enabled = false;
            string key = DBFile.typename(packedFile.FullPath);

            if (!DBTypeMap.Instance.IsSupported(key)) {
                showDBFileNotSupportedMessage("Sorry, this db file isn't supported yet.\r\n\r\nCurrently supported files:\r\n");
                var decoder = new DecodeTool.DecodeTool { TypeName = key, Bytes = packedFile.Data };
                decoder.ShowDialog();
                if (!DBTypeMap.Instance.IsSupported(key)) {
                    return;
                }
            }
            try {
                currentDBFile = new PackedFileDbCodec().readDbFile(packedFile);
            } catch {
                var decoder = new DecodeTool.DecodeTool { TypeName = key, Bytes = packedFile.Data };
                decoder.ShowDialog();
                try {
                    currentDBFile = new PackedFileDbCodec().readDbFile(packedFile);
                } catch {
                    return;
                }
            }
            dataGridView.DataSource = null;
            currentPackedFile = packedFile;
            TypeInfo info = currentDBFile.CurrentType;
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
                dataGridView.Columns.Add(createColumn(columnName, info.fields[num], packFile, info.fields.Count));
            }

            currentDataSet.Tables.Add(currentDataTable);
            currentDataTable.ColumnChanged += currentDataTable_ColumnChanged;
            currentDataTable.RowDeleting += currentDataTable_RowDeleted;
            currentDataTable.TableNewRow += currentDataTable_TableNewRow;

            for (num = 0; num < currentDBFile.Entries.Count; num++) {
                DataRow row = currentDataTable.NewRow();
                row[0] = num;
                for (int i = 1; i < currentDataTable.Columns.Count; i++) {
                    int num3 = Convert.ToInt32(currentDataTable.Columns[i].ColumnName);
                    row[i] = currentDBFile.Entries[num][num3].Value;
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

        private void pasteEvent() 
        {
            List<List<FieldInstance>> rows = copiedRows;
            int insertAtRow = dataGridView.CurrentCell.RowIndex;
            
            if (lastCopy == COPIED_TYPE.ROWS) 
            {
                insertAtRow++;
                foreach (List<FieldInstance> copied in rows)
                    createRow(copied, insertAtRow);
            } 
            else if (lastCopy == COPIED_TYPE.CELLS) 
            {
                int insertAtColumn = dataGridView.CurrentCell.ColumnIndex;
                for (int j = 0; j < copiedRows.Count; j++) 
                {
                    int row = insertAtRow + j;
                    DataRow dataRow = currentDataTable.Rows[row];
                    for (int i = 0; i < copiedRows[j].Count; i++) 
                    {
                        int col = insertAtColumn + i;
                        string val = copiedRows[j][i].Value;
                        
                        try 
                        {
                            if (copiedRows[j][i] != null)
                                dataRow[col] = val;
                        } 
                        catch (Exception e) 
                        {
                            MessageBox.Show(string.Format("Could not set {0}/{1} to '{2}': {3}", col, row, val, e));
                        }
                    }
                }
                currentPackedFile.Data = (PackedFileDbCodec.Encode(currentDBFile));
                dataGridView.Refresh();
            }
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
                dataGridView.TopLeftHeaderCell.Value = currentDBFile.Entries[0][0].Info.Name;
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
            Settings.Default.Save();
            if (currentPackedFile != null) 
            {
                // rebuild table
                Open(currentPackedFile);
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

                if (!dataChanged) 
                {
                    dataChanged = true;
                    MessageBox.Show("Don't forget to save your changes (DB Definitions->Save to Directory)");
                }
            }
        }

        private void setReferenceTarget(DataGridViewColumn column) {
            var info = (FieldInfo)column.Tag;
            string tableName = currentDBFile.CurrentType.name;
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
            Open(currentPackedFile);
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            DataGridViewColumn newColumn = dataGridView.Columns[e.ColumnIndex];

            if (e.Button == MouseButtons.Right) {
                var menu = new ContextMenu();
                var item = new MenuItem("Change Column Description", delegate {
                                                                             promptHeaderDescription(newColumn);
                                                                         });
                menu.MenuItems.Add(item);
                
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

                var info = newColumn.Tag as FieldInfo;
                if (info != null && info.ForeignReference != "") {
                    item = new MenuItem(string.Format("Clear Reference Target ({0})", info.ForeignReference), delegate {
                        info.ForeignReference = "";
                        // rebuild table to remove combo boxes if applicable
                        Open(currentPackedFile);
                    });
                    menu.MenuItems.Add(item);
                }

                string ignoreField = ((FieldInfo)newColumn.Tag).Name;
                bool ignored = Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, ignoreField);
                string itemText = ignored ? "Show Column" : "Hide Column";

                item = new MenuItem(itemText, delegate {
                                                      if (ignored) {
                                                          Settings.Default.UnignoreColumn(currentPackedFile.FullPath, ignoreField);
                                                      } else {
                                                          Settings.Default.IgnoreColumn(currentPackedFile.FullPath, ignoreField);
                                                      }
                                                      Settings.Default.Save();
                                                      applyColumnVisibility();
                                                  });

                menu.MenuItems.Add(item);

                item = new MenuItem("Clear Hide list for this table", delegate {
                                                                              Settings.Default.ResetIgnores(currentPackedFile.FullPath);
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
            if (currentPackedFile == null)
                return;

            for (int i = 0; i < dataGridView.ColumnCount; i++)
            {
                DataGridViewColumn column = dataGridView.Columns[i];
                if (column != null && column.Tag != null) 
                {
                    var info = ((FieldInfo)column.Tag);
                    bool show = !Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, info.Name);
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
            if (currentPackedFile != null) 
            {
                applyColumnVisibility();
            }
        }
    }
}
