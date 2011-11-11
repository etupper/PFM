namespace PackFileManager
{
    using BrightIdeasSoftware;
    using Common;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class UnitVariantFileEditorControl : UserControl
    {
        private ToolStripMenuItem addReferenceToModelPartToolStripMenuItem;
        private IContainer components;
        private ContextMenuStrip contextMenuStrip1;
        private PackedFile currentPackedFile;
        public bool dataChanged;
        private ToolStripMenuItem exportTextToTSVToolStripMenuItem;
        private ToolStripMenuItem insertModelPartToolStripMenuItem;
        private OLVColumn olvColumn1;
        private OLVColumn olvColumn2;
        private OLVColumn olvColumn3;
        private OLVColumn olvColumn4;
        private OLVColumn olvColumn5;
        private OLVColumn olvColumn6;
        private OLVColumn olvColumn7;
        private OLVColumn olvColumn8;
        private OLVColumn olvColumn9;
        private ToolStripMenuItem removeModelPartToolStripMenuItem;
        private ToolStripMenuItem removeReferenceToModelPartToolStripMenuItem1;
        private RichTextBox richTextBox1;
        private RichTextBox richTextBox2;
        private RichTextBox richTextBox3;
        private RichTextBox richTextBox4;
        private RichTextBox richTextBox5;
        private RichTextBox richTextBox6;
        private TreeListView treeListView1;
        private UnitVariantFile unitVariantFile;

        public UnitVariantFileEditorControl()
        {
            this.dataChanged = false;
            this.components = null;
            this.InitializeComponent();
        }

        public UnitVariantFileEditorControl(PackedFile packedFile)
        {
            this.dataChanged = false;
            this.components = null;
            this.InitializeComponent();
            this.currentPackedFile = packedFile;
            this.unitVariantFile = new UnitVariantFile();
            this.unitVariantFile.setPackedFile(packedFile);
            this.treeListView1.AddObjects(this.unitVariantFile.UnitVariantObjects);
            this.richTextBox2.Text = this.unitVariantFile.Unknown1.ToString();
            string str = this.unitVariantFile.B1.ToString("x2") + this.unitVariantFile.B2.ToString("x2");
            this.richTextBox2.Text = "B1B2: " + str;
            this.richTextBox3.Text = "B1B2 as UInt32: " + this.unitVariantFile.Unknown2.ToString();
            this.richTextBox4.Text = "B1 as Single: " + this.unitVariantFile.B1.ToString();
            this.richTextBox5.Text = "B2 as Single: " + this.unitVariantFile.B2.ToString();
            this.richTextBox6.Text = this.unitVariantFile.NumEntries.ToString();
            this.treeListView1.CanExpandGetter = x => (x is UnitVariantObject) && (((UnitVariantObject) x).Num3 != 0);
            this.treeListView1.ChildrenGetter = delegate (object x) {
                UnitVariantObject obj2 = (UnitVariantObject) x;
                ArrayList list = new ArrayList();
                list.AddRange(obj2.MeshTextureList);
                return list;
            };
            this.setColumnWidth();
            this.treeListView1.CellEditFinishing += new CellEditEventHandler(this.treeListView1_CellEditFinishing);
            this.treeListView1.Sort(this.olvColumn2, SortOrder.Ascending);
        }

        private void addReference_Click(object sender, EventArgs e)
        {
            UnitVariantObject entry = new UnitVariantObject();
            if (this.treeListView1.SelectedObject is MeshTextureObject)
            {
                entry = (UnitVariantObject) this.treeListView1.GetParent(this.treeListView1.SelectedObject);
            }
            else
            {
                entry = (UnitVariantObject) this.treeListView1.SelectedObject;
            }
            MeshTextureObject mTO = new MeshTextureObject("NEW_ENTRY", "NEW_ENTRY", false, false);
            this.unitVariantFile.addMTO(entry, mTO);
            this.treeListView1.RefreshObjects(this.unitVariantFile.UnitVariantObjects);
            this.dataChanged = true;
            this.Refresh();
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

        private void exportTSV_Click(object sender, EventArgs e)
        {
            List<string> strings = new List<string>();
            for (int i = 0; i < this.treeListView1.Items.Count; i++)
            {
                OLVListItem item = (OLVListItem) this.treeListView1.Items[i];
                if (item.RowObject is UnitVariantObject)
                {
                    UnitVariantObject rowObject = (UnitVariantObject) item.RowObject;
                    strings.Add(rowObject.ModelPart.ToString() + "\t" + rowObject.Num1.ToString() + "\t" + rowObject.Num2.ToString() + "\t" + rowObject.Num3.ToString() + "\t" + rowObject.Num4.ToString());
                    if (rowObject.MeshTextureList != null)
                    {
                        foreach (MeshTextureObject obj3 in rowObject.MeshTextureList)
                        {
                            strings.Add("\t\t\t\t\t" + obj3.Mesh + "\t" + obj3.Texture + "\t" + obj3.Bool1.ToString() + "\t" + obj3.Bool2.ToString());
                        }
                    }
                }
            }
            IOFunctions.writeToTSVFile(strings);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.richTextBox1 = new RichTextBox();
            this.richTextBox2 = new RichTextBox();
            this.treeListView1 = new TreeListView();
            this.olvColumn1 = new OLVColumn();
            this.olvColumn2 = new OLVColumn();
            this.olvColumn3 = new OLVColumn();
            this.olvColumn4 = new OLVColumn();
            this.olvColumn5 = new OLVColumn();
            this.olvColumn6 = new OLVColumn();
            this.olvColumn7 = new OLVColumn();
            this.olvColumn8 = new OLVColumn();
            this.olvColumn9 = new OLVColumn();
            this.richTextBox3 = new RichTextBox();
            this.richTextBox4 = new RichTextBox();
            this.contextMenuStrip1 = new ContextMenuStrip(this.components);
            this.addReferenceToModelPartToolStripMenuItem = new ToolStripMenuItem();
            this.removeReferenceToModelPartToolStripMenuItem1 = new ToolStripMenuItem();
            this.insertModelPartToolStripMenuItem = new ToolStripMenuItem();
            this.removeModelPartToolStripMenuItem = new ToolStripMenuItem();
            this.exportTextToTSVToolStripMenuItem = new ToolStripMenuItem();
            this.richTextBox5 = new RichTextBox();
            this.richTextBox6 = new RichTextBox();
            ((ISupportInitialize) this.treeListView1).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            base.SuspendLayout();
            this.richTextBox1.BackColor = SystemColors.Control;
            this.richTextBox1.BorderStyle = BorderStyle.None;
            this.richTextBox1.Location = new Point(9, 7);
            this.richTextBox1.Multiline = false;
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new Size(0x60, 0x10);
            this.richTextBox1.TabIndex = 6;
            this.richTextBox1.Text = "Header Information";
            this.richTextBox1.WordWrap = false;
            this.richTextBox2.BackColor = Color.WhiteSmoke;
            this.richTextBox2.Location = new Point(0xa4, 3);
            this.richTextBox2.Multiline = false;
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.Size = new Size(0x5f, 20);
            this.richTextBox2.TabIndex = 2;
            this.richTextBox2.Text = "";
            this.richTextBox2.WordWrap = false;
            this.richTextBox2.MouseHover += new EventHandler(this.richTextBox2_MouseHover);
            this.treeListView1.AllColumns.Add(this.olvColumn1);
            this.treeListView1.AllColumns.Add(this.olvColumn2);
            this.treeListView1.AllColumns.Add(this.olvColumn3);
            this.treeListView1.AllColumns.Add(this.olvColumn4);
            this.treeListView1.AllColumns.Add(this.olvColumn5);
            this.treeListView1.AllColumns.Add(this.olvColumn6);
            this.treeListView1.AllColumns.Add(this.olvColumn7);
            this.treeListView1.AllColumns.Add(this.olvColumn8);
            this.treeListView1.AllColumns.Add(this.olvColumn9);
            this.treeListView1.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.treeListView1.CellEditActivation = ObjectListView.CellEditActivateMode.DoubleClick;
            this.treeListView1.Columns.AddRange(new ColumnHeader[] { this.olvColumn1, this.olvColumn2, this.olvColumn3, this.olvColumn4, this.olvColumn5, this.olvColumn6, this.olvColumn7, this.olvColumn8, this.olvColumn9 });
            this.treeListView1.Cursor = Cursors.Default;
            this.treeListView1.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            this.treeListView1.Location = new Point(0, 0x19);
            this.treeListView1.Name = "treeListView1";
            this.treeListView1.OwnerDraw = true;
            this.treeListView1.ShowGroups = false;
            this.treeListView1.Size = new Size(0x369, 0x284);
            this.treeListView1.TabIndex = 0;
            this.treeListView1.UseCompatibleStateImageBehavior = false;
            this.treeListView1.View = View.Details;
            this.treeListView1.VirtualMode = true;
            this.treeListView1.CellRightClick += new EventHandler<CellRightClickEventArgs>(this.treeListView1_CellRightClick);
            this.olvColumn1.AspectName = "ModelPart";
            this.olvColumn1.HeaderFont = null;
            this.olvColumn1.Text = "ModelPart";
            this.olvColumn1.Width = 200;
            this.olvColumn2.AspectName = "Num1";
            this.olvColumn2.HeaderFont = null;
            this.olvColumn2.Text = "ModelPart #";
            this.olvColumn3.AspectName = "Num2";
            this.olvColumn3.HeaderFont = null;
            this.olvColumn3.Text = "Unknown";
            this.olvColumn4.AspectName = "Num3";
            this.olvColumn4.HeaderFont = null;
            this.olvColumn4.Text = "Variations";
            this.olvColumn5.AspectName = "Num4";
            this.olvColumn5.HeaderFont = null;
            this.olvColumn5.Text = "Item #";
            this.olvColumn6.AspectName = "Mesh";
            this.olvColumn6.HeaderFont = null;
            this.olvColumn6.Text = "Mesh";
            this.olvColumn6.Width = 150;
            this.olvColumn7.AspectName = "Texture";
            this.olvColumn7.HeaderFont = null;
            this.olvColumn7.Text = "Texture";
            this.olvColumn7.Width = 150;
            this.olvColumn8.AspectName = "Bool1";
            this.olvColumn8.HeaderFont = null;
            this.olvColumn8.Text = "Bool1";
            this.olvColumn8.Width = 50;
            this.olvColumn9.AspectName = "Bool2";
            this.olvColumn9.HeaderFont = null;
            this.olvColumn9.Text = "Bool2";
            this.olvColumn9.Width = 50;
            this.richTextBox3.Location = new Point(0x109, 3);
            this.richTextBox3.Multiline = false;
            this.richTextBox3.Name = "richTextBox3";
            this.richTextBox3.ScrollBars = RichTextBoxScrollBars.None;
            this.richTextBox3.Size = new Size(0x87, 20);
            this.richTextBox3.TabIndex = 3;
            this.richTextBox3.Text = "";
            this.richTextBox3.WordWrap = false;
            this.richTextBox3.LostFocus += new EventHandler(this.richTextBox3_LostFocus);
            this.richTextBox3.MouseHover += new EventHandler(this.richTextBox3_MouseHover);
            this.richTextBox3.TextChanged += new EventHandler(this.richTextBox3_TextChanged);
            this.richTextBox3.Click += new EventHandler(this.richTextBox3_Click);
            this.richTextBox4.BackColor = Color.WhiteSmoke;
            this.richTextBox4.Location = new Point(0x196, 3);
            this.richTextBox4.Multiline = false;
            this.richTextBox4.Name = "richTextBox4";
            this.richTextBox4.Size = new Size(0x5f, 20);
            this.richTextBox4.TabIndex = 4;
            this.richTextBox4.Text = "";
            this.richTextBox4.WordWrap = false;
            this.richTextBox4.MouseHover += new EventHandler(this.richTextBox4_MouseHover);
            this.contextMenuStrip1.Items.AddRange(new ToolStripItem[] { this.addReferenceToModelPartToolStripMenuItem, this.removeReferenceToModelPartToolStripMenuItem1, this.insertModelPartToolStripMenuItem, this.removeModelPartToolStripMenuItem, this.exportTextToTSVToolStripMenuItem });
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new Size(270, 0x72);
            this.addReferenceToModelPartToolStripMenuItem.Name = "addReferenceToModelPartToolStripMenuItem";
            this.addReferenceToModelPartToolStripMenuItem.Size = new Size(0x10d, 0x16);
            this.addReferenceToModelPartToolStripMenuItem.Text = "Add Mesh and Texture Reference";
            this.addReferenceToModelPartToolStripMenuItem.Click += new EventHandler(this.addReference_Click);
            this.removeReferenceToModelPartToolStripMenuItem1.Name = "removeReferenceToModelPartToolStripMenuItem1";
            this.removeReferenceToModelPartToolStripMenuItem1.Size = new Size(0x10d, 0x16);
            this.removeReferenceToModelPartToolStripMenuItem1.Text = "Remove Mesh and Texture Reference";
            this.removeReferenceToModelPartToolStripMenuItem1.Click += new EventHandler(this.removeReference_Click);
            this.insertModelPartToolStripMenuItem.Name = "insertModelPartToolStripMenuItem";
            this.insertModelPartToolStripMenuItem.Size = new Size(0x10d, 0x16);
            this.insertModelPartToolStripMenuItem.Text = "Insert Model Part";
            this.insertModelPartToolStripMenuItem.Click += new EventHandler(this.insertModelPart_Click);
            this.removeModelPartToolStripMenuItem.Name = "removeModelPartToolStripMenuItem";
            this.removeModelPartToolStripMenuItem.Size = new Size(0x10d, 0x16);
            this.removeModelPartToolStripMenuItem.Text = "Remove Model Part";
            this.removeModelPartToolStripMenuItem.Click += new EventHandler(this.removeModelPart_Click);
            this.exportTextToTSVToolStripMenuItem.Name = "exportTextToTSVToolStripMenuItem";
            this.exportTextToTSVToolStripMenuItem.Size = new Size(0x10d, 0x16);
            this.exportTextToTSVToolStripMenuItem.Text = "Export Entries to TSV File";
            this.exportTextToTSVToolStripMenuItem.Click += new EventHandler(this.exportTSV_Click);
            this.richTextBox5.BackColor = Color.WhiteSmoke;
            this.richTextBox5.Location = new Point(0x1fb, 3);
            this.richTextBox5.Multiline = false;
            this.richTextBox5.Name = "richTextBox5";
            this.richTextBox5.Size = new Size(0x5f, 20);
            this.richTextBox5.TabIndex = 5;
            this.richTextBox5.Text = "";
            this.richTextBox5.WordWrap = false;
            this.richTextBox5.MouseHover += new EventHandler(this.richTextBox5_MouseHover);
            this.richTextBox6.BackColor = SystemColors.Control;
            this.richTextBox6.Location = new Point(0x6f, 3);
            this.richTextBox6.Multiline = false;
            this.richTextBox6.Name = "richTextBox6";
            this.richTextBox6.ReadOnly = true;
            this.richTextBox6.ScrollBars = RichTextBoxScrollBars.None;
            this.richTextBox6.Size = new Size(0x2f, 20);
            this.richTextBox6.TabIndex = 7;
            this.richTextBox6.Text = "";
            this.richTextBox6.WordWrap = false;
            this.richTextBox6.MouseHover += new EventHandler(this.richTextBox6_MouseHover);
            this.richTextBox6.TextChanged += new EventHandler(this.richTextBox6_TextChanged);
            this.richTextBox6.Click += new EventHandler(this.richTextBox6_Click);
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.richTextBox3);
            base.Controls.Add(this.richTextBox4);
            base.Controls.Add(this.richTextBox6);
            base.Controls.Add(this.richTextBox1);
            base.Controls.Add(this.treeListView1);
            base.Controls.Add(this.richTextBox5);
            base.Controls.Add(this.richTextBox2);
            base.Name = "UnitVariantFileEditorControl";
            base.Size = new Size(0x36c, 0x29d);
            ((ISupportInitialize) this.treeListView1).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            base.ResumeLayout(false);
        }

        private void insertModelPart_Click(object sender, EventArgs e)
        {
            UnitVariantObject entry = new UnitVariantObject();
            UnitVariantObject parent = new UnitVariantObject();
            if (this.treeListView1.SelectedObject is MeshTextureObject)
            {
                parent = (UnitVariantObject) this.treeListView1.GetParent(this.treeListView1.SelectedObject);
            }
            else
            {
                parent = (UnitVariantObject) this.treeListView1.SelectedObject;
            }
            entry.ModelPart = "NEW_ENTRY";
            entry.Num1 = parent.Num1 + 1;
            entry.Num4 = parent.Num4 + parent.Num3;
            this.unitVariantFile.insertUVO(entry, (int) entry.Num1);
            this.treeListView1.AddObject(entry);
            this.treeListView1.RefreshObjects(this.unitVariantFile.UnitVariantObjects);
            this.richTextBox6.Text = this.unitVariantFile.NumEntries.ToString();
            this.dataChanged = true;
            this.Refresh();
        }

        private void removeModelPart_Click(object sender, EventArgs e)
        {
            UnitVariantObject parent = new UnitVariantObject();
            if (this.treeListView1.SelectedObject is MeshTextureObject)
            {
                parent = (UnitVariantObject) this.treeListView1.GetParent(this.treeListView1.SelectedObject);
            }
            else
            {
                parent = (UnitVariantObject) this.treeListView1.SelectedObject;
            }
            this.unitVariantFile.removeUVO((int) parent.Num1);
            this.treeListView1.RemoveObject(this.treeListView1.SelectedObject);
            this.treeListView1.RefreshObjects(this.unitVariantFile.UnitVariantObjects);
            this.richTextBox6.Text = this.unitVariantFile.NumEntries.ToString();
            this.dataChanged = true;
            this.Refresh();
        }

        private void removeReference_Click(object sender, EventArgs e)
        {
            MeshTextureObject selectedObject = (MeshTextureObject) this.treeListView1.SelectedObject;
            UnitVariantObject parent = (UnitVariantObject) this.treeListView1.GetParent(this.treeListView1.SelectedObject);
            int index = this.treeListView1.IndexOf(this.treeListView1.SelectedObject) - this.treeListView1.IndexOf(this.treeListView1.GetParent(this.treeListView1.SelectedObject));
            this.unitVariantFile.removeMTO(parent, selectedObject, index);
            this.treeListView1.RemoveObject(this.treeListView1.SelectedObject);
            this.treeListView1.RefreshObjects(this.unitVariantFile.UnitVariantObjects);
            this.dataChanged = true;
            this.Refresh();
        }

        private void richTextBox2_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.richTextBox2, "Unknown two byte sequence [B1B2] expressed in hexadecimal. (Uneditable)");
        }

        public void richTextBox3_Click(object sender, EventArgs e)
        {
            this.richTextBox3.Text = this.unitVariantFile.Unknown2.ToString();
            this.richTextBox3.SelectAll();
        }

        private void richTextBox3_LostFocus(object sender, EventArgs e)
        {
            if (this.richTextBox3.Modified)
            {
                this.unitVariantFile.Unknown2 = Convert.ToUInt32(this.richTextBox3.Text);
                byte[] bytes = BitConverter.GetBytes(this.unitVariantFile.Unknown2);
                this.unitVariantFile.B1 = bytes[0];
                this.unitVariantFile.B2 = bytes[1];
                this.richTextBox4.Text = "B1 as Single: " + this.unitVariantFile.B1.ToString();
                this.richTextBox5.Text = "B2 as Single: " + this.unitVariantFile.B2.ToString();
                string str = this.unitVariantFile.B1.ToString("x2") + this.unitVariantFile.B2.ToString("x2");
                this.richTextBox2.Text = "B1B2: " + str;
                this.richTextBox3.Text = "B1B2 as UInt32: " + this.unitVariantFile.Unknown2.ToString();
                this.dataChanged = true;
            }
        }

        private void richTextBox3_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.richTextBox3, "Unknown two byte sequence [B1B2] expressed as an unsigned integer value. (Editable)");
        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {
            this.unitVariantFile.Unknown2 = Convert.ToUInt32(this.richTextBox3.Text);
            this.dataChanged = true;
        }

        private void richTextBox4_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.richTextBox4, "First byte [B1] in an unknown two byte sequence [B1B2] expressed as an unsigned integer value. (Uneditable)");
        }

        private void richTextBox5_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.richTextBox5, "Second byte [B2] in an unknown two byte sequence [B1B2] expressed as an unsigned integer value. (Uneditable)");
        }

        public void richTextBox6_Click(object sender, EventArgs e)
        {
            this.richTextBox6.Text = this.unitVariantFile.NumEntries.ToString();
            this.richTextBox6.SelectAll();
        }

        private void richTextBox6_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.richTextBox6, "Number of Model Parts. (Uneditable)");
        }

        private void richTextBox6_TextChanged(object sender, EventArgs e)
        {
            this.unitVariantFile.NumEntries = Convert.ToUInt32(this.richTextBox6.Text);
            this.dataChanged = true;
        }

        private void setColumnWidth()
        {
            float width = 0f;
            float num2 = 0f;
            using (Graphics graphics = base.CreateGraphics())
            {
                foreach (UnitVariantObject obj2 in this.unitVariantFile.UnitVariantObjects)
                {
                    if (obj2.MeshTextureList != null)
                    {
                        foreach (MeshTextureObject obj3 in obj2.MeshTextureList)
                        {
                            if (width < graphics.MeasureString(obj3.Mesh, this.Font).Width)
                            {
                                width = graphics.MeasureString(obj3.Mesh, this.Font).Width;
                            }
                            if (num2 < graphics.MeasureString(obj3.Texture, this.Font).Width)
                            {
                                num2 = graphics.MeasureString(obj3.Texture, this.Font).Width;
                            }
                        }
                    }
                }
                if (width > 0f)
                {
                    this.olvColumn6.Width = (int) width;
                }
                if (num2 > 0f)
                {
                    this.olvColumn7.Width = (int) num2;
                }
            }
        }

        public void setData()
        {
            this.currentPackedFile.ReplaceData(this.unitVariantFile.GetBytes());
            this.dataChanged = false;
        }

        private void treeListView1_CellEditFinishing(object sender, CellEditEventArgs e)
        {
            AspectPutterDelegate delegate2 = null;
            AspectPutterDelegate delegate3 = null;
            AspectPutterDelegate delegate4 = null;
            AspectPutterDelegate delegate5 = null;
            AspectPutterDelegate delegate6 = null;
            AspectPutterDelegate delegate7 = null;
            AspectPutterDelegate delegate8 = null;
            AspectPutterDelegate delegate9 = null;
            AspectPutterDelegate delegate10 = null;
            switch ((e.Column.Index + 1))
            {
                case 1:
                    if (delegate2 == null)
                    {
                        delegate2 = delegate (object x, object newValue) {
                            if (x is UnitVariantObject)
                            {
                                ((UnitVariantObject) x).ModelPart = (string) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn1.AspectPutter = delegate2;
                    break;

                case 2:
                    if (delegate3 == null)
                    {
                        delegate3 = delegate (object x, object newValue) {
                            if (x is UnitVariantObject)
                            {
                                ((UnitVariantObject) x).Num1 = (uint) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn2.AspectPutter = delegate3;
                    break;

                case 3:
                    if (delegate4 == null)
                    {
                        delegate4 = delegate (object x, object newValue) {
                            if (x is UnitVariantObject)
                            {
                                ((UnitVariantObject) x).Num2 = (uint) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn3.AspectPutter = delegate4;
                    break;

                case 4:
                    if (delegate5 == null)
                    {
                        delegate5 = delegate (object x, object newValue) {
                            if (x is UnitVariantObject)
                            {
                                ((UnitVariantObject) x).Num3 = (uint) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn4.AspectPutter = delegate5;
                    break;

                case 5:
                    if (delegate6 == null)
                    {
                        delegate6 = delegate (object x, object newValue) {
                            if (x is UnitVariantObject)
                            {
                                ((UnitVariantObject) x).Num4 = (uint) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn5.AspectPutter = delegate6;
                    break;

                case 6:
                    if (delegate7 == null)
                    {
                        delegate7 = delegate (object x, object newValue) {
                            if (x is MeshTextureObject)
                            {
                                ((MeshTextureObject) x).Mesh = (string) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn6.AspectPutter = delegate7;
                    break;

                case 7:
                    if (delegate8 == null)
                    {
                        delegate8 = delegate (object x, object newValue) {
                            if (x is MeshTextureObject)
                            {
                                ((MeshTextureObject) x).Texture = (string) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn7.AspectPutter = delegate8;
                    break;

                case 8:
                    if (delegate9 == null)
                    {
                        delegate9 = delegate (object x, object newValue) {
                            if (x is MeshTextureObject)
                            {
                                ((MeshTextureObject) x).Bool1 = (bool) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn8.AspectPutter = delegate9;
                    break;

                case 9:
                    if (delegate10 == null)
                    {
                        delegate10 = delegate (object x, object newValue) {
                            if (x is MeshTextureObject)
                            {
                                ((MeshTextureObject) x).Bool2 = (bool) newValue;
                            }
                            this.dataChanged = true;
                        };
                    }
                    this.olvColumn9.AspectPutter = delegate10;
                    break;
            }
            this.Refresh();
        }

        private void treeListView1_CellRightClick(object sender, CellRightClickEventArgs e)
        {
            if (e.Item != null)
            {
                e.Item.Selected = true;
                this.contextMenuStrip1.Show(this.treeListView1, e.Location.X, e.Location.Y);
                if (e.Model is MeshTextureObject)
                {
                    this.removeReferenceToModelPartToolStripMenuItem1.Enabled = true;
                }
                else if (e.Model is UnitVariantObject)
                {
                    this.removeReferenceToModelPartToolStripMenuItem1.Enabled = false;
                    this.removeModelPartToolStripMenuItem.Enabled = true;
                    this.insertModelPartToolStripMenuItem.Enabled = true;
                }
            }
        }

        public void updatePackedFile()
        {
            if (this.dataChanged)
            {
                this.setData();
            }
        }
    }
}

