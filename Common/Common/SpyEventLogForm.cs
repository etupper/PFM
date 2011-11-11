namespace Common
{
    using SpyTools;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Reflection;
    using System.Windows.Forms;

    public class SpyEventLogForm : Form
    {
        private DataGridViewTextBoxColumn argsDataGridViewTextBoxColumn;
        private IContainer components = null;
        private ColumnHeader eventColumn;
        private ToolStripComboBox eventSpyComboBox;
        private Dictionary<string, EventSpyInfo> eventSpyList;
        private DataGridViewTextBoxColumn nameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn sourceDataGridViewTextBoxColumn;
        private ListView spyEventListView;
        private DataGridView spyEventLogDataGridView;
        private SpyEventLogDataSet spyEventLogDataSet;
        private ToolStrip toolStrip1;

        public SpyEventLogForm()
        {
            this.InitializeComponent();
            this.eventSpyList = new Dictionary<string, EventSpyInfo>();
            this.AddEventSpy(new EventSpy("Log", this));
        }

        public void AddEventSpy(EventSpy eventSpy)
        {
            EventSpyInfo info = new EventSpyInfo(eventSpy);
            this.eventSpyList.Add(eventSpy.SpyName, info);
            this.eventSpyComboBox.Items.Add(eventSpy.SpyName);
            eventSpy.SpyEvent += new SpyEventHandler(this.LogSpyEvent);
            if (this.eventSpyComboBox.SelectedIndex < 0)
            {
                this.eventSpyComboBox.SelectedIndex = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void eventSpyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SortedDictionary<string, SpyEvent> spyEvents = this.eventSpyList[this.eventSpyComboBox.SelectedItem as string].SpyEvents;
            this.spyEventListView.VirtualListSize = spyEvents.Count;
            this.spyEventListView.Refresh();
        }

        private void InitializeComponent()
        {
            this.spyEventLogDataGridView = new DataGridView();
            this.sourceDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            this.nameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            this.argsDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            this.spyEventLogDataSet = new SpyEventLogDataSet();
            this.toolStrip1 = new ToolStrip();
            this.eventSpyComboBox = new ToolStripComboBox();
            this.spyEventListView = new ListView();
            this.eventColumn = new ColumnHeader();
            ((ISupportInitialize) this.spyEventLogDataGridView).BeginInit();
            this.spyEventLogDataSet.BeginInit();
            this.toolStrip1.SuspendLayout();
            base.SuspendLayout();
            this.spyEventLogDataGridView.AllowUserToAddRows = false;
            this.spyEventLogDataGridView.AllowUserToDeleteRows = false;
            this.spyEventLogDataGridView.AllowUserToResizeRows = false;
            this.spyEventLogDataGridView.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.spyEventLogDataGridView.AutoGenerateColumns = false;
            this.spyEventLogDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.spyEventLogDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.spyEventLogDataGridView.Columns.AddRange(new DataGridViewColumn[] { this.sourceDataGridViewTextBoxColumn, this.nameDataGridViewTextBoxColumn, this.argsDataGridViewTextBoxColumn });
            this.spyEventLogDataGridView.DataMember = "SpyEventLogDataTable";
            this.spyEventLogDataGridView.DataSource = this.spyEventLogDataSet;
            this.spyEventLogDataGridView.Location = new Point(0, 0x1c);
            this.spyEventLogDataGridView.Name = "spyEventLogDataGridView";
            this.spyEventLogDataGridView.ReadOnly = true;
            this.spyEventLogDataGridView.RowHeadersVisible = false;
            this.spyEventLogDataGridView.Size = new Size(0x22b, 0x111);
            this.spyEventLogDataGridView.TabIndex = 0;
            this.sourceDataGridViewTextBoxColumn.DataPropertyName = "Source";
            this.sourceDataGridViewTextBoxColumn.HeaderText = "Source";
            this.sourceDataGridViewTextBoxColumn.Name = "sourceDataGridViewTextBoxColumn";
            this.sourceDataGridViewTextBoxColumn.ReadOnly = true;
            this.nameDataGridViewTextBoxColumn.DataPropertyName = "Name";
            this.nameDataGridViewTextBoxColumn.HeaderText = "Name";
            this.nameDataGridViewTextBoxColumn.Name = "nameDataGridViewTextBoxColumn";
            this.nameDataGridViewTextBoxColumn.ReadOnly = true;
            this.argsDataGridViewTextBoxColumn.DataPropertyName = "Args";
            this.argsDataGridViewTextBoxColumn.HeaderText = "Args";
            this.argsDataGridViewTextBoxColumn.Name = "argsDataGridViewTextBoxColumn";
            this.argsDataGridViewTextBoxColumn.ReadOnly = true;
            this.spyEventLogDataSet.DataSetName = "SpyEventLogDataSet";
            this.spyEventLogDataSet.SchemaSerializationMode = SchemaSerializationMode.IncludeSchema;
            this.toolStrip1.Items.AddRange(new ToolStripItem[] { this.eventSpyComboBox });
            this.toolStrip1.Location = new Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new Size(0x325, 0x19);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            this.eventSpyComboBox.Alignment = ToolStripItemAlignment.Right;
            this.eventSpyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.eventSpyComboBox.FlatStyle = FlatStyle.System;
            this.eventSpyComboBox.Name = "eventSpyComboBox";
            this.eventSpyComboBox.Size = new Size(0xf3, 0x19);
            this.eventSpyComboBox.Sorted = true;
            this.eventSpyComboBox.SelectedIndexChanged += new EventHandler(this.eventSpyComboBox_SelectedIndexChanged);
            this.spyEventListView.Anchor = AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            this.spyEventListView.CheckBoxes = true;
            this.spyEventListView.Columns.AddRange(new ColumnHeader[] { this.eventColumn });
            this.spyEventListView.HeaderStyle = ColumnHeaderStyle.None;
            this.spyEventListView.Location = new Point(0x231, 0x1c);
            this.spyEventListView.Name = "spyEventListView";
            this.spyEventListView.OwnerDraw = true;
            this.spyEventListView.Size = new Size(0xf3, 0x110);
            this.spyEventListView.Sorting = SortOrder.Ascending;
            this.spyEventListView.TabIndex = 2;
            this.spyEventListView.UseCompatibleStateImageBehavior = false;
            this.spyEventListView.View = View.Details;
            this.spyEventListView.VirtualMode = true;
            this.spyEventListView.MouseDoubleClick += new MouseEventHandler(this.spyEventListView_MouseDoubleClick);
            this.spyEventListView.MouseClick += new MouseEventHandler(this.spyEventListView_MouseClick);
            this.spyEventListView.DrawItem += new DrawListViewItemEventHandler(this.spyEventListView_DrawItem);
            this.spyEventListView.RetrieveVirtualItem += new RetrieveVirtualItemEventHandler(this.spyEventListView_RetrieveVirtualItem);
            this.eventColumn.Text = "Event";
            this.eventColumn.Width = 0x5dc;
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.ClientSize = new Size(0x325, 0x12d);
            base.Controls.Add(this.spyEventListView);
            base.Controls.Add(this.toolStrip1);
            base.Controls.Add(this.spyEventLogDataGridView);
            base.Name = "SpyEventLogForm";
            base.ShowIcon = false;
            this.Text = "SpyEventLog";
            ((ISupportInitialize) this.spyEventLogDataGridView).EndInit();
            this.spyEventLogDataSet.EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        public void LogSpyEvent(object sender, SpyEventArgs e)
        {
            EventSpyInfo info = this.eventSpyList[e.EventSpy.SpyName];
            if (info.SpyEvents[e.EventName].isMonitored)
            {
                SpyEventLogDataSet.SpyEventLogDataTableRow row = this.spyEventLogDataSet.SpyEventLogDataTable.NewSpyEventLogDataTableRow();
                row.Source = sender.ToString();
                row.Name = e.EventName;
                row.Args = e.EventArgs.ToString();
                this.spyEventLogDataSet.SpyEventLogDataTable.Rows.Add(row);
                this.spyEventLogDataSet.SpyEventLogDataTable.AcceptChanges();
                if (this.spyEventLogDataGridView.RowCount > 0)
                {
                    this.spyEventLogDataGridView.FirstDisplayedScrollingRowIndex = this.spyEventLogDataGridView.RowCount - 1;
                }
            }
        }

        private void spyEventListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
            if (!e.Item.Checked)
            {
                e.Item.Checked = true;
                e.Item.Checked = false;
            }
        }

        private void spyEventListView_MouseClick(object sender, MouseEventArgs e)
        {
            ListView view = (ListView) sender;
            ListViewItem itemAt = view.GetItemAt(e.X, e.Y);
            if ((itemAt != null) && (e.X < (itemAt.Bounds.Left + 0x10)))
            {
                SortedDictionary<string, SpyEvent> spyEvents = this.eventSpyList[this.eventSpyComboBox.SelectedItem as string].SpyEvents;
                foreach (int num in view.SelectedIndices)
                {
                    SpyEvent event2 = Enumerate.NthElement<SpyEvent>(num, spyEvents.Values);
                    event2.isMonitored = !event2.isMonitored;
                    view.RedrawItems(num, num, true);
                }
            }
        }

        private void spyEventListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView view = (ListView) sender;
            ListViewItem itemAt = view.GetItemAt(e.X, e.Y);
            if (itemAt != null)
            {
                view.Invalidate(itemAt.Bounds);
            }
        }

        private void spyEventListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            SortedDictionary<string, SpyEvent> spyEvents = this.eventSpyList[this.eventSpyComboBox.SelectedItem as string].SpyEvents;
            SpyEvent event2 = Enumerate.NthElement<SpyEvent>(e.ItemIndex, spyEvents.Values);
            ListViewItem item = new ListViewItem {
                Name = event2.name,
                Text = event2.name,
                Checked = event2.isMonitored
            };
            e.Item = item;
        }

        public Dictionary<string, EventSpyInfo> EventSpyList
        {
            get
            {
                return this.eventSpyList;
            }
        }

        public class EventSpyInfo
        {
            private SpyTools.EventSpy eventSpy;
            private SortedDictionary<string, SpyEventLogForm.SpyEvent> spyEvents;

            public EventSpyInfo(SpyTools.EventSpy eventSpy)
            {
                this.eventSpy = eventSpy;
                this.spyEvents = new SortedDictionary<string, SpyEventLogForm.SpyEvent>();
                foreach (EventInfo info in eventSpy.SpyTarget.GetType().GetEvents())
                {
                    this.spyEvents.Add(info.Name, new SpyEventLogForm.SpyEvent(info));
                }
            }

            public SpyTools.EventSpy EventSpy
            {
                get
                {
                    return this.eventSpy;
                }
            }

            public SortedDictionary<string, SpyEventLogForm.SpyEvent> SpyEvents
            {
                get
                {
                    return this.spyEvents;
                }
            }
        }

        public class SpyEvent
        {
            public bool isMonitored;
            public string name;
            public ParameterInfo[] parameters;

            public SpyEvent(EventInfo eventInfo)
            {
                this.name = eventInfo.Name;
                this.parameters = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters();
                this.isMonitored = false;
            }
        }
    }
}

