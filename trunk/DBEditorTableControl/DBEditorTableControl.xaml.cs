using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Common;
using CommonDialogs;
using CommonUtilities;
using Filetypes;

namespace DBTableControl
{
    /// <summary>
    /// Interaction logic for DBEditorTableControl.xaml
    /// </summary>
    public partial class DBEditorTableControl : UserControl, INotifyPropertyChanged, IPackedFileEditor
    {
        public static DBEditorTableControl RegisterDbEditor() {
            DBEditorTableControl control = new DBEditorTableControl();
            DBEditorTableHost host = new DBEditorTableHost {
                Child = control,
                Dock = System.Windows.Forms.DockStyle.Fill
            };
            PackedFileEditorRegistry.Editors.Add(host);
            return control;
        }
        
        DataSet loadedDataSet;

        // Data Source Properties
        DataTable currentTable;
        public DataTable CurrentTable
        {
            get { return currentTable; }
            set
            {
                dbDataGrid.ItemsSource = null;

                currentTable = value;

                // Reset event handlers
                currentTable.ColumnChanged -= new DataColumnChangeEventHandler(CurrentTable_ColumnChanged);
                CurrentTable.ColumnChanged += new DataColumnChangeEventHandler(CurrentTable_ColumnChanged);
                CurrentTable.RowDeleting -= new DataRowChangeEventHandler(CurrentTable_RowDeleting);
                CurrentTable.RowDeleting += new DataRowChangeEventHandler(CurrentTable_RowDeleting);
                CurrentTable.RowDeleted -= new DataRowChangeEventHandler(CurrentTable_RowDeleted);
                CurrentTable.RowDeleted += new DataRowChangeEventHandler(CurrentTable_RowDeleted);
                CurrentTable.TableNewRow -= new DataTableNewRowEventHandler(CurrentTable_TableNewRow);
                CurrentTable.TableNewRow += new DataTableNewRowEventHandler(CurrentTable_TableNewRow);

                // DBEditor code path.
                if (CurrentTable.ExtendedProperties.ContainsKey("PackedFile"))
                {
                    // Save off the current configuration.
                    UpdateConfig();

                    // Set currentPackedFile
                    currentPackedFile = CurrentTable.ExtendedProperties["PackedFile"] as PackedFile;

                    if (currentPackedFile != null)
                    {
                        try
                        {
                            codec = PackedFileDbCodec.FromFilename(currentPackedFile.FullPath);
                            editedFile = PackedFileDbCodec.Decode(currentPackedFile);
                        }
                        catch (DBFileNotSupportedException exception)
                        {
                            showDBFileNotSupportedMessage(exception.Message);
                        }
                    }
                }

                // Generate a new table format for the new table.
                GenerateColumns();

                // Assign frozen columns
                if (FreezeKeyColumns)
                {
                    NumKeyColumns = value.PrimaryKey.Length;
                }
                else
                {
                    NumKeyColumns = 0;
                }

                // Re-enable export control if it was disabled
                exportAsButton.IsEnabled = true;

                // Make sure the control knows it's table has changed.
                NotifyPropertyChanged(this, "CurrentTable");

                dbDataGrid.ItemsSource = CurrentTable.DefaultView;
            }
        }

        int numKeyColumns;
        public int NumKeyColumns
        {
            get { return numKeyColumns; }
            set { numKeyColumns = value; NotifyPropertyChanged(this, "NumKeyColumns"); }
        }

        bool freezeKeyColumns;
        public bool FreezeKeyColumns
        {
            get { return freezeKeyColumns; }
            set 
            { 
                freezeKeyColumns = value; 
                NotifyPropertyChanged(this, "FreezeKeyColumns");
                UpdateConfig();

                // Modify associated attribute.
                if (freezeKeyColumns)
                {
                    NumKeyColumns = CurrentTable.PrimaryKey.Length;
                }
                else
                {
                    NumKeyColumns = 0;
                }
            }
        }

        bool useComboBoxes;
        public bool UseComboBoxes
        {
            get { return useComboBoxes; }
            set 
            { 
                useComboBoxes = value; 
                NotifyPropertyChanged(this, "UseComboBoxes");
                UpdateConfig();

                if (EditedFile != null)
                {
                    // Generate new columns
                    GenerateColumns(false);
                }
            }
        }

