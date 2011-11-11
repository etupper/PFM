﻿namespace PackFileManager
{
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

    public class DBFileEditorControl : UserControl
    {
        private ToolStripButton addNewRowButton;
        private CheckBox checkBox1;
        private ToolStripButton cloneCurrentRow;
        private IContainer components;
        private ToolStripButton copyToolStripButton;
        private DataSet currentDataSet;
        private DataTable currentDataTable;
        private DBFile currentDBFile;
        private PackedFile currentPackedFile;
        private DataGridView dataGridView;
        private ToolStripButton exportButton;
        private ToolStripButton importButton;
        public OpenFileDialog openDBFileDialog;
        private ToolStripButton pasteToolStripButton;
        private ToolStrip toolStrip;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripSeparator toolStripSeparator2;
        private TextBox unsupportedDBErrorTextBox;
        private ToolStripMenuItem useOnlineDefinitionsToolStripMenuItem;

        public DBFileEditorControl()
        {
            this.components = null;
            this.InitializeComponent();
            initTypeMap(Path.GetDirectoryName(Application.ExecutablePath));
            this.dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        }

        public DBFileEditorControl(PackedFile packedFile)
        {
            this.components = null;
            this.InitializeComponent();
            initTypeMap(Path.GetDirectoryName(Application.ExecutablePath));
            this.Open(packedFile);
            if (this.dataGridView.Columns.Count > 0)
            {
                this.dataGridView.Columns[0].Width = 40;
                this.dataGridView.Columns[0].CellTemplate.Value = "";
            }
            this.toggleFirstColumnAsRowHeader(this.checkBox1.Checked);
        }

        private void addNewRowButton_Click(object sender, EventArgs e)
        {
            DataRow row = this.currentDataTable.NewRow();
            List<FieldInstance> newEntry = this.currentDBFile.GetNewEntry();
            for (int i = 1; i < this.currentDataTable.Columns.Count; i++)
            {
                int num2 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                row[i] = Convert.ChangeType(newEntry[num2].Value, this.currentDataTable.Columns[i].DataType);
            }
            this.currentDBFile.Entries.Add(newEntry);
            this.currentDataTable.Rows.Add(row);
            this.dataGridView.FirstDisplayedScrollingRowIndex = this.dataGridView.RowCount - 1;
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            this.toggleFirstColumnAsRowHeader(this.checkBox1.Checked);
            Settings.Default.UseFirstColumnAsRowHeader = this.checkBox1.Checked;
            Settings.Default.Save();
        }

        private void checkClipboardForPaste()
        {
            this.pasteToolStripButton.Enabled = false;
            if (Clipboard.ContainsText() && (Clipboard.GetText().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0].Split("\t".ToCharArray()).Length == (this.currentDataTable.Columns.Count - 1)))
            {
                this.pasteToolStripButton.Enabled = true;
            }
        }

        private void cloneCurrentRow_Click(object sender, EventArgs e)
        {
            int num = (this.dataGridView.SelectedRows.Count == 1) ? this.dataGridView.SelectedRows[0].Index : this.dataGridView.SelectedCells[0].RowIndex;
            int index = this.currentDataTable.Rows.IndexOf((this.dataGridView.Rows[num].DataBoundItem as DataRowView).Row);
            DataRow row = this.currentDataTable.NewRow();
            List<FieldInstance> newEntry = this.currentDBFile.GetNewEntry();
            List<FieldInstance> list2 = this.currentDBFile.Entries[index];
            for (int i = 1; i < this.currentDataTable.Columns.Count; i++)
            {
                int num4 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                newEntry[num4].Value = list2[num4].Value;
                if (!this.currentDataTable.Columns[i].DataType.ToString().Contains("string"))
                {
                    row[i] = list2[num4].Value;
                }
            }
            this.currentDBFile.Entries.Add(newEntry);
            this.currentDataTable.Rows.Add(row);
            this.dataGridView.FirstDisplayedScrollingRowIndex = this.dataGridView.RowCount - 1;
        }

        private void compatibilityMode_1_0_ToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void initTypeMap(string path)
        {
            try
            {
                DBTypeMap.Instance.initializeTypeMap(path);
            }
            catch (Exception e)
            {
                if (MessageBox.Show(string.Format("Could not initialize type map: {0}.\nTry autoupdate?", e.Message), 
                    "Initialize failed", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    PackFileManagerForm.tryUpdate();
                }
            }
        }

        private void copyEvent()
        {
            int num = (this.dataGridView.SelectedRows.Count == 1) ? this.dataGridView.SelectedRows[0].Index : this.dataGridView.SelectedCells[0].RowIndex;
            int index = this.currentDataTable.Rows.IndexOf((this.dataGridView.Rows[num].DataBoundItem as DataRowView).Row);
            List<FieldInstance> list = this.currentDBFile.Entries[index];
            string[] strArray = new string[this.currentDataTable.Columns.Count - 1];
            for (int i = 1; i < this.currentDataTable.Columns.Count; i++)
            {
                int num4 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                strArray[i - 1] = list[num4].Value;
            }
            Clipboard.SetText(string.Join("\t", strArray) + "\r\n");
            this.checkClipboardForPaste();
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            this.copyEvent();
        }

        private void currentDataTable_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            if (((this.dataGridView.DataSource != null) && (e.Row.RowState != DataRowState.Detached)) && (Convert.ToInt32(e.Column.ColumnName) != -1))
            {
                object proposedValue = e.ProposedValue;
                int num = Convert.ToInt32(e.Column.ColumnName);
                List<FieldInstance> list = this.currentDBFile.Entries[this.currentDataTable.Rows.IndexOf(e.Row)];
                FieldInstance instance = list[num];
                string str = (proposedValue == null) ? "" : proposedValue.ToString();
                if ((num > 0) && (list[num - 1].Info.modifier == FieldInfo.Modifier.NextFieldIsConditional))
                {
                    list[num - 1].Value = list[num - 1].Info.GetConditionString(str.Length > 0);
                }
                instance.Value = str;
                this.currentPackedFile.ReplaceData(this.currentDBFile.GetBytes());
            }
        }

        private void currentDataTable_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
            if (e.Action != DataRowAction.Delete)
            {
                throw new InvalidDataException("wtf?");
            }
            this.currentDBFile.Entries.RemoveAt(this.currentDataTable.Rows.IndexOf(e.Row));
            this.currentPackedFile.ReplaceData(this.currentDBFile.GetBytes());
        }

        private void currentDataTable_TableNewRow(object sender, DataTableNewRowEventArgs e)
        {
            if (this.dataGridView.DataSource != null)
            {
                this.currentPackedFile.ReplaceData(this.currentDBFile.GetBytes());
            }
        }

        private void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if ((((this.currentDBFile.CurrentType.fields.Count > 1) && this.checkBox1.Checked) && ((e.ColumnIndex == -1) && (e.RowIndex > -1))) && (e.RowIndex < (this.dataGridView.DataSource as BindingSource).Count))
            {
                e.PaintBackground(e.ClipBounds, false);
                string s = ((this.dataGridView.DataSource as BindingSource)[e.RowIndex] as DataRowView)[1].ToString();
                float num = Convert.ToSingle((int) (e.CellBounds.Height - this.dataGridView.DefaultCellStyle.Font.Height)) / 2f;
                RectangleF cellBounds = e.CellBounds;
                cellBounds.Inflate(0f, -num);
                cellBounds.X += 5f;
                cellBounds.Width -= 5f;
                using (Graphics graphics = e.Graphics)
                {
                    graphics.DrawString(s, this.dataGridView.DefaultCellStyle.Font, Brushes.Black, cellBounds);
                }
                e.Handled = true;
            }
        }

        private void dataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
        }

        private void dataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            object selectedCells = this.dataGridView.SelectedCells;
            if ((Control.ModifierKeys == Keys.Control) && (e.KeyValue == 0x63))
            {
                Clipboard.SetData(DataFormats.UnicodeText, selectedCells);
            }
            if ((e.Modifiers == Keys.ControlKey) && (e.KeyCode == Keys.V))
            {
                Clipboard.GetData(DataFormats.UnicodeText);
            }
        }

        private void dataGridView_KeyPress(object sender, KeyPressEventArgs e)
        {
            object selectedCells = this.dataGridView.SelectedCells;
            if ((Control.ModifierKeys == Keys.Control) && (e.KeyChar == 'c'))
            {
                Clipboard.SetData(DataFormats.UnicodeText, selectedCells);
            }
            if ((Control.ModifierKeys == Keys.Control) && (e.KeyChar == 'v'))
            {
                Clipboard.GetData(DataFormats.UnicodeText);
            }
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            this.cloneCurrentRow.Enabled = (this.dataGridView.SelectedRows.Count == 1) || (this.dataGridView.SelectedCells.Count == 1);
            this.copyToolStripButton.Enabled = this.cloneCurrentRow.Enabled;
        }

        private void DBFileEditorControl_Enter(object sender, EventArgs e)
        {
            this.checkClipboardForPaste();
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
                FileName = this.currentDBFile.CurrentType.name + ".tsv"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                StreamWriter writer = new StreamWriter(dialog.FileName);
                try
                {
                    this.currentDBFile.Export(writer);
                }
                catch (DBFileNotSupportedException exception)
                {
                    this.showDBFileNotSupportedMessage(exception.Message);
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Dispose();
                    }
                }
            }
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            this.openDBFileDialog.FileName = this.currentDBFile.CurrentType.name + ".tsv";
            if (this.openDBFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamReader reader = new StreamReader(this.openDBFileDialog.FileName))
                {
                    try
                    {
                        this.currentDBFile.Import(reader);
                    }
                    catch (DBFileNotSupportedException exception)
                    {
                        this.showDBFileNotSupportedMessage(exception.Message);
                    }
                    this.currentPackedFile.ReplaceData(this.currentDBFile.GetBytes());
                    this.Open(this.currentPackedFile);
                }
            }
        }

        private void InitializeComponent()
        {
            ComponentResourceManager manager = new ComponentResourceManager(typeof(DBFileEditorControl));
            this.dataGridView = new DataGridView();
            this.toolStrip = new ToolStrip();
            this.addNewRowButton = new ToolStripButton();
            this.cloneCurrentRow = new ToolStripButton();
            this.copyToolStripButton = new ToolStripButton();
            this.pasteToolStripButton = new ToolStripButton();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.exportButton = new ToolStripButton();
            this.importButton = new ToolStripButton();
            this.toolStripSeparator2 = new ToolStripSeparator();
            this.useOnlineDefinitionsToolStripMenuItem = new ToolStripMenuItem();
            this.openDBFileDialog = new OpenFileDialog();
            this.unsupportedDBErrorTextBox = new TextBox();
            this.checkBox1 = new CheckBox();
            ((ISupportInitialize) this.dataGridView).BeginInit();
            this.toolStrip.SuspendLayout();
            base.SuspendLayout();
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.dataGridView.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
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
            this.dataGridView.CellPainting += new DataGridViewCellPaintingEventHandler(this.dataGridView_CellPainting);
            this.dataGridView.KeyPress += new KeyPressEventHandler(this.dataGridView_KeyPress);
            this.dataGridView.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(this.dataGridView_DataBindingComplete);
            this.dataGridView.SelectionChanged += new EventHandler(this.dataGridView_SelectionChanged);
            this.toolStrip.Items.AddRange(new ToolStripItem[] { this.addNewRowButton, this.cloneCurrentRow, this.copyToolStripButton, this.pasteToolStripButton, this.toolStripSeparator1, this.exportButton, this.importButton, this.toolStripSeparator2 });
            this.toolStrip.Location = new Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new Size(0x36c, 0x19);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip";
            this.addNewRowButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.addNewRowButton.Enabled = false;
            this.addNewRowButton.Image = (Image) manager.GetObject("addNewRowButton.Image");
            this.addNewRowButton.ImageTransparentColor = Color.Magenta;
            this.addNewRowButton.Name = "addNewRowButton";
            this.addNewRowButton.Size = new Size(0x3b, 0x16);
            this.addNewRowButton.Text = "Add Row";
            this.addNewRowButton.ToolTipText = "Add New Row";
            this.addNewRowButton.Click += new EventHandler(this.addNewRowButton_Click);
            this.cloneCurrentRow.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.cloneCurrentRow.Enabled = false;
            this.cloneCurrentRow.Image = (Image) manager.GetObject("cloneCurrentRow.Image");
            this.cloneCurrentRow.ImageTransparentColor = Color.Magenta;
            this.cloneCurrentRow.Name = "cloneCurrentRow";
            this.cloneCurrentRow.Size = new Size(0x44, 0x16);
            this.cloneCurrentRow.Text = "Clone Row";
            this.cloneCurrentRow.ToolTipText = "Clone Current Row";
            this.cloneCurrentRow.Click += new EventHandler(this.cloneCurrentRow_Click);
            this.copyToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.copyToolStripButton.Enabled = false;
            this.copyToolStripButton.Image = (Image) manager.GetObject("copyToolStripButton.Image");
            this.copyToolStripButton.ImageTransparentColor = Color.Magenta;
            this.copyToolStripButton.Name = "copyToolStripButton";
            this.copyToolStripButton.Size = new Size(0x41, 0x16);
            this.copyToolStripButton.Text = "&Copy Row";
            this.copyToolStripButton.ToolTipText = "Copy Current Row";
            this.copyToolStripButton.Click += new EventHandler(this.copyToolStripButton_Click);
            this.pasteToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.pasteToolStripButton.Enabled = false;
            this.pasteToolStripButton.Image = (Image) manager.GetObject("pasteToolStripButton.Image");
            this.pasteToolStripButton.ImageTransparentColor = Color.Magenta;
            this.pasteToolStripButton.Name = "pasteToolStripButton";
            this.pasteToolStripButton.Size = new Size(0x41, 0x16);
            this.pasteToolStripButton.Text = "&Paste Row";
            this.pasteToolStripButton.ToolTipText = "Paste Row from Clipboard";
            this.pasteToolStripButton.Click += new EventHandler(this.pasteToolStripButton_Click);
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new Size(6, 0x19);
            this.exportButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.exportButton.Enabled = false;
            this.exportButton.Image = (Image) manager.GetObject("exportButton.Image");
            this.exportButton.ImageTransparentColor = Color.Magenta;
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new Size(0x43, 0x16);
            this.exportButton.Text = "Export TSV";
            this.exportButton.ToolTipText = "Export to tab-separated values";
            this.exportButton.Click += new EventHandler(this.exportButton_Click);
            this.importButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.importButton.Enabled = false;
            this.importButton.Image = (Image) manager.GetObject("importButton.Image");
            this.importButton.ImageTransparentColor = Color.Magenta;
            this.importButton.Name = "importButton";
            this.importButton.Size = new Size(70, 0x16);
            this.importButton.Text = "Import TSV";
            this.importButton.ToolTipText = "Import from tab-separated values";
            this.importButton.Click += new EventHandler(this.importButton_Click);
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new Size(6, 0x19);
            this.useOnlineDefinitionsToolStripMenuItem.Checked = Settings.Default.UseOnlineDefinitions;
            this.useOnlineDefinitionsToolStripMenuItem.CheckOnClick = true;
            this.useOnlineDefinitionsToolStripMenuItem.Enabled = false;
            this.useOnlineDefinitionsToolStripMenuItem.Name = "useOnlineDefinitionsToolStripMenuItem";
            this.useOnlineDefinitionsToolStripMenuItem.Size = new Size(0xdb, 0x16);
            this.useOnlineDefinitionsToolStripMenuItem.Text = "Use online definitions";
            this.useOnlineDefinitionsToolStripMenuItem.CheckedChanged += new EventHandler(this.useOnlineDefinitionsToolStripMenuItem_CheckedChanged);
            this.openDBFileDialog.Filter = "Tab separated values (TSV)|*.tsv|Any File|*.*";
            this.unsupportedDBErrorTextBox.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.unsupportedDBErrorTextBox.Location = new Point(0, 0x1c);
            this.unsupportedDBErrorTextBox.Multiline = true;
            this.unsupportedDBErrorTextBox.Name = "unsupportedDBErrorTextBox";
            this.unsupportedDBErrorTextBox.ReadOnly = true;
            this.unsupportedDBErrorTextBox.ScrollBars = ScrollBars.Vertical;
            this.unsupportedDBErrorTextBox.Size = new Size(0x36c, 0x281);
            this.unsupportedDBErrorTextBox.TabIndex = 3;
            this.unsupportedDBErrorTextBox.Visible = false;
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new Point(0x1a6, 4);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new Size(0xb7, 0x11);
            this.checkBox1.TabIndex = 4;
            this.checkBox1.Text = "Use First Column As Row Header";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new EventHandler(this.checkBox_CheckedChanged);
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.checkBox1);
            base.Controls.Add(this.toolStrip);
            base.Controls.Add(this.dataGridView);
            base.Controls.Add(this.unsupportedDBErrorTextBox);
            base.Name = "DBFileEditorControl";
            base.Size = new Size(0x36c, 0x29d);
            base.Enter += new EventHandler(this.DBFileEditorControl_Enter);
            ((ISupportInitialize) this.dataGridView).EndInit();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        public void Open(PackedFile packedFile)
        {
            int num;
            TypeInfo[] type = new TypeInfo[DBTypeMap.Instance.HighestHeaderVersion];
            string[] strArray = Path.GetDirectoryName(packedFile.Filepath).Split(new char[] { '\\' });
            string key = strArray[strArray.Length - 1].Remove(strArray[strArray.Length - 1].Length - 7);
            bool flag = false;
            for (num = 0; num < type.Length; num++)
            {
                type[num] = new TypeInfo();
                if (DBTypeMap.Instance[num] != null)
                {
                    if (DBTypeMap.Instance[num].TryGetValue(key, out type[num]))
                    {
                        flag = true;
                    }
                }
            }
            if (!flag)
            {
                this.showDBFileNotSupportedMessage("Sorry, this db file isn't supported yet.\r\n\r\nCurrently supported files:\r\n");
            }
            else
            {
                this.dataGridView.DataSource = null;
                this.currentPackedFile = packedFile;
                this.currentDBFile = new DBFile(packedFile, type);
                TypeInfo info = type[this.currentDBFile.TotalwarHeaderVersion];
                this.currentDataSet = new DataSet(info.name + "_DataSet");
                this.currentDataTable = new DataTable(info.name + "_DataTable");
                this.currentDataTable.Columns.Add(new DataColumn("#", System.Type.GetType("System.UInt32")));
                DataGridViewTextBoxColumn dataGridViewColumn = new DataGridViewTextBoxColumn {
                    DataPropertyName = "#",
                    Visible = false
                };
                this.dataGridView.Columns.Add(dataGridViewColumn);
                for (num = 0; num < info.fields.Count; num++)
                {
                    if (info.fields[num].modifier == FieldInfo.Modifier.None)
                    {
                        string columnName = num.ToString();
                        DataColumn column = new DataColumn(columnName);
                        if ((info.fields[num].type.ToString() == TypeCode.Empty.ToString()) || ((num > 0) && (info.fields[num - 1].modifier == FieldInfo.Modifier.NextFieldRepeats)))
                        {
                            column.DataType = System.Type.GetType("System.String");
                        }
                        else
                        {
                            column.DataType = System.Type.GetType("System." + info.fields[num].type.ToString());
                        }
                        this.currentDataTable.Columns.Add(column);
                        PackTypeCode code1 = info.fields[num].type;
                        DataGridViewColumn column3 = new DataGridViewAutoFilterTextBoxColumn {
                            DataPropertyName = columnName,
                            HeaderText = info.fields[num].name
                        };
                        this.dataGridView.Columns.Add(column3);
                    }
                }
                this.currentDataSet.Tables.Add(this.currentDataTable);
                this.currentDataTable.ColumnChanged += new DataColumnChangeEventHandler(this.currentDataTable_ColumnChanged);
                this.currentDataTable.RowDeleting += new DataRowChangeEventHandler(this.currentDataTable_RowDeleted);
                this.currentDataTable.TableNewRow += new DataTableNewRowEventHandler(this.currentDataTable_TableNewRow);
                for (num = 0; num < this.currentDBFile.Entries.Count; num++)
                {
                    DataRow row = this.currentDataTable.NewRow();
                    row[0] = num;
                    for (int i = 1; i < this.currentDataTable.Columns.Count; i++)
                    {
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
            }
        }

        private void pasteEvent()
        {
            if (Clipboard.ContainsText())
            {
                string[] strArray2 = Clipboard.GetText().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0].Split("\t".ToCharArray());
                int num = (this.dataGridView.SelectedRows.Count == 1) ? this.dataGridView.SelectedRows[0].Index : this.dataGridView.SelectedCells[0].RowIndex;
                int index = this.currentDataTable.Rows.IndexOf((this.dataGridView.Rows[num].DataBoundItem as DataRowView).Row);
                DataRow row = this.currentDataTable.Rows[index];
                List<FieldInstance> list = this.currentDBFile.Entries[index];
                for (int i = 1; i < this.currentDataTable.Columns.Count; i++)
                {
                    int num4 = Convert.ToInt32(this.currentDataTable.Columns[i].ColumnName);
                    list[num4].Value = strArray2[i - 1];
                    row[i] = Convert.ChangeType(strArray2[i - 1], this.currentDataTable.Columns[i].DataType);
                }
                this.currentPackedFile.ReplaceData(this.currentDBFile.GetBytes());
                this.dataGridView.Refresh();
            }
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            this.pasteEvent();
        }

        private void showDBFileNotSupportedMessage(string message)
        {
            this.dataGridView.Visible = false;
            this.unsupportedDBErrorTextBox.Visible = true;
            this.unsupportedDBErrorTextBox.Text = message;
            /*for (int i = 0; i < Settings.Default.totalwarHeaderVersions; i++)
            {
                if (this.typeMap[i] != null)
                {
                    foreach (string str in this.typeMap[i].Keys)
                    {
                        this.unsupportedDBErrorTextBox.Text = this.unsupportedDBErrorTextBox.Text + str + "\r\n";
                    }
                }
            }*/
            foreach (string str in DBTypeMap.Instance.dbFileTypes)
            {
                this.unsupportedDBErrorTextBox.Text = this.unsupportedDBErrorTextBox.Text + str + "\r\n";
            }
            this.addNewRowButton.Enabled = false;
            this.importButton.Enabled = false;
            this.exportButton.Enabled = false;
        }

        private void toggleFirstColumnAsRowHeader(bool isChecked)
        {
            this.dataGridView.Columns[0].Frozen = isChecked;
            this.dataGridView.Columns[1].Frozen = isChecked;
            if (isChecked)
            {
                this.dataGridView.TopLeftHeaderCell.Value = this.currentDBFile.Entries[0][0].Info.name;
                this.dataGridView.RowHeadersVisible = false;
            }
            else
            {
                this.dataGridView.TopLeftHeaderCell.Value = "";
                this.dataGridView.RowHeadersVisible = true;
            }
        }

        [Obsolete]
        private void useOnlineDefinitionsToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.UseOnlineDefinitions = this.useOnlineDefinitionsToolStripMenuItem.Checked;
            Settings.Default.Save();
            initTypeMap(Directory.GetCurrentDirectory());
        }

        private void writeTypeMapSchema()
        {
        }
    }
}

