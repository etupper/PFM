namespace Common
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    [Serializable, XmlSchemaProvider("GetTypedDataSetSchema"), GeneratedCode("System.Data.Design.TypedDataSetGenerator", "2.0.0.0"), XmlRoot("SpyEventLogDataSet"), HelpKeyword("vs.data.DataSet"), DesignerCategory("code"), ToolboxItem(true)]
    public class SpyEventLogDataSet : DataSet
    {
        private System.Data.SchemaSerializationMode _schemaSerializationMode;
        private SpyEventLogDataTableDataTable tableSpyEventLogDataTable;

        [DebuggerNonUserCode]
        public SpyEventLogDataSet()
        {
            this._schemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            base.BeginInit();
            this.InitClass();
            CollectionChangeEventHandler handler = new CollectionChangeEventHandler(this.SchemaChanged);
            base.Tables.CollectionChanged += handler;
            base.Relations.CollectionChanged += handler;
            base.EndInit();
        }

        [DebuggerNonUserCode]
        protected SpyEventLogDataSet(SerializationInfo info, StreamingContext context) : base(info, context, false)
        {
            this._schemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            if (base.IsBinarySerialized(info, context))
            {
                this.InitVars(false);
                CollectionChangeEventHandler handler = new CollectionChangeEventHandler(this.SchemaChanged);
                this.Tables.CollectionChanged += handler;
                this.Relations.CollectionChanged += handler;
            }
            else
            {
                string s = (string) info.GetValue("XmlSchema", typeof(string));
                if (base.DetermineSchemaSerializationMode(info, context) == System.Data.SchemaSerializationMode.IncludeSchema)
                {
                    DataSet dataSet = new DataSet();
                    dataSet.ReadXmlSchema(new XmlTextReader(new StringReader(s)));
                    if (dataSet.Tables["SpyEventLogDataTable"] != null)
                    {
                        base.Tables.Add(new SpyEventLogDataTableDataTable(dataSet.Tables["SpyEventLogDataTable"]));
                    }
                    base.DataSetName = dataSet.DataSetName;
                    base.Prefix = dataSet.Prefix;
                    base.Namespace = dataSet.Namespace;
                    base.Locale = dataSet.Locale;
                    base.CaseSensitive = dataSet.CaseSensitive;
                    base.EnforceConstraints = dataSet.EnforceConstraints;
                    base.Merge(dataSet, false, MissingSchemaAction.Add);
                    this.InitVars();
                }
                else
                {
                    base.ReadXmlSchema(new XmlTextReader(new StringReader(s)));
                }
                base.GetSerializationData(info, context);
                CollectionChangeEventHandler handler2 = new CollectionChangeEventHandler(this.SchemaChanged);
                base.Tables.CollectionChanged += handler2;
                this.Relations.CollectionChanged += handler2;
            }
        }

        [DebuggerNonUserCode]
        public override DataSet Clone()
        {
            SpyEventLogDataSet set = (SpyEventLogDataSet) base.Clone();
            set.InitVars();
            set.SchemaSerializationMode = this.SchemaSerializationMode;
            return set;
        }

        [DebuggerNonUserCode]
        protected override XmlSchema GetSchemaSerializable()
        {
            MemoryStream w = new MemoryStream();
            base.WriteXmlSchema(new XmlTextWriter(w, null));
            w.Position = 0L;
            return XmlSchema.Read(new XmlTextReader(w), null);
        }

        [DebuggerNonUserCode]
        public static XmlSchemaComplexType GetTypedDataSetSchema(XmlSchemaSet xs)
        {
            SpyEventLogDataSet set = new SpyEventLogDataSet();
            XmlSchemaComplexType type = new XmlSchemaComplexType();
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            XmlSchemaAny any2 = new XmlSchemaAny {
                Namespace = set.Namespace
            };
            XmlSchemaAny item = any2;
            sequence.Items.Add(item);
            type.Particle = sequence;
            XmlSchema schemaSerializable = set.GetSchemaSerializable();
            if (xs.Contains(schemaSerializable.TargetNamespace))
            {
                MemoryStream stream = new MemoryStream();
                MemoryStream stream2 = new MemoryStream();
                try
                {
                    XmlSchema current = null;
                    schemaSerializable.Write(stream);
                    IEnumerator enumerator = xs.Schemas(schemaSerializable.TargetNamespace).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        current = (XmlSchema) enumerator.Current;
                        stream2.SetLength(0L);
                        current.Write(stream2);
                        if (stream.Length == stream2.Length)
                        {
                            stream.Position = 0L;
                            stream2.Position = 0L;
                            while ((stream.Position != stream.Length) && (stream.ReadByte() == stream2.ReadByte()))
                            {
                            }
                            if (stream.Position == stream.Length)
                            {
                                return type;
                            }
                        }
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    if (stream2 != null)
                    {
                        stream2.Close();
                    }
                }
            }
            xs.Add(schemaSerializable);
            return type;
        }

        [DebuggerNonUserCode]
        private void InitClass()
        {
            base.DataSetName = "SpyEventLogDataSet";
            base.Prefix = "";
            base.Namespace = "http://tempuri.org/SpyEventLogDataSet.xsd";
            base.EnforceConstraints = true;
            this.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            this.tableSpyEventLogDataTable = new SpyEventLogDataTableDataTable();
            base.Tables.Add(this.tableSpyEventLogDataTable);
        }

        [DebuggerNonUserCode]
        protected override void InitializeDerivedDataSet()
        {
            base.BeginInit();
            this.InitClass();
            base.EndInit();
        }

        [DebuggerNonUserCode]
        internal void InitVars()
        {
            this.InitVars(true);
        }

        [DebuggerNonUserCode]
        internal void InitVars(bool initTable)
        {
            this.tableSpyEventLogDataTable = (SpyEventLogDataTableDataTable) base.Tables["SpyEventLogDataTable"];
            if (initTable && (this.tableSpyEventLogDataTable != null))
            {
                this.tableSpyEventLogDataTable.InitVars();
            }
        }

        [DebuggerNonUserCode]
        protected override void ReadXmlSerializable(XmlReader reader)
        {
            if (base.DetermineSchemaSerializationMode(reader) == System.Data.SchemaSerializationMode.IncludeSchema)
            {
                this.Reset();
                DataSet dataSet = new DataSet();
                dataSet.ReadXml(reader);
                if (dataSet.Tables["SpyEventLogDataTable"] != null)
                {
                    base.Tables.Add(new SpyEventLogDataTableDataTable(dataSet.Tables["SpyEventLogDataTable"]));
                }
                base.DataSetName = dataSet.DataSetName;
                base.Prefix = dataSet.Prefix;
                base.Namespace = dataSet.Namespace;
                base.Locale = dataSet.Locale;
                base.CaseSensitive = dataSet.CaseSensitive;
                base.EnforceConstraints = dataSet.EnforceConstraints;
                base.Merge(dataSet, false, MissingSchemaAction.Add);
                this.InitVars();
            }
            else
            {
                base.ReadXml(reader);
                this.InitVars();
            }
        }

        [DebuggerNonUserCode]
        private void SchemaChanged(object sender, CollectionChangeEventArgs e)
        {
            if (e.Action == CollectionChangeAction.Remove)
            {
                this.InitVars();
            }
        }

        [DebuggerNonUserCode]
        protected override bool ShouldSerializeRelations()
        {
            return false;
        }

        [DebuggerNonUserCode]
        private bool ShouldSerializeSpyEventLogDataTable()
        {
            return false;
        }

        [DebuggerNonUserCode]
        protected override bool ShouldSerializeTables()
        {
            return false;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), DebuggerNonUserCode]
        public DataRelationCollection Relations
        {
            get
            {
                return base.Relations;
            }
        }

        [DebuggerNonUserCode, Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override System.Data.SchemaSerializationMode SchemaSerializationMode
        {
            get
            {
                return this._schemaSerializationMode;
            }
            set
            {
                this._schemaSerializationMode = value;
            }
        }

        [DebuggerNonUserCode, DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Browsable(false)]
        public SpyEventLogDataTableDataTable SpyEventLogDataTable
        {
            get
            {
                return this.tableSpyEventLogDataTable;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), DebuggerNonUserCode]
        public DataTableCollection Tables
        {
            get
            {
                return base.Tables;
            }
        }

        [Serializable, XmlSchemaProvider("GetTypedTableSchema"), GeneratedCode("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
        public class SpyEventLogDataTableDataTable : DataTable, IEnumerable
        {
            private DataColumn columnArgs;
            private DataColumn columnName;
            private DataColumn columnSource;

            public event SpyEventLogDataSet.SpyEventLogDataTableRowChangeEventHandler SpyEventLogDataTableRowChanged;

            public event SpyEventLogDataSet.SpyEventLogDataTableRowChangeEventHandler SpyEventLogDataTableRowChanging;

            public event SpyEventLogDataSet.SpyEventLogDataTableRowChangeEventHandler SpyEventLogDataTableRowDeleted;

            public event SpyEventLogDataSet.SpyEventLogDataTableRowChangeEventHandler SpyEventLogDataTableRowDeleting;

            [DebuggerNonUserCode]
            public SpyEventLogDataTableDataTable()
            {
                base.TableName = "SpyEventLogDataTable";
                this.BeginInit();
                this.InitClass();
                this.EndInit();
            }

            [DebuggerNonUserCode]
            internal SpyEventLogDataTableDataTable(DataTable table)
            {
                base.TableName = table.TableName;
                if (table.CaseSensitive != table.DataSet.CaseSensitive)
                {
                    base.CaseSensitive = table.CaseSensitive;
                }
                if (table.Locale.ToString() != table.DataSet.Locale.ToString())
                {
                    base.Locale = table.Locale;
                }
                if (table.Namespace != table.DataSet.Namespace)
                {
                    base.Namespace = table.Namespace;
                }
                base.Prefix = table.Prefix;
                base.MinimumCapacity = table.MinimumCapacity;
            }

            [DebuggerNonUserCode]
            protected SpyEventLogDataTableDataTable(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                this.InitVars();
            }

            [DebuggerNonUserCode]
            public void AddSpyEventLogDataTableRow(SpyEventLogDataSet.SpyEventLogDataTableRow row)
            {
                base.Rows.Add(row);
            }

            [DebuggerNonUserCode]
            public SpyEventLogDataSet.SpyEventLogDataTableRow AddSpyEventLogDataTableRow(string Source, string Name, string Args)
            {
                SpyEventLogDataSet.SpyEventLogDataTableRow row = (SpyEventLogDataSet.SpyEventLogDataTableRow) base.NewRow();
                row.ItemArray = new object[] { Source, Name, Args };
                base.Rows.Add(row);
                return row;
            }

            [DebuggerNonUserCode]
            public override DataTable Clone()
            {
                SpyEventLogDataSet.SpyEventLogDataTableDataTable table = (SpyEventLogDataSet.SpyEventLogDataTableDataTable) base.Clone();
                table.InitVars();
                return table;
            }

            [DebuggerNonUserCode]
            protected override DataTable CreateInstance()
            {
                return new SpyEventLogDataSet.SpyEventLogDataTableDataTable();
            }

            [DebuggerNonUserCode]
            public virtual IEnumerator GetEnumerator()
            {
                return base.Rows.GetEnumerator();
            }

            [DebuggerNonUserCode]
            protected override Type GetRowType()
            {
                return typeof(SpyEventLogDataSet.SpyEventLogDataTableRow);
            }

            [DebuggerNonUserCode]
            public static XmlSchemaComplexType GetTypedTableSchema(XmlSchemaSet xs)
            {
                XmlSchemaComplexType type = new XmlSchemaComplexType();
                XmlSchemaSequence sequence = new XmlSchemaSequence();
                SpyEventLogDataSet set = new SpyEventLogDataSet();
                XmlSchemaAny any3 = new XmlSchemaAny {
                    Namespace = "http://www.w3.org/2001/XMLSchema",
                    MinOccurs = 0M,
                    MaxOccurs = 79228162514264337593543950335M,
                    ProcessContents = XmlSchemaContentProcessing.Lax
                };
                XmlSchemaAny item = any3;
                sequence.Items.Add(item);
                XmlSchemaAny any4 = new XmlSchemaAny {
                    Namespace = "urn:schemas-microsoft-com:xml-diffgram-v1",
                    MinOccurs = 1M,
                    ProcessContents = XmlSchemaContentProcessing.Lax
                };
                XmlSchemaAny any2 = any4;
                sequence.Items.Add(any2);
                XmlSchemaAttribute attribute3 = new XmlSchemaAttribute {
                    Name = "namespace",
                    FixedValue = set.Namespace
                };
                XmlSchemaAttribute attribute = attribute3;
                type.Attributes.Add(attribute);
                XmlSchemaAttribute attribute4 = new XmlSchemaAttribute {
                    Name = "tableTypeName",
                    FixedValue = "SpyEventLogDataTableDataTable"
                };
                XmlSchemaAttribute attribute2 = attribute4;
                type.Attributes.Add(attribute2);
                type.Particle = sequence;
                XmlSchema schemaSerializable = set.GetSchemaSerializable();
                if (xs.Contains(schemaSerializable.TargetNamespace))
                {
                    MemoryStream stream = new MemoryStream();
                    MemoryStream stream2 = new MemoryStream();
                    try
                    {
                        XmlSchema current = null;
                        schemaSerializable.Write(stream);
                        IEnumerator enumerator = xs.Schemas(schemaSerializable.TargetNamespace).GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            current = (XmlSchema) enumerator.Current;
                            stream2.SetLength(0L);
                            current.Write(stream2);
                            if (stream.Length == stream2.Length)
                            {
                                stream.Position = 0L;
                                stream2.Position = 0L;
                                while ((stream.Position != stream.Length) && (stream.ReadByte() == stream2.ReadByte()))
                                {
                                }
                                if (stream.Position == stream.Length)
                                {
                                    return type;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Close();
                        }
                        if (stream2 != null)
                        {
                            stream2.Close();
                        }
                    }
                }
                xs.Add(schemaSerializable);
                return type;
            }

            [DebuggerNonUserCode]
            private void InitClass()
            {
                this.columnSource = new DataColumn("Source", typeof(string), null, MappingType.Element);
                base.Columns.Add(this.columnSource);
                this.columnName = new DataColumn("Name", typeof(string), null, MappingType.Element);
                base.Columns.Add(this.columnName);
                this.columnArgs = new DataColumn("Args", typeof(string), null, MappingType.Element);
                base.Columns.Add(this.columnArgs);
            }

            [DebuggerNonUserCode]
            internal void InitVars()
            {
                this.columnSource = base.Columns["Source"];
                this.columnName = base.Columns["Name"];
                this.columnArgs = base.Columns["Args"];
            }

            [DebuggerNonUserCode]
            protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
            {
                return new SpyEventLogDataSet.SpyEventLogDataTableRow(builder);
            }

            [DebuggerNonUserCode]
            public SpyEventLogDataSet.SpyEventLogDataTableRow NewSpyEventLogDataTableRow()
            {
                return (SpyEventLogDataSet.SpyEventLogDataTableRow) base.NewRow();
            }

            [DebuggerNonUserCode]
            protected override void OnRowChanged(DataRowChangeEventArgs e)
            {
                base.OnRowChanged(e);
                if (this.SpyEventLogDataTableRowChanged != null)
                {
                    this.SpyEventLogDataTableRowChanged(this, new SpyEventLogDataSet.SpyEventLogDataTableRowChangeEvent((SpyEventLogDataSet.SpyEventLogDataTableRow) e.Row, e.Action));
                }
            }

            [DebuggerNonUserCode]
            protected override void OnRowChanging(DataRowChangeEventArgs e)
            {
                base.OnRowChanging(e);
                if (this.SpyEventLogDataTableRowChanging != null)
                {
                    this.SpyEventLogDataTableRowChanging(this, new SpyEventLogDataSet.SpyEventLogDataTableRowChangeEvent((SpyEventLogDataSet.SpyEventLogDataTableRow) e.Row, e.Action));
                }
            }

            [DebuggerNonUserCode]
            protected override void OnRowDeleted(DataRowChangeEventArgs e)
            {
                base.OnRowDeleted(e);
                if (this.SpyEventLogDataTableRowDeleted != null)
                {
                    this.SpyEventLogDataTableRowDeleted(this, new SpyEventLogDataSet.SpyEventLogDataTableRowChangeEvent((SpyEventLogDataSet.SpyEventLogDataTableRow) e.Row, e.Action));
                }
            }

            [DebuggerNonUserCode]
            protected override void OnRowDeleting(DataRowChangeEventArgs e)
            {
                base.OnRowDeleting(e);
                if (this.SpyEventLogDataTableRowDeleting != null)
                {
                    this.SpyEventLogDataTableRowDeleting(this, new SpyEventLogDataSet.SpyEventLogDataTableRowChangeEvent((SpyEventLogDataSet.SpyEventLogDataTableRow) e.Row, e.Action));
                }
            }

            [DebuggerNonUserCode]
            public void RemoveSpyEventLogDataTableRow(SpyEventLogDataSet.SpyEventLogDataTableRow row)
            {
                base.Rows.Remove(row);
            }

            [DebuggerNonUserCode]
            public DataColumn ArgsColumn
            {
                get
                {
                    return this.columnArgs;
                }
            }

            [Browsable(false), DebuggerNonUserCode]
            public int Count
            {
                get
                {
                    return base.Rows.Count;
                }
            }

            [DebuggerNonUserCode]
            public SpyEventLogDataSet.SpyEventLogDataTableRow this[int index]
            {
                get
                {
                    return (SpyEventLogDataSet.SpyEventLogDataTableRow) base.Rows[index];
                }
            }

            [DebuggerNonUserCode]
            public DataColumn NameColumn
            {
                get
                {
                    return this.columnName;
                }
            }

            [DebuggerNonUserCode]
            public DataColumn SourceColumn
            {
                get
                {
                    return this.columnSource;
                }
            }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
        public class SpyEventLogDataTableRow : DataRow
        {
            private SpyEventLogDataSet.SpyEventLogDataTableDataTable tableSpyEventLogDataTable;

            [DebuggerNonUserCode]
            internal SpyEventLogDataTableRow(DataRowBuilder rb) : base(rb)
            {
                this.tableSpyEventLogDataTable = (SpyEventLogDataSet.SpyEventLogDataTableDataTable) base.Table;
            }

            [DebuggerNonUserCode]
            public bool IsArgsNull()
            {
                return base.IsNull(this.tableSpyEventLogDataTable.ArgsColumn);
            }

            [DebuggerNonUserCode]
            public bool IsNameNull()
            {
                return base.IsNull(this.tableSpyEventLogDataTable.NameColumn);
            }

            [DebuggerNonUserCode]
            public bool IsSourceNull()
            {
                return base.IsNull(this.tableSpyEventLogDataTable.SourceColumn);
            }

            [DebuggerNonUserCode]
            public void SetArgsNull()
            {
                base[this.tableSpyEventLogDataTable.ArgsColumn] = Convert.DBNull;
            }

            [DebuggerNonUserCode]
            public void SetNameNull()
            {
                base[this.tableSpyEventLogDataTable.NameColumn] = Convert.DBNull;
            }

            [DebuggerNonUserCode]
            public void SetSourceNull()
            {
                base[this.tableSpyEventLogDataTable.SourceColumn] = Convert.DBNull;
            }

            [DebuggerNonUserCode]
            public string Args
            {
                get
                {
                    string str;
                    try
                    {
                        str = (string) base[this.tableSpyEventLogDataTable.ArgsColumn];
                    }
                    catch (InvalidCastException exception)
                    {
                        throw new StrongTypingException("The value for column 'Args' in table 'SpyEventLogDataTable' is DBNull.", exception);
                    }
                    return str;
                }
                set
                {
                    base[this.tableSpyEventLogDataTable.ArgsColumn] = value;
                }
            }

            [DebuggerNonUserCode]
            public string Name
            {
                get
                {
                    string str;
                    try
                    {
                        str = (string) base[this.tableSpyEventLogDataTable.NameColumn];
                    }
                    catch (InvalidCastException exception)
                    {
                        throw new StrongTypingException("The value for column 'Name' in table 'SpyEventLogDataTable' is DBNull.", exception);
                    }
                    return str;
                }
                set
                {
                    base[this.tableSpyEventLogDataTable.NameColumn] = value;
                }
            }

            [DebuggerNonUserCode]
            public string Source
            {
                get
                {
                    string str;
                    try
                    {
                        str = (string) base[this.tableSpyEventLogDataTable.SourceColumn];
                    }
                    catch (InvalidCastException exception)
                    {
                        throw new StrongTypingException("The value for column 'Source' in table 'SpyEventLogDataTable' is DBNull.", exception);
                    }
                    return str;
                }
                set
                {
                    base[this.tableSpyEventLogDataTable.SourceColumn] = value;
                }
            }
        }

        [GeneratedCode("System.Data.Design.TypedDataSetGenerator", "2.0.0.0")]
        public class SpyEventLogDataTableRowChangeEvent : EventArgs
        {
            private DataRowAction eventAction;
            private SpyEventLogDataSet.SpyEventLogDataTableRow eventRow;

            [DebuggerNonUserCode]
            public SpyEventLogDataTableRowChangeEvent(SpyEventLogDataSet.SpyEventLogDataTableRow row, DataRowAction action)
            {
                this.eventRow = row;
                this.eventAction = action;
            }

            [DebuggerNonUserCode]
            public DataRowAction Action
            {
                get
                {
                    return this.eventAction;
                }
            }

            [DebuggerNonUserCode]
            public SpyEventLogDataSet.SpyEventLogDataTableRow Row
            {
                get
                {
                    return this.eventRow;
                }
            }
        }

        public delegate void SpyEventLogDataTableRowChangeEventHandler(object sender, SpyEventLogDataSet.SpyEventLogDataTableRowChangeEvent e);
    }
}