        bool showAllColumns;
        public bool ShowAllColumns
        {
            get { return showAllColumns; }
            set
            {
                showAllColumns = value; 
                NotifyPropertyChanged(this, "ShowAllColumns");
                UpdateConfig();

                // Set all columns to visible, but do not reset currentTable's extended properties.
                foreach (DataGridColumn col in dbDataGrid.Columns)
                {
                    if (currentTable.Columns[(string)col.Header].ExtendedProperties.ContainsKey("Hidden"))
                    {
                        bool ishidden = (bool)currentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"];

                        if (ishidden && !showAllColumns)
                        {
                            col.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            col.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        bool readOnly;
        public bool ReadOnly
        {
            get { return readOnly; }
            set 
            { 
                readOnly = value; 
                NotifyPropertyChanged(this, "TableReadOnly");

                // Set whether we can add rows via button based on readonly.
                addRowButton.IsEnabled = !readOnly;
                importFromButton.IsEnabled = !readOnly;
                dbDataGrid.CanUserAddRows = !readOnly;
                dbDataGrid.CanUserDeleteRows = !readOnly;

                BuiltTablesSetReadOnly(readOnly);
        }
        }

        // PFM needed Properties
        PackedFile currentPackedFile;
        public PackedFile CurrentPackedFile
        {
            get { return currentPackedFile; }
            set
            {
                if (currentPackedFile != null && DataChanged)
                {
                    Commit();
                }

                if (editedFile != null)
                {
                    // Save off the editor configuration.
                    UpdateConfig();
                }
                
                dataChanged = false;
                currentPackedFile = value;

                if (currentPackedFile != null)
                {
                    try
                    {
                        codec = PackedFileDbCodec.FromFilename(currentPackedFile.FullPath);
                        editedFile = PackedFileDbCodec.Decode(currentPackedFile);
                    }
                    catch (DBFileNotSupportedException exception)
                    {
                        showDBFileNotSupportedMessage(exception.Message);
                    }
                }

                // Create and set CurrentTable
                CurrentTable = CreateTable(editedFile);

                NotifyPropertyChanged(this, "CurrentPackedFile");

                // Disabled until a better solution can be devised.
                //SetColumnSizes();
            }
        }

        PackedFileDbCodec codec;

        DBFile editedFile;
        public DBFile EditedFile { get { return editedFile; } }

        bool dataChanged;
        public bool DataChanged { get { return dataChanged; } }

        // Import Export default directory
        public string ModDirectory {
            get;
            set;
        }
        string importDirectory;
        public string ImportDirectory 
        { 
            get { return importDirectory; } 
            set 
            { 
                importDirectory = value; 
                UpdateConfig(); 
            } 
        }

        string exportDirectory;
        public string ExportDirectory
        {
            get { return exportDirectory; }
            set
            {
                exportDirectory = value;
                UpdateConfig();
            }
        }

        // Configuration data
        private DBTableEditorConfig savedconfig;

        private List<string> hiddenColumns;

        public DBEditorTableControl()
        {
            InitializeComponent();

            // Attempt to load configuration settings, loading default values if config file doesn't exist.
            savedconfig = new DBTableEditorConfig();
            savedconfig.Load();

            // Instantiate default datatable.
            currentTable = new DataTable();
            hiddenColumns = new List<string>();
            loadedDataSet = new DataSet("Loaded Tables");
            loadedDataSet.EnforceConstraints = false;

            // Transfer saved settings.
            freezeKeyColumns = savedconfig.FreezeKeyColumns;
            useComboBoxes = savedconfig.UseComboBoxes;
            showAllColumns = savedconfig.ShowAllColumns;
            importDirectory = savedconfig.ImportDirectory;
            exportDirectory = savedconfig.ExportDirectory;

            // Set Initial checked status.
            freezeKeysCheckBox.IsChecked = freezeKeyColumns;
            useComboBoxesCheckBox.IsChecked = useComboBoxes;
            showAllColumnsCheckBox.IsChecked = showAllColumns;

            // Register for Datatable events
            CurrentTable.ColumnChanged += new DataColumnChangeEventHandler(CurrentTable_ColumnChanged);
            CurrentTable.RowDeleting += new DataRowChangeEventHandler(CurrentTable_RowDeleting);
            CurrentTable.RowDeleted += new DataRowChangeEventHandler(CurrentTable_RowDeleted);
            CurrentTable.TableNewRow += new DataTableNewRowEventHandler(CurrentTable_TableNewRow);
            
            // Default the clonerowButton to false for all tables.
            cloneRowButton.IsEnabled = false;

            // Route the Paste event here so we can do it ourselves.
            CommandManager.RegisterClassCommandBinding(typeof(DataGrid), 
                new CommandBinding(ApplicationCommands.Paste, 
                    new ExecutedRoutedEventHandler(OnExecutedPaste), 
                    new CanExecuteRoutedEventHandler(OnCanExecutePaste)));

            NumKeyColumns = 0;
        }

        #region IPackedFileEditor Implementation
        public bool CanEdit(PackedFile file)
        {
            bool result = file.FullPath.StartsWith("db");
            try
            {
                if (result)
                {
                    DBFileHeader header = PackedFileDbCodec.readHeader(file);
                    TypeInfo info = DBTypeMap.Instance.GetVersionedInfo(Path.GetFileName(Path.GetDirectoryName (file.FullPath)), header.Version);
                    if (info != null)
                    {
                        foreach (FieldInfo field in info.Fields)
                        {
                            result &= !(field is ListType);
                            if (!result)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public void Commit()
        {
            // Ignore Commit call if there is nothing to commit, or if the user simply wandered to another table.
            DataTable test = currentTable.GetChanges();
            if (EditedFile == null || (currentTable.GetChanges() == null && !dataChanged))
            {
                return;
            }

            if (CurrentTable.HasErrors)
            {
                MessageBox.Show("Warning, the current table has errors, it may crash your game!",
                                "Table Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                try
                {
                    // Since the data in CurrentTable aren't actually bound to the packed file we are editing
                    // we don't have to save any changes, this way if a user navigates away from the the current
                    // db file, all his changes don't look like they have been saved, aka all the red coloring stays.
                    //CurrentTable.AcceptChanges();
                }
                catch (Exception e)
                {
                    ErrorDialog.ShowDialog(e);
                }
            }

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    codec.Encode(stream, editedFile);
                    currentPackedFile.Data = stream.ToArray();
                }

                // Also save off the configuration.
                UpdateConfig();

                dataChanged = false;
            }
            catch (Exception e)
            {
                ErrorDialog.ShowDialog(e);
            }
        }

        public bool TryCommit()
        {
            if (CurrentTable.HasErrors)
            {
                return false;
            }

            try
            {
                CurrentTable.AcceptChanges();
            }
            catch (Exception e)
            {
                ErrorDialog.ShowDialog(e);
            }

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    codec.Encode(stream, editedFile);
                    currentPackedFile.Data = stream.ToArray();
                }

                // Also save off the configuration.
                UpdateConfig();

                dataChanged = false;

                return true;
            }
            catch(Exception e)
            {
                ErrorDialog.ShowDialog(e);
                return false;
            }
        }

        public void CommitAll()
        {
            // To be used when the pack file is saved, to AcceptChanges to all loaded DataTables.
        }
        #endregion

        /********************************************************************************************
         * This function uses the schema of currentTable to generate dbDataGrid's columns           *
         * programmatically.  This is necessary to not only create the combo box cells properly     *
         * but to set up more complex bindings than are allowed in the XAML.                        *
         ********************************************************************************************/
        void GenerateColumns(bool clearHidden = true)
        {
            dbDataGrid.Columns.Clear();

            if (clearHidden)
            {
                hiddenColumns.Clear();

                if (savedconfig.HiddenColumns.ContainsKey(editedFile.CurrentType.Name))
                {
                    hiddenColumns = new List<string>(savedconfig.HiddenColumns[editedFile.CurrentType.Name]);
                }
            }

            foreach (DataColumn column in CurrentTable.Columns)
            {
                bool isRelated = false;
                List<string> referencevalues = new List<string>();
                Visibility columnvisibility = System.Windows.Visibility.Visible;
                DataRelation columnRelation = null;

                // Set initial column visibility
                if (!column.ExtendedProperties.ContainsKey("Hidden"))
                {
                    column.ExtendedProperties.Add("Hidden", false);
                }

                if (hiddenColumns.Contains(column.ColumnName))
                {
                    if (!showAllColumns)
                    {
                        columnvisibility = System.Windows.Visibility.Hidden;
                    }

                    column.ExtendedProperties["Hidden"] = true;
                }

                // Determine relations as assigned by DBE
                foreach (DataRelation relation in CurrentTable.ParentRelations)
                {
                    if (relation.ChildColumns.Contains(column))
                    {
                        columnRelation = relation;

                        referencevalues = columnRelation.ParentColumns.First().Table.Rows.OfType<DataRow>()
                                                                                         .Select(n => n.Field<string>(columnRelation.ParentColumns.First()))
                                                                                         .OrderBy(n => n)
                                                                                         .ToList();

                        if (referencevalues.Count() > 0 && ReferenceContainsAllValues(referencevalues, column))
                        {
                            isRelated = true;
                        }
                    }
                }

                // Determine relations as assigned by PFM
                if (column.ExtendedProperties.ContainsKey("FKey") && useComboBoxes)
                {
                    SortedSet<string> testset = new SortedSet<string>();
                    testset = DBReferenceMap.Instance.ResolveReference(column.ExtendedProperties["FKey"].ToString());
                    referencevalues = DBReferenceMap.Instance.ResolveReference(column.ExtendedProperties["FKey"].ToString()).ToList();

                    if (referencevalues.Count() > 0 && ReferenceContainsAllValues(referencevalues, column))
                    {
                        isRelated = true;
                    }
                }

                if (isRelated && !column.ReadOnly && UseComboBoxes)
                {
                    // Combobox Column
                    DataGridComboBoxColumn constructionColumn = new DataGridComboBoxColumn();
                    constructionColumn.Header = column.ColumnName;
                    constructionColumn.IsReadOnly = column.ReadOnly;

                    // Set the combo boxes items source to the already tested list.
                    constructionColumn.ItemsSource = referencevalues;

                    Binding constructionBinding = new Binding(String.Format("{0}", column.ColumnName));
                    constructionBinding.Mode = BindingMode.TwoWay;
                    constructionBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                    constructionColumn.SelectedItemBinding = constructionBinding;

                    // Setup the column context menu
                    // TODO: programatically create context menu so hidden item can be bound to current state.
                    Style tempstyle = new System.Windows.Style(typeof(DataGridColumnHeader), (Style)this.Resources["GridHeaderStyle"]);
                    //ConstructionColumn.HeaderStyle = (Style)this.Resources["GridHeaderStyle"];
                    constructionColumn.HeaderStyle = tempstyle;

                    // Set visibility
                    constructionColumn.Visibility = columnvisibility;

                    dbDataGrid.Columns.Add(constructionColumn);
                }
                else if (column.DataType.FullName == "System.Boolean")
                {
                    // Checkbox Column
                    DataGridCheckBoxColumn constructionColumn = new DataGridCheckBoxColumn();
                    constructionColumn.Header = column.ColumnName;
                    constructionColumn.IsReadOnly = column.ReadOnly;
                    Binding constructionBinding = new Binding(String.Format("{0}", column.ColumnName));
                    if (!column.ReadOnly)
                    {
                        constructionBinding.Mode = BindingMode.TwoWay;
                    }
                    else
                    {
                        constructionBinding.Mode = BindingMode.OneWay;
                    }

                    constructionBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                    constructionColumn.Binding = constructionBinding;

                    // Setup the column context menu
                    // TODO: programatically create context menu so hidden item can be bound to current state.
                    Style tempstyle = new System.Windows.Style(typeof(DataGridColumnHeader), (Style)this.Resources["GridHeaderStyle"]);
                    //ConstructionColumn.HeaderStyle = (Style)this.Resources["GridHeaderStyle"];
                    constructionColumn.HeaderStyle = tempstyle;

                    // Set visibility
                    constructionColumn.Visibility = columnvisibility;

                    dbDataGrid.Columns.Add(constructionColumn);
                }
                else
                {
                    // Textbox Column
                    DataGridTextColumn constructionColumn = new DataGridTextColumn();
                    constructionColumn.Header = column.ColumnName;
                    constructionColumn.IsReadOnly = column.ReadOnly;
                    Binding constructionBinding = new Binding(String.Format("{0}", column.ColumnName));
                    if (!column.ReadOnly)
                    {
                        constructionBinding.Mode = BindingMode.TwoWay;
                    }
                    else
                    {
                        constructionBinding.Mode = BindingMode.OneWay;
                    }

                    constructionColumn.Binding = constructionBinding;

                    // Setup the column context menu
                    // TODO: programatically create context menu so hidden item can be bound to current state.
                    Style tempstyle = new System.Windows.Style(typeof(DataGridColumnHeader), (Style)this.Resources["GridHeaderStyle"]);
                    //ConstructionColumn.HeaderStyle = (Style)this.Resources["GridHeaderStyle"];
                    constructionColumn.HeaderStyle = tempstyle;

                    // Set visibility
                    constructionColumn.Visibility = columnvisibility;

                    dbDataGrid.Columns.Add(constructionColumn);
                }
            }
        }

        private DataTable CreateTable(DBFile table)
        {
            DataTable constructionTable = new DataTable(currentPackedFile.Name);

            // If the table already exists just re-load it.
            // 
            // OLD: Add the new table to the DataSet, if it exists already remove it since the table may have been edited with 
            // OLD: another control between then and now.
            if (loadedDataSet.Tables.Contains(constructionTable.TableName))
            {
                return loadedDataSet.Tables[currentPackedFile.Name];
                //loadedDataSet.Tables.Remove(constructionTable.TableName);
            }
            loadedDataSet.Tables.Add(constructionTable);

            DataColumn constructionColumn;
            List<DataColumn> keyList = new List<DataColumn>();
            constructionTable.BeginLoadData();

            foreach (FieldInfo columnInfo in table.CurrentType.Fields)
            {
                // Create the new column
                constructionColumn = new DataColumn(columnInfo.Name, GetTypeFromCode(columnInfo.TypeCode));
                constructionColumn.AllowDBNull = columnInfo.Optional;
                constructionColumn.Unique = false;
                constructionColumn.ReadOnly = readOnly;

                // Save the FKey if it exists
                if (!String.IsNullOrEmpty(columnInfo.ForeignReference))
                {
                    constructionColumn.ExtendedProperties.Add("FKey", columnInfo.ForeignReference);
                }

                // If the column is a primary key, save it for later adding
                if (columnInfo.PrimaryKey)
                {
                    keyList.Add(constructionColumn);
                }

                constructionTable.Columns.Add(constructionColumn);
            }

            // If the table has primary keys, set them.
            if (keyList.Count > 0)
            {
                constructionTable.PrimaryKey = keyList.ToArray();
            }

            // Now that the DataTable schema is contructed, add in all the data.
            foreach (List<FieldInstance> rowentry in table.Entries)
            {
                constructionTable.Rows.Add(rowentry.Select(n => n.Value).ToArray<object>());
            }

            constructionTable.EndLoadData();
            constructionTable.AcceptChanges();

            return constructionTable;
        }

        public void SetColumnSizes()
        {
            Dictionary<DataGridColumn, double> desiredsizes = new Dictionary<DataGridColumn,double>();

            foreach (DataGridColumn column in dbDataGrid.Columns.Reverse())
            {
                double maxdesiredwidth = 0;

                for (int i = 0; i < dbDataGrid.Items.Count; i++)
                {
                    DataGridCell cell = GetCell(i, column.DisplayIndex);

                    if (cell == null)
                    {
                        continue;
                    }

                    maxdesiredwidth = Math.Max(maxdesiredwidth, cell.DesiredSize.Width);
                }

                maxdesiredwidth = Math.Max(maxdesiredwidth, column.Width.DesiredValue);
                desiredsizes.Add(column, maxdesiredwidth);
            }

            dbDataGrid.ColumnWidth = DataGridLength.Auto;

            foreach (DataGridColumn column in desiredsizes.Keys)
            {
                if (desiredsizes[column] > 1)
                {

                    column.Width = desiredsizes[column];
                }
                else
                {
                    column.Width = DataGridLength.Auto;
                }
            }
        }
        
        private void Import(DBFile importfile)
        {
            // If we are here, then importfile has already been imported into editfile, so no need to do any type checking.
            // The old DBE would check for matching keys and overwrite any it found, data validation means this is no longer
            // necessary to maintain GUI integrity.
            
            // Unbind the GUI datasource, and tell currentTable to get ready for new data.
            dbDataGrid.ItemsSource = null;
            currentTable.BeginLoadData();

            MessageBoxResult question = MessageBox.Show("Replace the current data?", "Replace data?", 
                                                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (question == MessageBoxResult.Cancel) {
                return;
            } else if (question == MessageBoxResult.Yes) {
                currentTable.Clear();
            }

            // Since Data.Rows lacks an AddRange method, enumerate through the entries manually.
            foreach (List<FieldInstance> entry in importfile.Entries)
            {
                DataRow row = currentTable.NewRow();
                row.ItemArray = entry.Select(n => n.Value).ToArray();
                CurrentTable.Rows.Add(row);
                var test = entry.Select(n => n.Value).ToArray(); ;
            }
            
            currentTable.EndLoadData();
            dbDataGrid.ItemsSource = CurrentTable.DefaultView;
        }

        private bool ReferenceContainsAllValues(List<string> referencevalues, DataColumn column)
        {
            foreach (object item in column.GetItemArray())
            {
                if (!referencevalues.Contains(item.ToString()))
                {
                    return false;
                }
            }

            return true;
        }

        #region Toolbar Events
        private void AddRowButton_Clicked(object sender, RoutedEventArgs e)
        {
            DataRow row = CurrentTable.NewRow();
            CurrentTable.Rows.Add(row);
            
            dataChanged = true;
        }

        private void CloneRowButton_Clicked(object sender, RoutedEventArgs e)
        {
            // Only do anything if atleast 1 row is selected.
            if (dbDataGrid.SelectedItems.Count > 0)
            {
                foreach (DataRowView rowview in dbDataGrid.SelectedItems)
                {
                    DataRow row = CurrentTable.NewRow();
                    row.ItemArray = rowview.Row.ItemArray.ToArray();
                    CurrentTable.Rows.Add(row);
                }
            }

            dataChanged = true;
        }

        private void ExportAsButton_Clicked(object sender, RoutedEventArgs e)
        {
            ExportContextMenu.PlacementTarget = (Button)sender;
            ExportContextMenu.IsOpen = true;
        }

        private void ImportFromButton_Clicked(object sender, RoutedEventArgs e)
        {
            ImportContextMenu.PlacementTarget = (Button)sender;
            ImportContextMenu.IsOpen = true;
        }

        private void ExportTSVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string extractTo = ModDirectory;
            // TODO: Add support for ModManager
            //extractTo = ModManager.Instance.CurrentModSet ? ModManager.Instance.CurrentModDirectory : null;
            if (extractTo == null)
            {
                DirectoryDialog dialog = new DirectoryDialog
                {
                    Description = "Please point to folder to extract to",
                    SelectedPath = String.IsNullOrEmpty(exportDirectory)
                                    ? System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
                                    : exportDirectory
                };
                extractTo = dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
                exportDirectory = dialog.SelectedPath;
            }
            if (!string.IsNullOrEmpty(extractTo))
            {
                List<PackedFile> files = new List<PackedFile>();
                files.Add(CurrentPackedFile);
                FileExtractor extractor = new FileExtractor(extractTo) { Preprocessor = new TsvExtractionPreprocessor() };
                extractor.ExtractFiles(files);
                MessageBox.Show(string.Format("File exported to TSV."));
            }
        }

        private void ExportCSVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            /*
            string extractTo = null;
            // TODO: Add support for ModManager
            //extractTo = ModManager.Instance.CurrentModSet ? ModManager.Instance.CurrentModDirectory : null;
            if (extractTo == null)
            {
                DirectoryDialog dialog = new DirectoryDialog
                {
                    Description = "Please point to folder to extract to",
                    SelectedPath = String.IsNullOrEmpty(importExportDirectory)
                                    ? System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
                                    : importExportDirectory
                };
                extractTo = dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
                importExportDirectory = dialog.SelectedPath;
            }
            if (!string.IsNullOrEmpty(extractTo))
            {
                List<PackedFile> files = new List<PackedFile>();
                files.Add(CurrentPackedFile);
                FileExtractor extractor = new FileExtractor(extractTo) { Preprocessor = new CsvExtractionPreprocessor() };
                extractor.ExtractFiles(files);
                MessageBox.Show(string.Format("File exported to CSV."));
            }
             */
        }

        private void ExportBinaryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string extractTo = null;
            // TODO: Add support for ModManager
            //extractTo = ModManager.Instance.CurrentModSet ? ModManager.Instance.CurrentModDirectory : null;
            if (extractTo == null)
            {
                DirectoryDialog dialog = new DirectoryDialog
                {
                    Description = "Please point to folder to extract to",
                    SelectedPath = String.IsNullOrEmpty(exportDirectory) 
                                    ? System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) 
                                    : exportDirectory
                };
                extractTo = dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
                exportDirectory = dialog.SelectedPath;
            }
            if (!string.IsNullOrEmpty(extractTo))
            {
                List<PackedFile> files = new List<PackedFile>();
                files.Add(CurrentPackedFile);
                FileExtractor extractor = new FileExtractor(extractTo);
                extractor.ExtractFiles(files);
                MessageBox.Show(string.Format("File exported as binary."));
            }
        }

        private void ExportCAXmlMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Write PackedFile EncodeasCAXml()
            Refresh();
        }

        private void ImportTSVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string initialDirectory = ModDirectory != null ? ModDirectory : exportDirectory;
            System.Windows.Forms.OpenFileDialog openDBFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = initialDirectory,
                FileName = String.Format("{0}.tsv", EditedFile.CurrentType.Name)
            };

            DBFile loadedfile = null;
            bool tryAgain = false;
            if (openDBFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                importDirectory = System.IO.Path.GetDirectoryName(openDBFileDialog.FileName);
                do
                {
                    try
                    {
                        try
                        {
                            using (var stream = new MemoryStream(File.ReadAllBytes(openDBFileDialog.FileName)))
                            {
                                loadedfile = new TextDbCodec().Decode(stream);
                                // No need to import to editedFile directly, since it will be handled in the 
                                // CurrentTable_TableNewRow event handler.
                                //editedFile.Import(loadedfile);
                                Import(loadedfile);
                            }

                        }
                        catch (DBFileNotSupportedException exception)
                        {
                            showDBFileNotSupportedMessage(exception.Message);
                        }

                        currentPackedFile.Data = (codec.Encode(EditedFile));
                    }
                    catch (Exception ex)
                    {
                        tryAgain = (System.Windows.Forms.MessageBox.Show(string.Format("Import failed: {0}", ex.Message),
                            "Import failed",
                            System.Windows.Forms.MessageBoxButtons.RetryCancel)
                            == System.Windows.Forms.DialogResult.Retry);
                    }
                } while (tryAgain);
            }
        }

        private void ImportCSVMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openDBFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = exportDirectory,
                FileName = String.Format("{0}.csv", EditedFile.CurrentType.Name)
            };

