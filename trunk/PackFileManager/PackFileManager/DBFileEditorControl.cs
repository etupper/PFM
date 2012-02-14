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
        enum COPIED_TYPE {
            NONE,
            ROWS,
            CELLS
        }

        private ToolStripButton addNewRowButton;
        private CheckBox useFirstColumnAsRowHeader;
        private ToolStripButton cloneCurrentRow;
        private IContainer components;
        private ToolStripButton copyToolStripButton;
        private DataSet currentDataSet;
        private DataTable currentDataTable;
        private DBFile currentDBFile;
        public PackedFile currentPackedFile;
        private DataGridView dataGridView;
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
        private bool dataChanged = false;
        private List<List<FieldInstance>> copiedRows = new List<List<FieldInstance>>();
        private COPIED_TYPE lastCopy = COPIED_TYPE.NONE;

        public DBFileEditorControl() {
            this.components = null;
            this.InitializeComponent();
            initTypeMap(Path.GetDirectoryName(Application.ExecutablePath));
            this.dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dataGridView.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            try {
                this.useFirstColumnAsRowHeader.Checked = Settings.Default.UseFirstColumnAsRowHeader;
                this.showAllColumns.Checked = Settings.Default.ShowAllColumns;
            } catch {
            }
            dataGridView.KeyUp += copyPaste;
        }

        private void copyPaste(object sender, KeyEventArgs arge) {
            if (currentPackedFile != null) {
                if (arge.Control) {
                    if (arge.KeyCode == Keys.C) {
                        copyEvent();
                    } else if (arge.KeyCode == Keys.V) {
                        pasteEvent();
                    }
                }
            }
        }

        private void addNewRowButton_Click(object sender, EventArgs e) {
            List<FieldInstance> newEntry = this.currentDBFile.GetNewEntry();
            int insertAtColumn = dataGridView.Rows.Count;
            if (dataGridView.CurrentCell != null) {
                insertAtColumn = dataGridView.CurrentCell.RowIndex + 1;
            }
            createRow(newEntry, insertAtColumn);
        }

        private void createRow(List<FieldInstance> newEntry, int index) {
            DataRow row = this.currentDataTable.NewRow();
            for (int i = 1; i < this.currentDataTable.Columns.Count; i++) {
                int num2 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                row[i] = Convert.ChangeType(newEntry[num2].Value, this.currentDataTable.Columns[i].DataType);
            }
            this.currentDBFile.Entries.Insert(index, newEntry);
            currentDataTable.Rows.InsertAt(row, index);
            this.dataGridView.FirstDisplayedScrollingRowIndex = index;
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e) {
            if (dataGridView.Columns.Count > 0) {
                this.toggleFirstColumnAsRowHeader(this.useFirstColumnAsRowHeader.Checked);
                Settings.Default.UseFirstColumnAsRowHeader = this.useFirstColumnAsRowHeader.Checked;
                Settings.Default.Save();
            }
        }

        private void cloneCurrentRow_Click(object sender, EventArgs e) {
            int num = (this.dataGridView.SelectedRows.Count == 1) ? this.dataGridView.SelectedRows[0].Index : this.dataGridView.SelectedCells[0].RowIndex;
            int index = this.currentDataTable.Rows.IndexOf((this.dataGridView.Rows[num].DataBoundItem as DataRowView).Row);
            DataRow row = this.currentDataTable.NewRow();
            List<FieldInstance> newEntry = this.currentDBFile.GetNewEntry();
            List<FieldInstance> list2 = this.currentDBFile.Entries[index];
            for (int i = 1; i < this.currentDataTable.Columns.Count; i++) {
                int num4 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                newEntry[num4].Value = list2[num4].Value;
                if (!this.currentDataTable.Columns[i].DataType.ToString().Contains("string")) {
                    row[i] = list2[num4].Value;
                }
            }
            this.currentDBFile.Entries.Add(newEntry);
            this.currentDataTable.Rows.Add(row);
            this.dataGridView.FirstDisplayedScrollingRowIndex = this.dataGridView.RowCount - 1;
        }

        private void compatibilityMode_1_0_ToolStripMenuItem_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.Save();
        }

        private void initTypeMap(string path) {
            try {
                DBTypeMap.Instance.initializeTypeMap(path);
                //DBTypeMap.Instance.fromXmlSchema(path);
                //DBReferenceMap.Instance.load (path);
            } catch (Exception e) {
                if (MessageBox.Show(string.Format("Could not initialize type map: {0}.\nTry autoupdate?", e.Message),
                    "Initialize failed", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes) {
                    PackFileManagerForm.tryUpdate();
                }
            }
        }

        private void copyEvent() {
            if (currentDBFile == null) {
                return;
            }
            copiedRows = new List<List<FieldInstance>>();
            if (dataGridView.SelectedRows.Count != 0) {
                DataGridViewSelectedRowCollection selected = dataGridView.SelectedRows;
                foreach (DataGridViewRow row in selected) {
                    List<FieldInstance> toCopy = currentDBFile.Entries[row.Index];
                    List<FieldInstance> copy = new List<FieldInstance>(toCopy.Count);
                    toCopy.ForEach(field => copy.Add(new FieldInstance(field.Info, field.Value)));
                    copiedRows.Add(copy);
                    lastCopy = COPIED_TYPE.ROWS;
                }
            } else {
                DataGridViewSelectedCellCollection cells = dataGridView.SelectedCells;
                copiedRows = new List<List<FieldInstance>>();
                int minColumn = dataGridView.ColumnCount;
                int maxColumn = -1;
                int minRow = dataGridView.RowCount;
                int maxRow = -1;
                foreach (DataGridViewCell cell in cells) {
                    minColumn = Math.Min(minColumn, cell.ColumnIndex);
                    maxColumn = Math.Max(maxColumn, cell.ColumnIndex);
                    minRow = Math.Min(minRow, cell.RowIndex);
                    maxRow = Math.Max(maxRow, cell.RowIndex);
                }
                for (int j = minRow; j <= maxRow; j++) {
                    DataRowView dataRowView = this.dataGridView.Rows[j].DataBoundItem as DataRowView;
                    List<FieldInstance> copy = new List<FieldInstance>(maxColumn - minColumn);
                    for (int i = minColumn; i <= maxColumn; i++) {
                        FieldInfo info = (FieldInfo)dataGridView.Columns[i].Tag;
                        Console.WriteLine("{1}: {0}", dataRowView[i], info.TypeName);
                        copy.Add(new FieldInstance(info, dataRowView[i].ToString()));
                    }
                    copiedRows.Add(copy);
                }
                lastCopy = COPIED_TYPE.CELLS;
            }
            pasteToolStripButton.Enabled = copiedRows.Count != 0;
        }

        private void copyToolStripButton_Click(object sender, EventArgs e) {
            this.copyEvent();
        }

        private void currentDataTable_ColumnChanged(object sender, DataColumnChangeEventArgs e) {
            if (((this.dataGridView.DataSource != null) && (e.Row.RowState != DataRowState.Detached)) && (Convert.ToInt32(e.Column.ColumnName) != -1)) {
                object proposedValue = e.ProposedValue;
                int num = Convert.ToInt32(e.Column.ColumnName);
                List<FieldInstance> list = this.currentDBFile.Entries[this.currentDataTable.Rows.IndexOf(e.Row)];
                FieldInstance instance = list[num];
                string str = (proposedValue == null) ? "" : proposedValue.ToString();
                instance.Value = str;
                this.currentPackedFile.Data = this.currentDBFile.GetBytes();
            }
        }

        private void currentDataTable_RowDeleted(object sender, DataRowChangeEventArgs e) {
            if (e.Action != DataRowAction.Delete) {
                throw new InvalidDataException("wtf?");
            }
            this.currentDBFile.Entries.RemoveAt(this.currentDataTable.Rows.IndexOf(e.Row));
            this.currentPackedFile.Data = this.currentDBFile.GetBytes();
        }

        private void currentDataTable_TableNewRow(object sender, DataTableNewRowEventArgs e) {
            if (this.dataGridView.DataSource != null) {
                this.currentPackedFile.Data = (this.currentDBFile.GetBytes());
            }
        }

        private void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e) {
            if ((((this.currentDBFile.CurrentType.fields.Count > 1) && this.useFirstColumnAsRowHeader.Checked) && ((e.ColumnIndex == -1) && (e.RowIndex > -1))) && (e.RowIndex < (this.dataGridView.DataSource as BindingSource).Count)) {
                e.PaintBackground(e.ClipBounds, false);
                string s = ((this.dataGridView.DataSource as BindingSource)[e.RowIndex] as DataRowView)[1].ToString();
                float num = Convert.ToSingle((int)(e.CellBounds.Height - this.dataGridView.DefaultCellStyle.Font.Height)) / 2f;
                RectangleF cellBounds = e.CellBounds;
                cellBounds.Inflate(0f, -num);
                cellBounds.X += 5f;
                cellBounds.Width -= 5f;
                using (Graphics graphics = e.Graphics) {
                    graphics.DrawString(s, this.dataGridView.DefaultCellStyle.Font, Brushes.Black, cellBounds);
                }
                e.Handled = true;
            }
        }

        private void dataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e) {
        }

        private void dataGridView_KeyDown(object sender, KeyEventArgs e) {
            object selectedCells = this.dataGridView.SelectedCells;
            if ((Control.ModifierKeys == Keys.Control) && (e.KeyValue == 0x63)) {
                Clipboard.SetData(DataFormats.UnicodeText, selectedCells);
            }
            if ((e.Modifiers == Keys.ControlKey) && (e.KeyCode == Keys.V)) {
                Clipboard.GetData(DataFormats.UnicodeText);
            }
        }

        private void dataGridView_KeyPress(object sender, KeyPressEventArgs e) {
            object selectedCells = this.dataGridView.SelectedCells;
            if ((Control.ModifierKeys == Keys.Control) && (e.KeyChar == 'c')) {
                Clipboard.SetData(DataFormats.UnicodeText, selectedCells);
            }
            if ((Control.ModifierKeys == Keys.Control) && (e.KeyChar == 'v')) {
                Clipboard.GetData(DataFormats.UnicodeText);
            }
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e) {
            this.cloneCurrentRow.Enabled = (this.dataGridView.SelectedRows.Count == 1) || (this.dataGridView.SelectedCells.Count == 1);
        }

        private void DBFileEditorControl_Enter(object sender, EventArgs e) {
            pasteToolStripButton.Enabled = copiedRows.Count != 0;
        }

        protected override void Dispose(bool disposing) {
            if (disposing && (this.components != null)) {
                Utilities.DisposeHandlers(this);
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void exportButton_Click(object sender, EventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = this.currentDBFile.CurrentType.name + ".tsv"
            };
            if (dialog.ShowDialog() == DialogResult.OK) {
                Stream stream = new FileStream(dialog.FileName, FileMode.Create);
                //StreamWriter writer = new StreamWriter (dialog.FileName);
                try {
                    new TextDbCodec().writeDbFile(stream, currentDBFile);
                    stream.Close();
                    //this.currentDBFile.Export (writer);
                } catch (DBFileNotSupportedException exception) {
                    this.showDBFileNotSupportedMessage(exception.Message);
                } finally {
                    if (stream != null) {
                        stream.Dispose();
                    }
                }
            }
        }

        private void importButton_Click(object sender, EventArgs e) {
            this.openDBFileDialog.FileName = this.currentDBFile.CurrentType.name + ".tsv";
            if (this.openDBFileDialog.ShowDialog() == DialogResult.OK) {
                using (StreamReader reader = new StreamReader(this.openDBFileDialog.FileName)) {
                    try {
                        using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(openDBFileDialog.FileName))) {
                            this.currentDBFile.Import((new PackedFileDbCodec()).readDbFile(currentPackedFile.FullPath, stream));
                        }
                    } catch (DBFileNotSupportedException exception) {
                        this.showDBFileNotSupportedMessage(exception.Message);
                    }
                    this.currentPackedFile.Data = (this.currentDBFile.GetBytes());
                    this.Open(this.currentPackedFile);
                }
            }
        }

        private void InitializeComponent() {
            PackFileManager.Properties.Settings settings3 = new PackFileManager.Properties.Settings();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.addNewRowButton = new System.Windows.Forms.ToolStripButton();
            this.cloneCurrentRow = new System.Windows.Forms.ToolStripButton();
            this.copyToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.pasteToolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exportButton = new System.Windows.Forms.ToolStripButton();
            this.importButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.openDBFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.unsupportedDBErrorTextBox = new System.Windows.Forms.TextBox();
            this.useFirstColumnAsRowHeader = new System.Windows.Forms.CheckBox();
            this.showAllColumns = new System.Windows.Forms.CheckBox();
            this.useComboBoxCells = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
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
            this.dataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dataGridView_CellPainting);
            this.dataGridView.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dataGridView_DataBindingComplete);
            this.dataGridView.SelectionChanged += new System.EventHandler(this.dataGridView_SelectionChanged);
            this.dataGridView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.dataGridView_KeyPress);
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addNewRowButton,
            this.cloneCurrentRow,
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
            // cloneCurrentRow
            // 
            this.cloneCurrentRow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cloneCurrentRow.Enabled = false;
            this.cloneCurrentRow.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cloneCurrentRow.Name = "cloneCurrentRow";
            this.cloneCurrentRow.Size = new System.Drawing.Size(68, 22);
            this.cloneCurrentRow.Text = "Clone Row";
            this.cloneCurrentRow.ToolTipText = "Clone Current Row";
            this.cloneCurrentRow.Click += new System.EventHandler(this.cloneCurrentRow_Click);
            // 
            // copyToolStripButton
            // 
            this.copyToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.copyToolStripButton.Enabled = false;
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
            // openDBFileDialog
            // 
            this.openDBFileDialog.Filter = "Tab separated values (TSV)|*.tsv|Any File|*.*";
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
            this.useFirstColumnAsRowHeader.CheckedChanged += new System.EventHandler(this.checkBox_CheckedChanged);
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
            settings3.IgnoreColumns = "";
            settings3.SettingsKey = "";
            settings3.ShowAllColumns = false;
            settings3.TwcThreadId = "10595000";
            settings3.UpdateOnStartup = false;
            settings3.UseComboboxCells = true;
            settings3.UseFirstColumnAsRowHeader = false;
            this.useComboBoxCells.Checked = settings3.UseComboboxCells;
            this.useComboBoxCells.CheckState = System.Windows.Forms.CheckState.Checked;
            this.useComboBoxCells.Location = new System.Drawing.Point(611, 4);
            this.useComboBoxCells.Name = "useComboBoxCells";
            this.useComboBoxCells.Size = new System.Drawing.Size(124, 17);
            this.useComboBoxCells.TabIndex = 5;
            this.useComboBoxCells.Text = "Use ComboBox Cells";
            this.useComboBoxCells.UseVisualStyleBackColor = true;
            this.useComboBoxCells.CheckedChanged += new System.EventHandler(this.useComboBoxCells_CheckedChanged);
            // 
            // DBFileEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.showAllColumns);
            this.Controls.Add(this.useComboBoxCells);
            this.Controls.Add(this.useFirstColumnAsRowHeader);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.unsupportedDBErrorTextBox);
            this.Name = "DBFileEditorControl";
            this.Size = new System.Drawing.Size(876, 669);
            this.Enter += new System.EventHandler(this.DBFileEditorControl_Enter);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        DataGridViewColumn createColumn(string columnName, FieldInfo fieldInfo, PackFile packFile, int fieldCount) {
            DataGridViewColumn column = null;
            if (Settings.Default.UseComboboxCells && packFile != null) {
                try {
                    SortedSet<string> items;
                    if (packFile != null) {
                        items = DBReferenceMap.Instance.resolveFromPackFile(fieldInfo.ForeignReference, packFile);
                    } else {
                        items = DBReferenceMap.Instance[fieldInfo.ForeignReference];
                    }
                    if (items != null) {
                        column = new DataGridViewComboBoxColumn {
                            DataPropertyName = columnName
                        };
                        DataGridViewComboBoxColumn cb = (DataGridViewComboBoxColumn)column;
                        cb.Items.Add(string.Empty);
                        foreach (string item in items) {
                            cb.Items.Add(item);
                        }
                    }
                } catch (Exception x) {
                    Console.WriteLine(x);
                }
            }
            if (column == null) {
                column = new DataGridViewAutoFilterTextBoxColumn {
                    DataPropertyName = columnName,
                    AutomaticSortingEnabled = false
                };
            }
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
            column.HeaderText = fieldInfo.Name + (Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, fieldInfo.Name) ? "*" : "");
            column.Tag = fieldInfo;
            column.Visible = !Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, fieldInfo.Name);
            if (column.Visible) {
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
            int num;
            string key = DBFile.typename(packedFile.FullPath);
            if (!DBTypeMap.Instance.IsSupported(key)) {
                this.showDBFileNotSupportedMessage("Sorry, this db file isn't supported yet.\r\n\r\nCurrently supported files:\r\n");
                DecodeTool.DecodeTool decoder = new DecodeTool.DecodeTool();
                decoder.TypeName = key;
                decoder.Bytes = packedFile.Data;
                decoder.ShowDialog();
            } else {
                try {
                    this.currentDBFile = new PackedFileDbCodec().readDbFile(packedFile);
                } catch {
                    DecodeTool.DecodeTool decoder = new DecodeTool.DecodeTool();
                    decoder.TypeName = key;
                    decoder.Bytes = packedFile.Data;
                    decoder.ShowDialog();
                    return;
                }
                this.dataGridView.DataSource = null;
                this.currentPackedFile = packedFile;
                TypeInfo info = currentDBFile.CurrentType;
                this.currentDataSet = new DataSet(info.name + "_DataSet");
                this.currentDataTable = new DataTable(info.name + "_DataTable");
                this.currentDataTable.Columns.Add(new DataColumn("#", System.Type.GetType("System.UInt32")));
                DataGridViewTextBoxColumn dataGridViewColumn = new DataGridViewTextBoxColumn {
                    DataPropertyName = "#",
                    Visible = false
                };
                this.dataGridView.Columns.Add(dataGridViewColumn);
                for (num = 0; num < info.fields.Count; num++) {
                    string columnName = num.ToString();
                    DataColumn column = new DataColumn(columnName);
                    if (info.fields[num].TypeCode == TypeCode.Empty) {
                        column.DataType = System.Type.GetType("System.String");
                    } else {
                        column.DataType = System.Type.GetType("System." + info.fields[num].TypeCode);
                    }
                    this.currentDataTable.Columns.Add(column);
                    this.dataGridView.Columns.Add(createColumn(columnName, info.fields[num], packFile, info.fields.Count));
                }
                this.currentDataSet.Tables.Add(this.currentDataTable);
                this.currentDataTable.ColumnChanged += new DataColumnChangeEventHandler(this.currentDataTable_ColumnChanged);
                this.currentDataTable.TableNewRow += new DataTableNewRowEventHandler(this.currentDataTable_TableNewRow);
                for (num = 0; num < this.currentDBFile.Entries.Count; num++) {
                    DataRow row = this.currentDataTable.NewRow();
                    row[0] = num;
                    for (int i = 1; i < this.currentDataTable.Columns.Count; i++) {
                        int num3 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                        row[i] = this.currentDBFile.Entries[num][num3].Value;
                    }
                    this.currentDataTable.Rows.Add(row);
                }

                this.dataGridView.DataSource = new BindingSource(this.currentDataSet, info.name + "_DataTable");
                this.addNewRowButton.Enabled = true;
                this.importButton.Enabled = true;
                this.exportButton.Enabled = true;
                this.dataGridView.Visible = true;
                this.unsupportedDBErrorTextBox.Visible = false;
                toggleFirstColumnAsRowHeader(Settings.Default.UseFirstColumnAsRowHeader);
            }
        }

        private void pasteEvent() {
            List<List<FieldInstance>> rows = copiedRows;
            int insertAtRow = dataGridView.CurrentCell.RowIndex;
            if (lastCopy == COPIED_TYPE.ROWS) {
                insertAtRow++;
                foreach (List<FieldInstance> copied in rows) {
                    createRow(copied, insertAtRow);
                }
            } else if (lastCopy == COPIED_TYPE.CELLS) {
                int insertAtColumn = dataGridView.CurrentCell.ColumnIndex;
                for (int j = 0; j < copiedRows.Count; j++) {
                    int row = insertAtRow + j;
                    DataRow dataRow = currentDataTable.Rows[row];
                    for (int i = 0; i < copiedRows[j].Count; i++) {
                        int col = insertAtColumn + i;
                        string val = copiedRows[j][i].Value;
                        try {
                            if (copiedRows[j][i] != null) {
                                dataRow[col] = val;
                            }
                        } catch (Exception e) {
                            MessageBox.Show(string.Format("Could not set {0}/{1} to '{2}': {3}", col, row, val, e));
                        }
                    }
                }
                currentPackedFile.Data = (currentDBFile.GetBytes());
                dataGridView.Refresh();
            }
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e) {
            this.pasteEvent();
        }

        private void showDBFileNotSupportedMessage(string message) {
            this.dataGridView.Visible = false;
            this.unsupportedDBErrorTextBox.Visible = true;
            this.unsupportedDBErrorTextBox.Text = string.Format("{0}{1}", message, string.Join("\r\n", DBTypeMap.Instance.DBFileTypes));
            // this.unsupportedDBErrorTextBox.Text = message;
            this.addNewRowButton.Enabled = false;
            this.importButton.Enabled = false;
            this.exportButton.Enabled = false;
        }

        private void toggleFirstColumnAsRowHeader(bool isChecked) {
            this.dataGridView.Columns[0].Frozen = isChecked;
            if (this.dataGridView.ColumnCount > 1) {
                this.dataGridView.Columns[1].Frozen = isChecked;
            }
            if (isChecked) {
                this.dataGridView.TopLeftHeaderCell.Value = this.currentDBFile.Entries[0][0].Info.Name;
                this.dataGridView.RowHeadersVisible = false;
            } else {
                this.dataGridView.TopLeftHeaderCell.Value = "";
                this.dataGridView.RowHeadersVisible = true;
            }
        }

        private void useComboBoxCells_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.UseComboboxCells = useComboBoxCells.Checked;
            Settings.Default.Save();
            if (currentPackedFile != null) {
                // rebuild table
                Open(currentPackedFile, null);
            }
        }

        private void promptHeaderDescription(DataGridViewColumn newColumn) {
            FieldInfo info = (FieldInfo)newColumn.Tag;
            InputBox box = new InputBox { Text = "Enter new description", Input = info.Name };
            if (box.ShowDialog() == DialogResult.OK) {
                info.Name = box.Input;
                newColumn.HeaderText = info.Name;

                if (!dataChanged) {
                    dataChanged = true;
                    MessageBox.Show("Don't forget to save your changes (DB Definitions->Save to Directory)");
                }
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e) {
            DataGridViewColumn newColumn = dataGridView.Columns[e.ColumnIndex];

            if (e.Button == System.Windows.Forms.MouseButtons.Right) {
                ContextMenu menu = new ContextMenu();
                MenuItem item = new MenuItem("Change Column Description", new EventHandler(delegate(object s, EventArgs args) {
                    promptHeaderDescription(newColumn);
                }));
                menu.MenuItems.Add(item);

                string ignoreField = ((FieldInfo)newColumn.Tag).Name;
                bool ignored = Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, ignoreField);
                string itemText = ignored ? "Show Column" : "Hide Column";
                item = new MenuItem(itemText, new EventHandler(delegate(object s, EventArgs args) {
                    if (ignored) {
                        Settings.Default.UnignoreColumn(currentPackedFile.FullPath, ignoreField);
                    } else {
                        Settings.Default.IgnoreColumn(currentPackedFile.FullPath, ignoreField);
                    }
                    Settings.Default.Save();
                    applyColumnVisibility();
                }));
                menu.MenuItems.Add(item);

                item = new MenuItem("Clear Hide list for this table", new EventHandler(delegate(object s, EventArgs args) {
                    Settings.Default.ResetIgnores(currentPackedFile.FullPath);
                    Settings.Default.Save();
                    applyColumnVisibility();
                }));
                menu.MenuItems.Add(item);
                item = new MenuItem("Clear Hide list for all tables", new EventHandler(delegate(object s, EventArgs args) {
                    Settings.Default.IgnoreColumns = "";
                    Settings.Default.Save();
                    applyColumnVisibility();
                }));
                menu.MenuItems.Add(item);
                menu.Show(dataGridView, e.Location);
                return;
            }

            DataGridViewColumn oldColumn = dataGridView.SortedColumn;
            ListSortDirection direction;
            // If oldColumn is null, then the DataGridView is not sorted.
            if (oldColumn != null) {
                // Sort the same column again, reversing the SortOrder.
                if (oldColumn == newColumn &&
                    dataGridView.SortOrder == SortOrder.Ascending) {
                    direction = ListSortDirection.Descending;
                } else {
                    // Sort a new column and remove the old SortGlyph.
                    direction = ListSortDirection.Ascending;
                    oldColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
                }
            } else {
                direction = ListSortDirection.Ascending;
            }

            // Sort the selected column.
            dataGridView.Sort(newColumn, direction);
            newColumn.HeaderCell.SortGlyphDirection =
                direction == ListSortDirection.Ascending ?
                SortOrder.Ascending : SortOrder.Descending;
        }

        private void applyColumnVisibility() {
            if (currentPackedFile == null) {
                return;
            }
            for (int i = 0; i < dataGridView.ColumnCount; i++) {
                DataGridViewColumn column = dataGridView.Columns[i];
                if (column != null && column.Tag != null) {
                    FieldInfo info = ((FieldInfo)column.Tag);
                    bool show = !Settings.Default.IsColumnIgnored(currentPackedFile.FullPath, info.Name);
                    column.Visible = (Settings.Default.ShowAllColumns || show);
                    column.HeaderText = info.Name + (show ? "" : "*");
                } else {
                    Console.WriteLine("no column?");
                }
            }
        }

        private void showAllColumns_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.ShowAllColumns = showAllColumns.Checked;
            Settings.Default.Save();
            if (currentPackedFile != null) {
                applyColumnVisibility();
            }
        }
    }
}