            DBFile loadedfile = null;
            bool tryAgain = false;
            if (openDBFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                importDirectory = System.IO.Path.GetDirectoryName(openDBFileDialog.FileName);
                do
                {
                    try
                    {
                        try
                        {
                            using (var stream = new MemoryStream(File.ReadAllBytes(openDBFileDialog.FileName)))
                            {
                                loadedfile = new TextDbCodec().Decode(stream);
                                // No need to import to editedFile directly, since it will be handled in the 
                                // CurrentTable_TableNewRow event handler.
                                //editedFile.Import(loadedfile);
                                Import(loadedfile);
                            }

                        }
                        catch (DBFileNotSupportedException exception)
                        {
                            showDBFileNotSupportedMessage(exception.Message);
                        }

                        currentPackedFile.Data = (codec.Encode(EditedFile));
                    }
                    catch (Exception ex)
                    {
                        tryAgain = (System.Windows.Forms.MessageBox.Show(string.Format("Import failed: {0}", ex.Message),
                            "Import failed",
                            System.Windows.Forms.MessageBoxButtons.RetryCancel)
                            == System.Windows.Forms.DialogResult.Retry);
                    }
                } while (tryAgain);
            }
        }

        private void ImportBinaryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openDBFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = exportDirectory,
                FileName = EditedFile.CurrentType.Name
            };

            DBFile loadedfile = null;
            bool tryAgain = false;
            if (openDBFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                importDirectory = System.IO.Path.GetDirectoryName(openDBFileDialog.FileName);
                do
                {
                    try
                    {
                        try
                        {
                            using (var stream = new MemoryStream(File.ReadAllBytes(openDBFileDialog.FileName)))
                            {
                                loadedfile = codec.Decode(stream);
                                // No need to import to editedFile directly, since it will be handled in the 
                                // CurrentTable_TableNewRow event handler.
                                //editedFile.Import(loadedfile);
                                Import(loadedfile);
                            }

                        }
                        catch (DBFileNotSupportedException exception)
                        {
                            showDBFileNotSupportedMessage(exception.Message);
                        }

                        currentPackedFile.Data = (codec.Encode(EditedFile));
                    }
                    catch (Exception ex)
                    {
                        tryAgain = (System.Windows.Forms.MessageBox.Show(string.Format("Import failed: {0}", ex.Message),
                            "Import failed",
                            System.Windows.Forms.MessageBoxButtons.RetryCancel)
                            == System.Windows.Forms.DialogResult.Retry);
                    }
                } while (tryAgain);
            }
        }

        private void ImportCAXmlMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement
        }

        #endregion

        #region DataGrid Events

        private void dbDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Resetting the cell's Style forces a UI ReDraw to occur without disturbing the rest of the datagrid.
            DataGridCell cell = e.EditingElement.Parent as DataGridCell;

            Style TempStyle = cell.Style;
            cell.Style = null;
            cell.Style = TempStyle;
        }

        private void dbDataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Used to set up the 2-click edit for ComboBox and CheckBox cells.
            DataGridCell cell = sender as DataGridCell;
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly && !CurrentTable.HasErrors)
            {
                if (cell.Content is CheckBox || cell.Content is ComboBox)
                {
                    cell.Focus();
                    cell.IsEditing = true;
                }
            }
        }

        private void dbDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is ScrollViewer)
            {
                // User clicked 'outside' the datagrid, deselect everthing.
                dbDataGrid.UnselectAll();
                dbDataGrid.UnselectAllCells();
            }
        }

        private void dbDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/Disable Clone Row button based on current selection.
            if (!readOnly)
            {
            cloneRowButton.IsEnabled = dbDataGrid.SelectedItems.Count > 0;
        }
        }

        private void dbDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var typetest = e.Row.Item;

            if (!(typetest is DataRowView))
            {
                e.Row.Header = "*";
            }
            else
            {
                e.Row.Header = CurrentTable.Rows.IndexOf((e.Row.Item as DataRowView).Row) + 1;
            }
        }

        protected virtual void OnExecutedPaste(object sender, ExecutedRoutedEventArgs args)
        {
            string clipboarddata = (string)Clipboard.GetData(DataFormats.UnicodeText);
            int rowIndex;
            int columnIndex;

            if (ClipboardIsEmpty())
            {
                // Clipboard Empty
            }
            else if (ClipboardContainsSingleCell())
            {
                // Single Cell Paste
                string pastevalue = clipboarddata.Trim();

                foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells)
                {
                    rowIndex = CurrentTable.Rows.IndexOf((cellinfo.Item as DataRowView).Row);
                    columnIndex = cellinfo.Column.DisplayIndex;

                    CurrentTable.Rows[rowIndex].BeginEdit();

                    if (!TryPasteValue(rowIndex, columnIndex, pastevalue))
                    {
                        // Paste Error
                    }

                    CurrentTable.Rows[rowIndex].EndEdit();
                }
            }
            else if (!ClipboardContainsOnlyRows())
            {
                if (dbDataGrid.SelectedItems.OfType<DataRowView>().Count() != dbDataGrid.SelectedItems.Count)
                {
                    // The blank row is selected, abort.
                    MessageBox.Show("Only select the blank row when pasting rows.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Use -1 values to indicate that nothing is selected, and to paste any full rows the
                // clipboard might contain as new rows.
                int baseColumnIndex = -1;
                rowIndex = -1;
                if (dbDataGrid.SelectedCells.Count > 1)
                {
                    // User has more than 1 cells selected, therefore the selection must match the clipboard data.
                    if (!PasteMatchesSelection(clipboarddata))
                    {
                        // Warn user
                        if (MessageBox.Show("Warning! Cell selection does not match copied data, attempt to paste anyway?",
                                           "Selection Error", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        {
                            return;
                        }
                    }

                    // Set values to the first cell's coordinates
                    rowIndex = CurrentTable.Rows.IndexOf((dbDataGrid.SelectedCells[0].Item as DataRowView).Row);
                    baseColumnIndex = dbDataGrid.SelectedCells[0].Column.DisplayIndex;

                    // Determine upper left corner of selection
                    foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells)
                    {
                        rowIndex = Math.Min(rowIndex, dbDataGrid.Items.IndexOf(cellinfo.Item));
                        baseColumnIndex = Math.Min(baseColumnIndex, dbDataGrid.Columns.IndexOf(cellinfo.Column));
                    }
                }
                else if (dbDataGrid.SelectedCells.Count == 1)
                {
                    // User has 1 cell selected, assume it is the top left corner and attempt to paste.
                    rowIndex = CurrentTable.Rows.IndexOf((dbDataGrid.SelectedCells[0].Item as DataRowView).Row);
                    baseColumnIndex = dbDataGrid.SelectedCells[0].Column.DisplayIndex;
                }

                foreach (string line in clipboarddata.Split('\n'))
                {
                    columnIndex = baseColumnIndex;

                    if (rowIndex > CurrentTable.Rows.Count - 1 || columnIndex == -1)
                    {
                        if (IsLineARow(line))
                        {
                            // We have a full row, but no where to paste it, so add it as a new row.
                            DataRow newrow = currentTable.NewRow();
                            newrow.ItemArray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToArray<object>();
                            currentTable.Rows.Add(newrow);
                        }

                        rowIndex++;
                        continue;
                    }

                    if (String.IsNullOrEmpty(line.Trim()))
                    {
                        rowIndex++;
                        continue;
                    }

                    foreach (string cell in line.Replace("\r", "").Split('\t'))
                    {
                        if (columnIndex > CurrentTable.Columns.Count - 1)
                        {
                            break;
                        }

                        if (String.IsNullOrEmpty(cell.Trim()))
                        {
                            columnIndex++;
                            continue;
                        }

                        CurrentTable.Rows[rowIndex].BeginEdit();

                        if (!TryPasteValue(rowIndex, columnIndex, cell.Trim()))
                        {
                            // Paste Error
                        }

                        CurrentTable.Rows[rowIndex].EndEdit();

                        dataChanged = true;
                        columnIndex++;
                    }

                    rowIndex++;
                }
            }
            else
            {
                // Paste Rows, with no floater cells.
                if (dbDataGrid.SelectedCells.Count == (dbDataGrid.SelectedItems.Count * EditedFile.CurrentType.Fields.Count))
                {
                    // Only rows are selected.
                    // Since the SelectedItems list is in the order of selection and NOT in the order of appearance we need
                    // to create a custom sorted list of indicies to paste to.
                    List<int> indiciesToPaste = new List<int>();
                    foreach (DataRowView rowview in dbDataGrid.SelectedItems.OfType<DataRowView>())
                    {
                        indiciesToPaste.Add(currentTable.Rows.IndexOf(rowview.Row));
                    }
                    indiciesToPaste.Sort();

                    rowIndex = 0;

                    foreach (string line in clipboarddata.Replace("\r", "").Split('\n'))
                    {
                        if (!IsLineARow(line) || String.IsNullOrEmpty(line))
                        {
                            rowIndex++;
                            continue;
                        }

                        if (rowIndex >= indiciesToPaste.Count)
                        {
                            // Add new row
                            DataRow newrow = currentTable.NewRow();
                            newrow.ItemArray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToArray<object>();
                            currentTable.Rows.Add(newrow);

                            rowIndex++;
                            continue;
                        }

                        currentTable.Rows[indiciesToPaste[rowIndex]].ItemArray = line.Split('\t').ToArray<object>();
                        rowIndex++;
                    }
                }
                else
                {
                    // Please select rows.
                    MessageBox.Show("When pasting rows, please use the row header button to select entire rows only.", 
                                    "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            Refresh(true);
        }

        protected virtual void OnCanExecutePaste(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = dbDataGrid.CurrentCell != null;
            args.CanExecute = !readOnly;
            args.Handled = true;
        }

        #endregion

        #region Context Menu Events

        private void ColumnHeaderContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            DataGridColumn col = ((sender as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;
            ContextMenu currentmenu = (ContextMenu)sender;

            Type columntype = currentTable.Columns[(string)col.Header].DataType;
            foreach (MenuItem item in currentmenu.Items.OfType<MenuItem>())
            {
                // Enable/Disable Remove Sorting item based on if the column is actually sorted or not.
                if(item.Header.Equals("Remove Sorting"))
                {
                    item.IsEnabled = col.SortDirection != null;
                }

                // Enable/Disable Apply expression and renumber based on column type.
                if (item.Header.Equals("Apply Expression") || item.Header.Equals("Renumber Cells"))
                {
                    if (!col.IsReadOnly && (columntype.Name.Equals("Single") || columntype.Name.Equals("Int32") || columntype.Name.Equals("Int16")))
                    {
                        item.IsEnabled = true;
                    }
                    else
                    {
                        item.IsEnabled = false;
                    }
                }
                
                // Hide <--> Unhide based on current hidden status.
                if (item.Header.Equals("Hide Column") || item.Header.Equals("Unhide Column"))
                {
                    if (currentTable.Columns[(string)col.Header].ExtendedProperties.ContainsKey("Hidden"))
                    {
                        if ((bool)currentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"])
                        {
                            item.Header = "Unhide Column";
                        }
                        else
                        {
                            item.Header = "Hide Column";
                        }
                    }
                }
            }
        }

        private void SelectColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;

            int columnindex = dbDataGrid.Columns.IndexOf(col);
            for (int i = 0; i < dbDataGrid.Items.Count; i++)
            {
                // Test if the cell is already contained in SelectedCells
                DataGridCellInfo cellinfo = new DataGridCellInfo(dbDataGrid.Items[i], col);
                if (!dbDataGrid.SelectedCells.Contains(cellinfo))
                {
                    dbDataGrid.SelectedCells.Add(cellinfo);
                }
            }
        }

        private void RemoveSortingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;

            col.SortDirection = null;

            Refresh();
        }

        private void ColumnApplyExpressionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;

            ApplyExpressionWindow getexpwindow = new ApplyExpressionWindow();
            getexpwindow.ShowDialog();

            if (getexpwindow.DialogResult != null && (bool)getexpwindow.DialogResult)
            {
                for (int i = 0; i < currentTable.Rows.Count; i++)
                {
                    // Grab the given expression, modifying it for each cell.
                    string expression = getexpwindow.EnteredExpression.Replace("x", string.Format("{0}", currentTable.Rows[i][(string)col.Header]));
                    currentTable.Rows[i][(string)col.Header] = currentTable.Compute(expression, "");
                }
            }

            RefreshColumn(col.DisplayIndex);
        }

        private void RenumberMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the column this context menu was called from.
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;

            InputBox renumberInputBox = new InputBox { Text = "Re-Number from", Input = "1" };
            if (renumberInputBox.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    int parsedNumber = int.Parse(renumberInputBox.Input);
                    for (int i = 0; i < dbDataGrid.Items.Count; i++)
                    {
                        // Skip any non DataRowView, which should only be the blank row at the bottom.
                        if (!(dbDataGrid.Items[i] is DataRowView))
                        {
                            continue;
                        }
                        // Get the row from the datagrid, and set the value in the data source.
                        DataRow row = (dbDataGrid.Items[i] as DataRowView).Row;
                        row[(string)col.Header] = i + parsedNumber;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Could not apply values: {0}", ex.Message), "You fail!");
                }
            }

            RefreshColumn(col.DisplayIndex);
        }

        private void HideColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;

            if (!CurrentTable.Columns[(string)col.Header].ExtendedProperties.ContainsKey("Hidden"))
            {
                CurrentTable.Columns[(string)col.Header].ExtendedProperties.Add("Hidden", false);
            }

            // 3 possible scenarios:
            if (col.Visibility == Visibility.Visible && !(bool)CurrentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"])
            {
                // 1. Column is visible and is not set as hidden
                // If we are not showing hidden columns, set actual visibility
                if (!showAllColumns)
                {
                    col.Visibility = Visibility.Hidden;
                }

                // Consider the column as hidden.
                CurrentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"] = true;

                // Add the column to the internal hidden columns list.
                if (!hiddenColumns.Contains((string)col.Header))
                {
                    hiddenColumns.Add((string)col.Header);
                }
            }
            else if (col.Visibility == Visibility.Visible && (bool)CurrentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"])
            {
                // 2. Column is visible, but considered hidden, meaning we are showing hidden columns.
                // Consider the column as visible, but do not change actual visibility.
                CurrentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"] = false;

                // Remove the column from the internal hidden columns list.
                if (hiddenColumns.Contains((string)col.Header))
                {
                    hiddenColumns.Remove((string)col.Header);
                }
            }
            else if (col.Visibility == Visibility.Hidden)
            {
                // 3. Column is hidden, meaning we are not showing all hidden columns.
                // Set actual visibility.
                col.Visibility = Visibility.Visible;

                // Consider the column as hidden.
                CurrentTable.Columns[(string)col.Header].ExtendedProperties["Hidden"] = false;

                // Remove the column from the internal hidden columns list.
                if (hiddenColumns.Contains((string)col.Header))
                {
                    hiddenColumns.Remove((string)col.Header);
                }
            }

            UpdateConfig();

            if (showAllColumns)
            {
            Refresh();
        }
        }

        private void EditVisibleListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Get clicked column.
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;

            ListEditor hiddencolumnslisteditor = new ListEditor();
            hiddencolumnslisteditor.LeftLabel = "Visible Columns:";
            hiddencolumnslisteditor.RightLabel = "Hidden Columns:";
            hiddencolumnslisteditor.OriginalOrder = dbDataGrid.Columns.Select(n => (string)n.Header).ToList<string>();
            hiddencolumnslisteditor.LeftList = currentTable.Columns.OfType<DataColumn>()
                                                                   .Where(n => !(bool)n.ExtendedProperties["Hidden"])
                                                                   .Select(n => n.ColumnName).ToList();
            hiddencolumnslisteditor.RightList = currentTable.Columns.OfType<DataColumn>()
                                                                   .Where(n => (bool)n.ExtendedProperties["Hidden"])
                                                                   .Select(n => n.ColumnName).ToList();

            System.Windows.Forms.DialogResult result = hiddencolumnslisteditor.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                hiddenColumns.Clear();
                hiddenColumns = hiddencolumnslisteditor.RightList;

                foreach (DataColumn column in CurrentTable.Columns)
                {
                    if (hiddencolumnslisteditor.LeftList.Contains(column.ColumnName))
                    {
                        column.ExtendedProperties["Hidden"] = false;
                        dbDataGrid.Columns[column.Ordinal].Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        column.ExtendedProperties["Hidden"] = true;

                        if (showAllColumns)
                        {
                            dbDataGrid.Columns[column.Ordinal].Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            dbDataGrid.Columns[column.Ordinal].Visibility = System.Windows.Visibility.Hidden;
                        }
                    }
                }

                UpdateConfig();

                if (showAllColumns)
                {
                Refresh();
            }
        }
        }

        private void ClearTableHiddenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataGridColumn column in dbDataGrid.Columns)
            {
                DataColumn datacolumn = currentTable.Columns[(string)column.Header];

                if (!datacolumn.ExtendedProperties.ContainsKey("Hidden"))
                {
                    datacolumn.ExtendedProperties.Add("Hidden", false);
                }
                datacolumn.ExtendedProperties["Hidden"] = false;

                column.Visibility = Visibility.Visible;
            }

            // Clear the internal hidden columns list.
            hiddenColumns.Clear();
            UpdateConfig();

            if (showAllColumns)
            {
            Refresh();
        }
        }

        private void ClearAllHiddenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Prompt for confirmation.
            string text = "Are you sure you want to clear all saved hidden column information?";
            string caption = "Clear Confirmation";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage image = MessageBoxImage.Question;

            MessageBoxResult result = MessageBox.Show(text, caption, button, image);

            if (result == MessageBoxResult.Yes)
            {
                // Clear internal list.
                hiddenColumns.Clear();

                // Clear saved list.
                savedconfig.HiddenColumns.Clear();
            }
            UpdateConfig();

            if (showAllColumns)
            {
            Refresh();
        }
        }

        #endregion

        #region Datatable Events

        void CurrentTable_TableNewRow(object sender, DataTableNewRowEventArgs e)
        {
            // Add the new row to editedfile.
            List<FieldInstance> dbfileconstructionRow = new List<FieldInstance>();
            for (int i = 0; i < e.Row.ItemArray.Length; i++)
            {
                dbfileconstructionRow.Add(editedFile.CurrentType.Fields[i].CreateInstance());
            }

            editedFile.Entries.Add(dbfileconstructionRow);

            dataChanged = true;
        }

        void CurrentTable_RowDeleting(object sender, DataRowChangeEventArgs e)
        {
            int removalindex = e.Row.Table.Rows.IndexOf(e.Row);
            editedFile.Entries.RemoveAt(removalindex);

            dataChanged = true;
        }

        void CurrentTable_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
            if (e.Row.RowState != DataRowState.Detached)
            {
                // Remove the row, because otherwise there will be indexing issues due to how the DataTable class handles row deletion.
                currentTable.Rows.Remove(e.Row);
            }

            dataChanged = true;
        }

        void CurrentTable_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            // Set row index as either the last row in edited file if we are creating a new row.
            int rowIndex = e.Row.RowState == DataRowState.Detached ? EditedFile.Entries.Count - 1 : e.Row.Table.Rows.IndexOf(e.Row);
            int colIndex = e.Column.Ordinal;

            editedFile.Entries[rowIndex][colIndex].Value = e.ProposedValue.ToString();

            dataChanged = true;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(object sender, string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }

        #region UI Helper Methods

        public DataGridCell GetCell(int row, int column, bool onlyvisible = false)
        {
            DataGridRow rowContainer = GetRow(row);

            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                // UI Virtualization may interfere with the presenter, so scroll to the item and try again.
                if (presenter == null)
                {
                    // If vitrualized and not in view, ignore based on optional paramater.
                    if (onlyvisible)
                    {
                        return null;
                    }

                    dbDataGrid.ScrollIntoView(rowContainer, dbDataGrid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);
                }

                // try to get the cell but it may possibly be virtualized
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    // If virtualized possibly ignore
                    if (onlyvisible)
                    {
                        return null;
                    }

                    // now try to bring into view and retreive the cell
                    dbDataGrid.ScrollIntoView(rowContainer, dbDataGrid.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public DataGridRow GetRow(int index)
        {
            DataGridRow row = (DataGridRow)dbDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // may be virtualized, bring into view and try again
                dbDataGrid.ScrollIntoView(dbDataGrid.Items[index]);
                row = (DataGridRow)dbDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            var test = LogicalTreeHelper.GetChildren(parent);
            LogicalTreeHelper.BringIntoView(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }

                if (child != null)
                {
                    break;
                }
            }

            return child;
        }

        static List<T> GetVisualChildren<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            List<T> returnlist = new List<T>();
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            LogicalTreeHelper.BringIntoView(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    var test = GetVisualChildren<T>(v);
                    if (test == null)
                    {
                        continue;
                    }
                    returnlist.AddRange(GetVisualChildren<T>(v));
                }

                if (child != null)
                {
                    returnlist.Add(child as T);
                }
            }

            return returnlist;
        }

        public DependencyObject FindFirstControlInChildren(DependencyObject obj, string controlType)
        {
            if (obj == null)
                return null;

            // Get a list of all occurrences of a particular type of control (eg "CheckBox") 
            IEnumerable<DependencyObject> ctrls = FindInVisualTreeDown(obj, controlType);
            if (ctrls.Count() == 0)
                return null;

            return ctrls.First();
        }

        public IEnumerable<DependencyObject> FindInVisualTreeDown(DependencyObject obj, string type)
        {
            if (obj != null)
            {
                if (obj.GetType().ToString().EndsWith(type))
                {
                    yield return obj;
                }

                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    foreach (var child in FindInVisualTreeDown(VisualTreeHelper.GetChild(obj, i), type))
                    {
                        if (child != null)
                        {
                            yield return child;
                        }
                    }
                }
            }
            yield break;
        }

        private void Refresh(bool onlyvisible = false)
        {
            if (!onlyvisible)
            {
            // TODO: Try to find another way to force refresh the screen.
            var selectedItems = dbDataGrid.SelectedItems;
            List<DataGridCellInfo> selectedCells = dbDataGrid.SelectedCells.ToList();
            List<int> selectedItemsIndicies = dbDataGrid.SelectedItems.OfType<DataRowView>().Select(n => dbDataGrid.Items.IndexOf(n)).ToList();

            dbDataGrid.ItemsSource = null;
            dbDataGrid.ItemsSource = CurrentTable.DefaultView;

            foreach (int index in selectedItemsIndicies)
            {
                dbDataGrid.SelectedItems.Add(dbDataGrid.Items[index]);
            }

            foreach (DataGridCellInfo cellinfo in selectedCells)
            {
                DataGridCellInfo cellToAdd = new DataGridCellInfo(cellinfo.Item, cellinfo.Column);
                if (!dbDataGrid.SelectedCells.Contains(cellToAdd))
                {
                    dbDataGrid.SelectedCells.Add(cellToAdd);
                }
            }
        }
            else // Refresh only visible elements, column by column.
            {
                for (int i = 0; i < currentTable.Columns.Count; i++)
                {
                    RefreshColumn(i);
                }
            }
        }

        private void RefreshColumn(int column)
        {
            for (int i = 0; i < dbDataGrid.Items.Count; i++)
            {
                DataGridCell cell = GetCell(i, column, true);

                if (cell != null)
                {
                    // Resetting the cell's Style forces a UI ReDraw to occur without disturbing the rest of the datagrid.
                    Style TempStyle = cell.Style;
                    cell.Style = null;
                    cell.Style = TempStyle;
                }
#if DEBUG
                else
                {
                    string breakpointstring = "";
                }
#endif
            }
        }

        private void RefreshCell(int row, int column)
        {
            DataGridCell cell = GetCell(row, column, true);

            if (cell != null)
            {
                // Resetting the cell's Style forces a UI ReDraw to occur without disturbing the rest of the datagrid.
                Style TempStyle = cell.Style;
                cell.Style = null;
                cell.Style = TempStyle;
            }
        }

        #endregion

        #region Utility Functions

        public List<List<string>> GetPrimaryKeySequences()
        {
            List<List<string>> pksequences = new List<List<string>>();

            foreach (List<FieldInstance> row in EditedFile.Entries)
            {
                pksequences.Add(row.Where(n => n.Info.PrimaryKey).Select(n => n.Value).ToList<string>());
            }

            return pksequences;
        }

        private void UpdateConfig()
        {
            if (EditedFile == null)
            {
                return;
            }

            // Save off any required information before changing anything.
            savedconfig.FreezeKeyColumns = freezeKeyColumns;
            savedconfig.UseComboBoxes = useComboBoxes;
            savedconfig.ShowAllColumns = showAllColumns;
            savedconfig.ImportDirectory = importDirectory;
            savedconfig.ExportDirectory = exportDirectory;

            if (editedFile != null)
            {
                if (savedconfig.HiddenColumns.ContainsKey(editedFile.CurrentType.Name))
                {
                    // Overwrite the old hidden column list for this table.
                    savedconfig.HiddenColumns[editedFile.CurrentType.Name].Clear();
                    savedconfig.HiddenColumns[editedFile.CurrentType.Name].AddRange(hiddenColumns);
                }
                else
                {
                    // Create a new list for the table.
                    savedconfig.HiddenColumns.Add(new KeyValuePair<string, List<string>>(editedFile.CurrentType.Name, new List<string>(hiddenColumns)));
                }
            }
            savedconfig.HiddenColumns.Sort();
            savedconfig.Save();
        }

        private void showDBFileNotSupportedMessage(string message)
        {
            // Set the warning box as visible.
            dbDataGrid.Visibility = System.Windows.Visibility.Hidden;
            unsupportedDBErrorTextBox.Visibility = System.Windows.Visibility.Visible;

            // Set the message
            unsupportedDBErrorTextBox.Text = string.Format("{0}{1}", message, string.Join("\r\n", DBTypeMap.Instance.DBFileTypes));

            // Modify controls accordingly
            // Most controls useability are bound by TableReadOnly, so set it.
            readOnly = true;
            // Modify the remaining controls manually.
            exportAsButton.IsEnabled = false;
        }

        private Type GetTypeFromCode(TypeCode code)
        {
            switch (code)
            {
                case TypeCode.Boolean:
                    return typeof(bool);

                case TypeCode.Byte:
                    return typeof(byte);

                case TypeCode.Char:
                    return typeof(char);

                case TypeCode.DateTime:
                    return typeof(DateTime);

                case TypeCode.DBNull:
                    return typeof(DBNull);

                case TypeCode.Decimal:
                    return typeof(decimal);

                case TypeCode.Double:
                    return typeof(double);

                case TypeCode.Empty:
                    return typeof(string);

                case TypeCode.Int16:
                    return typeof(short);

                case TypeCode.Int32:
                    return typeof(int);

                case TypeCode.Int64:
                    return typeof(long);

                case TypeCode.Object:
                    return typeof(object);

                case TypeCode.SByte:
                    return typeof(sbyte);

                case TypeCode.Single:
                    return typeof(Single);

                case TypeCode.String:
                    return typeof(string);

                case TypeCode.UInt16:
                    return typeof(UInt16);

                case TypeCode.UInt32:
                    return typeof(UInt32);

                case TypeCode.UInt64:
                    return typeof(UInt64);
            }

            return null;
        }

        private bool ClipboardIsEmpty()
        {
            string clipboardText = Clipboard.GetText();

            foreach (string line in clipboardText.Split('\n'))
            {
                foreach (string cell in line.Split('\t'))
                {
                    if (!String.IsNullOrEmpty(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ClipboardContainsOnlyRows()
        {
            string clipboardText = Clipboard.GetText();

            foreach (string line in clipboardText.Split('\n'))
            {
                if (String.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (!IsLineARow(line))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsLineARow(string line)
        {
            if (line.Count(n => n == '\t') >= editedFile.CurrentType.Fields.Count - 1)
            {
                bool fullrow = true;
                string[] cells = line.Split('\t').Take(EditedFile.CurrentType.Fields.Count).ToArray();
                for (int i = 0; i < cells.Length; i++)
                {
                    if (String.IsNullOrEmpty(cells[i]) &&
                        (!editedFile.CurrentType.Fields[i].Optional && editedFile.CurrentType.Fields[i].TypeCode != TypeCode.String) &&
                        !editedFile.CurrentType.Fields[i].TypeName.Equals("optstring"))
                    {
                        fullrow = false;
                        break;
                    }
                }

                if (fullrow)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ClipboardContainsSingleCell()
        {
            return !Clipboard.GetText().Contains('\t') && Clipboard.GetText().Count(n => n == '\n') == 1;
        }

        private bool TryPasteValue(int rowIndex, int columnIndex, string value)
        {
            // Paste Event
            Type celltype = CurrentTable.Columns[columnIndex].DataType;
            bool retval = false;

            if (celltype.Name == "String")
            {
                CurrentTable.Rows[rowIndex][columnIndex] = value.Trim();
                retval = true;
            }
            else if (celltype.Name == "Single")
            {
                try
                {
                    CurrentTable.Rows[rowIndex][columnIndex] = float.Parse(value.Trim());
                    retval = true;
                }
                catch(Exception e)
                {
                    ErrorDialog.ShowDialog(e);
                }
            }
            else if (celltype.Name == "Boolean")
            {
                try
                {
                    CurrentTable.Rows[rowIndex][columnIndex] = bool.Parse(value.Trim());
                    retval = true;
                }
                catch (Exception e)
                {
                    ErrorDialog.ShowDialog(e);
                }
            }
            else if (celltype.Name == "Int32")
            {
                try
                {
                    CurrentTable.Rows[rowIndex][columnIndex] = int.Parse(value.Trim());
                    retval = true;
                }
                catch (Exception e)
                {
                    ErrorDialog.ShowDialog(e);
                }
            }
            else if (celltype.Name == "Int16")
            {
                try
                {
                    CurrentTable.Rows[rowIndex][columnIndex] = short.Parse(value.Trim());
                    retval = true;
                }
                catch (Exception e)
                {
                    ErrorDialog.ShowDialog(e);
                }
            }

            return retval;
        }

        private bool PasteMatchesSelection(string clipboardData)
        {
            // Build a blank clipboard copy from selected cells to compare to clipboardData
            string testSelection = "";
            for (int i = 0; i < currentTable.Rows.Count; i++)
            {
                bool writeEndofLine = false;
                int minColumnIndex = dbDataGrid.SelectedCells.Min(n => n.Column.DisplayIndex);
                int maxColumnIndex = dbDataGrid.SelectedCells.Max(n => n.Column.DisplayIndex);
                
                foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells.Where(n => dbDataGrid.Items.IndexOf(n.Item) == i))
                {
                    for (int j = minColumnIndex; j < maxColumnIndex; j++)
                    {
                        testSelection += "\t";
                    }
                    writeEndofLine = true;
                    break;
                }

                if (writeEndofLine)
                {
                    testSelection += "\r\n";
                }
            }

            // If the number of lines don't match return false
            if (testSelection.Count(n => n == '\n') != clipboardData.Count(n => n == '\n'))
            {
                return false;
            }

            // If the number of lines match, test each line for the same number of 'cells'
            foreach (string line in clipboardData.Split('\n'))
            {
                if (testSelection.Count(n => n == '\t') != clipboardData.Count(n => n == '\t'))
                {
                    return false;
                }
            }

            return true;
        }

        private void PasteError(string error)
        {
            MessageBox.Show(error, "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

		private void BuiltTablesSetReadOnly(bool tablesreadonly)
        {
            foreach (DataTable table in loadedDataSet.Tables)
            {
                foreach (DataColumn column in table.Columns)
                {
                    column.ReadOnly = tablesreadonly;
                }
            }
        }

        #endregion
    }

    public static class Extensions
    {
        public static List<object> GetItemArray(this DataColumn column)
        {
            DataTable table = column.Table;
            List<object> Items = new List<object>();

            foreach (DataRow row in table.Rows)
            {
                Items.Add(row[column]);
            }

            Items.Sort();

            return Items;
        }
    }
}
