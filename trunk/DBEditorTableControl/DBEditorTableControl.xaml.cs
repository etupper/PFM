﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static void DbEditorCommitTables()
        {
            // Make sure the editor is actually loaded.
            if (PackedFileEditorRegistry.Editors.OfType<DBEditorTableHost>().Count() == 1)
            {
                (PackedFileEditorRegistry.Editors.OfType<DBEditorTableHost>().First().Child as DBEditorTableControl).PFMSaving();
            }
        }

        public static void DbEditorClearCache()
        {
            // Make sure the editor is actually loaded.
            if (PackedFileEditorRegistry.Editors.OfType<DBEditorTableHost>().Count() == 1)
            {
                (PackedFileEditorRegistry.Editors.OfType<DBEditorTableHost>().First().Child as DBEditorTableControl).ClearTableCache();
            }
        }

        public void PFMSaving()
        {
            // This method is invoked when the user tries to save a pack file, committing changes to all tables currently loaded.
            foreach (DataTable table in loadedDataSet.Tables)
            {
                table.AcceptChanges();
            }
            currentTable.AcceptChanges();

            UpdateConfig();
        }

        public void ClearTableCache()
        {
            // Clears the data cache, for when a user opens a new pack file.
            loadedDataSet.Tables.Clear();

            UpdateConfig();
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
                CurrentTable.ColumnChanged -= new DataColumnChangeEventHandler(CurrentTable_ColumnChanged);
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

                // Re-enable export control if it was disabled
                exportAsButton.IsEnabled = true;

                // Load table filters from saved data.
                LoadFilters();

                // Make sure the control knows it's table has changed.
                NotifyPropertyChanged(this, "CurrentTable");

                dbDataGrid.ItemsSource = CurrentTable.DefaultView;
            }
        }

        bool moveAndFreezeKeys;
        public bool MoveAndFreezeKeys
        {
            get { return moveAndFreezeKeys; }
            set
            {
                moveAndFreezeKeys = value;
                NotifyPropertyChanged(this, "MoveAndFreezeKeys");
                UpdateConfig();

                if (editedFile != null)
                {
                    if (moveAndFreezeKeys)
                    {
                        FreezeKeys();
                    }
                    else
                    {
                        UnfreezeKeys();
                    }
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

                if (findButton.IsEnabled)
                {
                    replaceButton.IsEnabled = !readOnly;
                }

                BuiltTablesSetReadOnly(readOnly);
            }
        }

        bool showFilters;
        public bool ShowFilters 
        { 
            get { return showFilters; } 
            set 
            { 
                showFilters = value;
                NotifyPropertyChanged(this, "ShowFilters");
                UpdateConfig();

                if (showFilters)
                {
                    filterDockPanel.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    filterDockPanel.Visibility = System.Windows.Visibility.Collapsed;
                }
            } 
        }

        ObservableCollection<DBFilter> filterList;

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
        private List<Visibility> visibleRows;

        FindAndReplaceWindow findReplaceWindow;

        public DBEditorTableControl()
        {
            InitializeComponent();

            // Attempt to load configuration settings, loading default values if config file doesn't exist.
            savedconfig = new DBTableEditorConfig();
            savedconfig.Load();

            // Instantiate default datatable, and others.
            currentTable = new DataTable();
            loadedDataSet = new DataSet("Loaded Tables");
            loadedDataSet.EnforceConstraints = false;
            hiddenColumns = new List<string>();
            visibleRows = new List<System.Windows.Visibility>();
            filterList = new ObservableCollection<DBFilter>();
            filterListView.ItemsSource = filterList;

            // Transfer saved settings.
            moveAndFreezeKeys = savedconfig.FreezeKeyColumns;
            useComboBoxes = savedconfig.UseComboBoxes;
            showAllColumns = savedconfig.ShowAllColumns;
            importDirectory = savedconfig.ImportDirectory;
            exportDirectory = savedconfig.ExportDirectory;
            ShowFilters = savedconfig.ShowFilters;

            // Set Initial checked status.
            moveAndFreezeKeysCheckBox.IsChecked = moveAndFreezeKeys;
            useComboBoxesCheckBox.IsChecked = useComboBoxes;
            showAllColumnsCheckBox.IsChecked = showAllColumns;

            // Register for Datatable events
            CurrentTable.ColumnChanged += new DataColumnChangeEventHandler(CurrentTable_ColumnChanged);
            CurrentTable.RowDeleting += new DataRowChangeEventHandler(CurrentTable_RowDeleting);
            CurrentTable.RowDeleted += new DataRowChangeEventHandler(CurrentTable_RowDeleted);
            CurrentTable.TableNewRow += new DataTableNewRowEventHandler(CurrentTable_TableNewRow);

            // Register for FindAndReplaceWindowEvents
            findReplaceWindow = new FindAndReplaceWindow();
            findReplaceWindow.FindNext += new EventHandler(findWindow_FindNext);
            findReplaceWindow.FindAll += new EventHandler(findReplaceWindow_FindAll);
            findReplaceWindow.Replace += new EventHandler(replaceWindow_Replace);
            findReplaceWindow.ReplaceAll += new EventHandler(replaceWindow_ReplaceAll);

            // Enable keyboard interop for the findReplaceWindow, otherwise WinForms will intercept all keyboard input.
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(findReplaceWindow);

            // Default the below buttons to false for all tables.
            cloneRowButton.IsEnabled = false;
            findButton.IsEnabled = false;
            replaceButton.IsEnabled = false; ;

            // Route the Paste event here so we can do it ourselves.
            CommandManager.RegisterClassCommandBinding(typeof(DataGrid), 
                new CommandBinding(ApplicationCommands.Paste, 
                    new ExecutedRoutedEventHandler(OnExecutedPaste), 
                    new CanExecuteRoutedEventHandler(OnCanExecutePaste)));
            
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
                    // Since the data in CurrentTable isn't actually bound to the packed file we are editing
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

            // Finally, based on whether we are moving and freezing columns, rearrange their order.
            if (editedFile != null)
            {
                if (moveAndFreezeKeys)
                {
                    FreezeKeys();
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
                // Update the visible rows for the cached table.
                visibleRows.Clear();
                for (int i = 0; i < loadedDataSet.Tables[currentPackedFile.Name].Rows.Count; i++)
                {
                    visibleRows.Add(System.Windows.Visibility.Visible);
                }

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

            // Finally, generate the visibleRows list based on total number of rows in the new table.
            visibleRows.Clear();
            for (int i = 0; i < constructionTable.Rows.Count; i++)
            {
                visibleRows.Add(System.Windows.Visibility.Visible);
            }

            return constructionTable;
        }

        public void LoadFilters()
        {
            filterList.Clear();

            // If the saved config has not filters, skip.
            if (!savedconfig.Filters.ContainsKey(editedFile.CurrentType.Name))
            {
                return;
            }

            // Load saved filters, attaching activation listeners for each one.
            foreach (DBFilter filter in savedconfig.Filters[editedFile.CurrentType.Name])
            {
                // Always load filters as inactive.
                filter.IsActive = false;
                filter.FilterToggled += new EventHandler(filter_FilterToggled);
                filterList.Add(filter);
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

        #region UserControl Events

        private void dbeControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Set Ctrl-B as testing key;
            if (e.Key == Key.B && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                // Test if the visual tree is based off of DataGridColumn.DisplayIndex, or its actual index.
                DataGridCell cell = GetCell(0, 0);
            }
            else if (e.Key == Key.Z && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                string clipboardtext = GetClipboardText();

                clipboardtext = clipboardtext.Replace("\t", "\\t");
                clipboardtext = clipboardtext.Replace("\r", "\\r");
                clipboardtext = clipboardtext.Replace("\n", "\\n");

                MessageBox.Show(clipboardtext);
            }
            else if (e.Key == Key.X && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                DataGridCell cell = GetCell(0, 0);
                string outputstring = "";

                for (int i = 0; i < dbDataGrid.Items.Count; i++)
                {
                    if (!(dbDataGrid.Items[i] is DataRowView))
                    {
                        continue;
                    }

                    int datarow = currentTable.Rows.IndexOf((dbDataGrid.Items[i] as DataRowView).Row);
                    outputstring = outputstring + String.Format("Visual row {0}, stored at CurrentTable[{1}].\n\r", i, datarow);
                }

                MessageBox.Show(outputstring);
            }
        }

        private void TableControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UpdateConfig();
        }

        #endregion

        #region Toolbar Events
        private void AddRowButton_Clicked(object sender, RoutedEventArgs e)
        {
            DataRow row = CurrentTable.NewRow();
            CurrentTable.Rows.Add(row);

            dataChanged = true;
            SendDataChanged();
        }

        private void CloneRowButton_Clicked(object sender, RoutedEventArgs e)
        {
            // Only do anything if atleast 1 row is selected.
            if (dbDataGrid.SelectedItems.Count > 0)
            {
                foreach (DataRowView rowview in dbDataGrid.SelectedItems.OfType<DataRowView>())
                {
                    DataRow row = CurrentTable.NewRow();
                    row.ItemArray = rowview.Row.ItemArray.ToArray();
                    CurrentTable.Rows.Add(row);
                }
            }

            dataChanged = true;
            SendDataChanged();
        }

        private void findButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SendKeys.Send("^f");
        }

        private void replaceButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SendKeys.Send("^h");
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

            // Set the find and replace button IsEnabled, once user clicks on grid.
            if (!findButton.IsEnabled)
            {
                findButton.IsEnabled = true;
                replaceButton.IsEnabled = !readOnly;
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

            if (!(e.Row.Item is DataRowView))
            {
                e.Row.Header = "*";
            }
            else
            {
                int datarowindex = CurrentTable.Rows.IndexOf((e.Row.Item as DataRowView).Row);
                e.Row.Header = datarowindex + 1;

                // Additional error checking on the visibleRows internal list.
                if (datarowindex >= visibleRows.Count)
                {
                    UpdateVisibleRows();
                }
                e.Row.Visibility = visibleRows[datarowindex];
            }
        }

        private void dbDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Look for Ctrl-F, for Find shortcut.
            if (e.Key == Key.F && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                if (dbDataGrid.SelectedCells.Count == 1 && dbDataGrid.SelectedCells.First().Item is DataRowView)
                {
                    findReplaceWindow.UpdateFindText(currentTable.Rows[dbDataGrid.Items.IndexOf(dbDataGrid.SelectedCells.First().Item)]
                                                                      [dbDataGrid.SelectedCells.First().Column.Header.ToString()].ToString());
                }

                findReplaceWindow.CurrentMode = FindAndReplaceWindow.FindReplaceMode.FindMode;
                findReplaceWindow.ReadOnly = readOnly;
                findReplaceWindow.Show();
            }

            // Look for Ctrl-H, for Replace shortcut.
            if (e.Key == Key.H && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && !readOnly)
            {
                findReplaceWindow.CurrentMode = FindAndReplaceWindow.FindReplaceMode.ReplaceMode;
                findReplaceWindow.Show();
            }

            // Look for F3, shortcut for Find Next.
            if (e.Key == Key.F3)
            {
                FindNext(findReplaceWindow.FindValue);
            }

            // Look for Insert key press, and check if a row is selected.
            if (!readOnly && e.Key == Key.Insert && dbDataGrid.SelectedItems.Count > 0)
            {
                DataRow newrow = CurrentTable.NewRow();

                if (dbDataGrid.SelectedItems.OfType<DataRowView>().Count() > 0)
                {
                    int datarowindex = -1;
                    int visualrowindex = -1;

                    // First, find the lowest visual row index.
                    foreach (DataRowView rowview in dbDataGrid.SelectedItems.OfType<DataRowView>())
                    {
                        if (visualrowindex == -1)
                        {
                            visualrowindex = dbDataGrid.Items.IndexOf(rowview);
                            datarowindex = currentTable.Rows.IndexOf(rowview.Row);
                            continue;
                        }

                        if (visualrowindex > dbDataGrid.Items.IndexOf(rowview))
                        {
                            visualrowindex = dbDataGrid.Items.IndexOf(rowview);
                            datarowindex = currentTable.Rows.IndexOf(rowview.Row);
                        }
                    }

                    // Now that we have the lowest selected row index, and it's corresponding location in our data source, we can insert.
                    CurrentTable.Rows.InsertAt(newrow, datarowindex);
                    UpdateVisibleRows();
                    dbDataGrid.Items.Refresh();
                }
                else if (dbDataGrid.SelectedItems.Count == 1 && !(dbDataGrid.SelectedItems[0] is DataRowView))
                {
                    // We should only hit this code if the user is attempting to insert rows with only the blank row selected
                    // in this case we want to simply add the rows on to the end of the table, no need to refresh either.
                    CurrentTable.Rows.Add(newrow);
                    UpdateVisibleRows();
                }
            }
        }

        private void dbDataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            if (e.Item is DataRowView)
            {
                // Clear copy data if row is collapsed from filtering.
                int datarowindex = currentTable.Rows.IndexOf((e.Item as DataRowView).Row);
                if (datarowindex >= visibleRows.Count)
                {
                    UpdateVisibleRows();
                }

                if (visibleRows[datarowindex] == System.Windows.Visibility.Collapsed)
                {
                    e.ClipboardRowContent.Clear();
                }
            }
        }

        #endregion

        #region Find and Replace

        private void findWindow_FindNext(object sender, EventArgs e)
        {
            FindNext(findReplaceWindow.FindValue);
        }

        private void findReplaceWindow_FindAll(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void replaceWindow_Replace(object sender, EventArgs e)
        {
            // If nothing is selected, then find something to replace first.
            if (dbDataGrid.SelectedCells.Count == 0)
            {
                // If we fail to find a match, return.
                if (!FindNext(findReplaceWindow.FindValue))
                {
                    return;
                }
            }

            // If nothing is STILL selected, then we found nothing to replace.
            // Or, if more than 1 cell is selected, we have a problem.
            if (dbDataGrid.SelectedCells.Count == 0 || dbDataGrid.SelectedCells.Count > 1)
            {
                return;
            }

            int rowindex = dbDataGrid.Items.IndexOf(dbDataGrid.SelectedCells.First().Item);
            int colindex = dbDataGrid.Columns.IndexOf(dbDataGrid.SelectedCells.First().Column);

            while (findReplaceWindow.ReplaceValue.Equals(currentTable.Rows[rowindex][colindex].ToString()))
            {
                // If what is selected has already been replaced, move on to the next match, returning if we fail.
                if (!FindNext(findReplaceWindow.FindValue))
                {
                    return;
                }

                // Update current coordinates.
                rowindex = dbDataGrid.Items.IndexOf(dbDataGrid.SelectedCells.First().Item);
                colindex = dbDataGrid.Columns.IndexOf(dbDataGrid.SelectedCells.First().Column);
            }
            
            if (findReplaceWindow.FindValue.Equals(currentTable.Rows[rowindex][colindex].ToString()))
            {
                // Test for a combobox comlumn.
                if (dbDataGrid.Columns[colindex] is DataGridComboBoxColumn)
                {
                    if (ComboBoxColumnContainsValue((DataGridComboBoxColumn)dbDataGrid.Columns[colindex], findReplaceWindow.ReplaceValue))
                    {
                        // The value in the Replace field is not valid for this column, alert user and return.
                        MessageBox.Show(String.Format("The value '{0}', is not a valid value for Column '{1}'", 
                                                      findReplaceWindow.ReplaceValue, 
                                                      dbDataGrid.Columns[colindex].Header.ToString()));

                        return;
                    }
                }

                // Assign the value, and update the UI
                currentTable.Rows[rowindex][colindex] = findReplaceWindow.ReplaceValue;
                RefreshCell(rowindex, colindex);
            }
        }

        private void replaceWindow_ReplaceAll(object sender, EventArgs e)
        {
            // Clear selection, so that FindNext() starts at the beginning of the table.
            dbDataGrid.SelectedCells.Clear();

            int rowindex;
            int colindex;

            while (FindNext(findReplaceWindow.FindValue))
            {
                // Update current coordinates.
                rowindex = dbDataGrid.Items.IndexOf(dbDataGrid.SelectedCells.First().Item);
                colindex = dbDataGrid.Columns.IndexOf(dbDataGrid.SelectedCells.First().Column);

                if (dbDataGrid.Columns[colindex] is DataGridComboBoxColumn)
                {
                    if (ComboBoxColumnContainsValue((DataGridComboBoxColumn)dbDataGrid.Columns[colindex], findReplaceWindow.ReplaceValue))
                    {
                        // The value in the Replace field is not valid for this column, alert user and continue.
                        MessageBox.Show(String.Format("The value '{0}', is not a valid value for Column '{1}'",
                                                      findReplaceWindow.ReplaceValue,
                                                      dbDataGrid.Columns[colindex].Header.ToString()));
                        continue;
                    }
                }

                // Assign the value, and update the UI
                currentTable.Rows[rowindex][colindex] = findReplaceWindow.ReplaceValue;
                RefreshCell(rowindex, colindex);
            }
        }

        private bool FindNext(string findthis)
        {
            if (String.IsNullOrEmpty(findthis))
            {
                MessageBox.Show("Nothing entered in Find bar!");
                return false;
            }

            // Set starting point at table upper left.
            int rowstartindex = 0;
            int colstartindex = 0;

            // If the user has a single cell selected, assume this as starting point.
            if (dbDataGrid.SelectedCells.Count == 1)
            {
                rowstartindex = currentTable.Rows.IndexOf((dbDataGrid.SelectedCells.First().Item as DataRowView).Row);
                colstartindex = currentTable.Columns.IndexOf(dbDataGrid.SelectedCells.First().Column.Header.ToString());
            }

            bool foundmatch = false;
            bool atstart = true;
            for (int i = rowstartindex; i < dbDataGrid.Items.Count; i++)
            {
                // Additional error checking on the visibleRows internal list.
                if (i >= visibleRows.Count)
                {
                    UpdateVisibleRows();
                }

                // Ignore the blank row, and any collapsed (filtered) rows.
                if (!(dbDataGrid.Items[i] is DataRowView) || visibleRows[i] == System.Windows.Visibility.Collapsed)
                {
                    continue;
                }

                for (int j = 0; j < dbDataGrid.Columns.Count; j++)
                {
                    if (atstart)
                    {
                        j = colstartindex;
                        atstart = false;
                    }

                    // Skip current cell.
                    if (i == rowstartindex && j == colstartindex)
                    {
                        continue;
                    }

                    foundmatch = DBUtil.isMatch(currentTable.Rows[i][j].ToString(), findthis);

                    if (foundmatch)
                    {
                        // Clears current selection for new selection.
                        dbDataGrid.SelectedCells.Clear();
                        SelectCell(i, j, true);
                        break;
                    }
                }

                if (foundmatch)
                {
                    break;
                }
            }

            if (!foundmatch)
            {
                MessageBox.Show("No More Matches Found.");
            }

            return foundmatch;
        }

        #endregion

        #region Paste Code

        protected virtual void OnExecutedPaste(object sender, ExecutedRoutedEventArgs args)
        {
            string clipboarddata = GetClipboardText();
            int rowindex;
            int datarowindex;
            int columnindex;
            int datacolumnindex;

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
                    // Get both visible and data row index
                    rowindex = dbDataGrid.Items.IndexOf(cellinfo.Item);
                    datarowindex = currentTable.Rows.IndexOf((cellinfo.Item as DataRowView).Row);

                    // Additional error checking for the visibleRows internal list.
                    if (datarowindex >= visibleRows.Count)
                    {
                        UpdateVisibleRows();
                    }

                    // Selecting cells while the table is filtered will select collapsed rows, so skip them here.
                    if (visibleRows[datarowindex] == System.Windows.Visibility.Collapsed)
                    {
                        continue;
                    }

                    // Get both visible and data column index
                    columnindex = dbDataGrid.Columns.IndexOf(cellinfo.Column);
                    datacolumnindex = currentTable.Columns.IndexOf(cellinfo.Column.Header.ToString());

                    CurrentTable.Rows[datarowindex].BeginEdit();

                    if (!TryPasteValue(datarowindex, datacolumnindex, pastevalue))
                    {
                        // Paste Error
                    }

                    CurrentTable.Rows[datarowindex].EndEdit();
                    RefreshCell(rowindex, columnindex);
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
                int basecolumnindex = -1;
                rowindex = -1;
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
                    // Get both visible and data row index
                    rowindex = dbDataGrid.Items.IndexOf(dbDataGrid.SelectedCells[0].Item);
                    basecolumnindex = dbDataGrid.SelectedCells[0].Column.DisplayIndex;

                    // Determine upper left corner of selection
                    foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells)
                    {
                        rowindex = Math.Min(rowindex, dbDataGrid.Items.IndexOf(cellinfo.Item));
                        basecolumnindex = Math.Min(basecolumnindex, cellinfo.Column.DisplayIndex);
                    }
                }
                else if (dbDataGrid.SelectedCells.Count == 1)
                {
                    // User has 1 cell selected, assume it is the top left corner and attempt to paste.
                    rowindex = dbDataGrid.Items.IndexOf(dbDataGrid.SelectedCells[0].Item);
                    basecolumnindex = dbDataGrid.SelectedCells[0].Column.DisplayIndex;
                }

                List<string> pasteinstructions = new List<string>();

                foreach (string line in clipboarddata.Split('\n'))
                {
                    columnindex = basecolumnindex;

                    if (rowindex > CurrentTable.Rows.Count - 1 || columnindex == -1)
                    {
                        if (IsLineARow(line))
                        {
                            // We have a full row, but no where to paste it, so add it as a new row.
                            DataRow newrow = currentTable.NewRow();

                            if (moveAndFreezeKeys)
                            {
                                // Data is being displayed with keys moved, so assume the clipboard data matches the visual appearance and not
                                // the order of the data source.
                                object tempitem;
                                List<object> itemarray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToList<object>();
                                for (int i = currentTable.PrimaryKey.Count() - 1; i >= 0; i--)
                                {
                                    tempitem = itemarray[i];
                                    itemarray.RemoveAt(i);
                                    itemarray.Insert(currentTable.Columns.IndexOf(currentTable.PrimaryKey[i]), tempitem);
                                }

                                // Once we have reordered the clipboard data to match the data source, convert to an object[]
                                newrow.ItemArray = itemarray.ToArray();
                            }
                            else
                            {
                                // Data is displayed as it is stored, so assume the clipboard data is ordered the same way.
                                newrow.ItemArray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToArray<object>();
                            }
                            
                            currentTable.Rows.Add(newrow);
                        }

                        rowindex++;
                        continue;
                    }

                    if (String.IsNullOrEmpty(line.Trim()))
                    {
                        rowindex++;
                        continue;
                    }

                    // Convert visual row and column index to data row and column index.
                    datarowindex = currentTable.Rows.IndexOf((dbDataGrid.Items[rowindex] as DataRowView).Row);

                    // Additional error checking for the visibleRows internal list.
                    if (datarowindex >= visibleRows.Count)
                    {
                        UpdateVisibleRows();
                    }

                    // Skip past any collapsed (filtered) rows.
                    while (visibleRows[datarowindex] == System.Windows.Visibility.Collapsed)
                    {
                        rowindex++;
                        datarowindex = currentTable.Rows.IndexOf((dbDataGrid.Items[rowindex] as DataRowView).Row);

                        if (rowindex >= currentTable.Rows.Count)
                        {
                            break;
                        }
                    }

                    foreach (string cell in line.Replace("\r", "").Split('\t'))
                    {
                        if (columnindex > CurrentTable.Columns.Count - 1)
                        {
                            break;
                        }

                        if (String.IsNullOrEmpty(cell.Trim()))
                        {
                            columnindex++;
                            continue;
                        }

                        // Convert visual column index, the display index not its location in the datagrid's collection, to data column index.
                        datacolumnindex = currentTable.Columns.IndexOf(dbDataGrid.Columns.Single(n => n.DisplayIndex == columnindex).Header.ToString());

                        // Since refresh works on the visual tree, and the visual tree is not affected by DisplayIndex, find the columns location
                        // in the datagrid's column collection to pass on.
                        int refreshcolumnindex = dbDataGrid.Columns.IndexOf(dbDataGrid.Columns.Single(n => n.DisplayIndex == columnindex));

                        // Rather than attempting to modify cells as we go, we should modify them in batches
                        // since any kind of sorting may interfere with paste order in real time.
                        pasteinstructions.Add(String.Format("{0};{1};{2};{3};{4}", datarowindex, rowindex, datacolumnindex, refreshcolumnindex, cell));

                        columnindex++;
                    }

                    rowindex++;
                }

                // Now that we have a list of paste instructions, execute them simultaneously across the data source
                // to avoid interference from any visual resorting.
                // Instruction Format: Data Row index;Visual Row index;Data Column index;Visual Column index;value
                foreach (string instruction in pasteinstructions)
                {
                    // Parse out the instructions.
                    string[] parameters = instruction.Split(';');
                    datarowindex = int.Parse(parameters[0]);
                    rowindex = int.Parse(parameters[1]);
                    datacolumnindex = int.Parse(parameters[2]);
                    columnindex = int.Parse(parameters[3]);

                    // Edit currentTable
                    currentTable.Rows[datarowindex].BeginEdit();
                    currentTable.Rows[datarowindex][datacolumnindex] = parameters[4];
                    currentTable.Rows[datarowindex].EndEdit();

                    // Refresh the visual cell
                    RefreshCell(rowindex, columnindex);
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
                    List<int> dataindiciesToPaste = new List<int>();
                    int testindex;
                    foreach (DataRowView rowview in dbDataGrid.SelectedItems.OfType<DataRowView>())
                    {
                        testindex = currentTable.Rows.IndexOf(rowview.Row);

                        // Additional error checking for the visibleRows internal list.
                        if (testindex >= visibleRows.Count)
                        {
                            UpdateVisibleRows();
                        }

                        // Skip any collapsed (filtered) rows.
                        if (visibleRows[testindex] == System.Windows.Visibility.Visible)
                        {
                            indiciesToPaste.Add(dbDataGrid.Items.IndexOf(rowview));
                        }
                    }
                    indiciesToPaste.Sort();

                    // Now that we have the selected rows visual locations, we need to determine their locations in our data source.
                    foreach (int i in indiciesToPaste)
                    {
                        dataindiciesToPaste.Add(currentTable.Rows.IndexOf((dbDataGrid.Items[i] as DataRowView).Row));
                    }

                    // We now have a list of data indicies sorted in visual order.
                    int currentindex = 0;

                    foreach (string line in clipboarddata.Replace("\r", "").Split('\n'))
                    {
                        if (!IsLineARow(line) || String.IsNullOrEmpty(line) || !IsRowValid(line))
                        {
                            currentindex++;
                            continue;
                        }

                        if (currentindex >= dataindiciesToPaste.Count)
                        {
                            // Add new row
                            DataRow newrow = currentTable.NewRow(); 
                            if (moveAndFreezeKeys)
                            {
                                // Data is being displayed with keys moved, so assume the clipboard data matches the visual appearance and not
                                // the order of the data source.
                                object tempitem;
                                List<object> itemarray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToList<object>();
                                for (int i = currentTable.PrimaryKey.Count() - 1; i >= 0; i--)
                                {
                                    tempitem = itemarray[i];
                                    itemarray.RemoveAt(i);
                                    itemarray.Insert(currentTable.Columns.IndexOf(currentTable.PrimaryKey[i]), tempitem);
                                }

                                // Once we have reordered the clipboard data to match the data source, convert to an object[]
                                newrow.ItemArray = itemarray.ToArray();
                            }
                            else
                            {
                                // Data is displayed as it is stored, so assume the clipboard data is ordered the same way.
                                newrow.ItemArray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToArray<object>();
                            }

                            currentTable.Rows.Add(newrow);

                            currentindex++;
                            continue;
                        }

                        CurrentTable.Rows[dataindiciesToPaste[currentindex]].BeginEdit();
                        if (moveAndFreezeKeys)
                        {
                            // Data is being displayed with keys moved, so assume the clipboard data matches the visual appearance and not
                            // the order of the data source.
                            object tempitem;
                            List<object> itemarray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToList<object>();
                            for (int i = currentTable.PrimaryKey.Count() - 1; i >= 0; i--)
                            {
                                tempitem = itemarray[i];
                                itemarray.RemoveAt(i);
                                itemarray.Insert(currentTable.Columns.IndexOf(currentTable.PrimaryKey[i]), tempitem);
                            }

                            // Once we have reordered the clipboard data to match the data source, convert to an object[]
                            CurrentTable.Rows[dataindiciesToPaste[currentindex]].ItemArray = itemarray.ToArray();
                        }
                        else
                        {
                            // Data is displayed as it is stored, so assume the clipboard data is ordered the same way.
                            CurrentTable.Rows[dataindiciesToPaste[currentindex]].ItemArray = line.Split('\t').Take(editedFile.CurrentType.Fields.Count).ToArray<object>();
                        }

                        CurrentTable.Rows[dataindiciesToPaste[currentindex]].EndEdit();
                        currentindex++;
                    }

                    Refresh(true);
                }
                else
                {
                    // Please select rows.
                    MessageBox.Show("When pasting rows, please use the row header button to select entire rows only.", 
                                    "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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
                    object newvalue = currentTable.Compute(expression, "");
                    Type columntype = currentTable.Columns[(string)col.Header].DataType;

                    // For integer based columns, do a round first if necessary.
                    if (columntype.Name.Equals("Int32") || columntype.Name.Equals("Int16"))
                    {
                        int newintvalue;
                        if(!Int32.TryParse(newvalue.ToString(), out newintvalue))
                        {
                            double tempvalue = Double.Parse(newvalue.ToString());
                            tempvalue = Math.Round(tempvalue, 0);
                            newintvalue = (int)tempvalue;
                        }

                        newvalue = newintvalue;
                    }
                    currentTable.Rows[i][(string)col.Header] = newvalue;
                }
            }

            RefreshColumn(dbDataGrid.Columns.IndexOf(col));
        }

        private void RenumberMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the column index this context menu was called from.
            DataGridColumn col = (((sender as MenuItem).Parent as ContextMenu).PlacementTarget as DataGridColumnHeader).Column;
            List<int> visualroworder = new List<int>();

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
                        // Store the data row index associated with the current visual row to account for column sorting.
                        visualroworder.Add(currentTable.Rows.IndexOf((dbDataGrid.Items[i] as DataRowView).Row));
                    }

                    // Now that we have a set order, we can assign values.
                    for (int i = 0; i < visualroworder.Count; i++)
                    {
                        currentTable.Rows[visualroworder[i]][col.Header.ToString()] = parsedNumber + i;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Could not apply values: {0}", ex.Message), "You fail!");
                }
            }

            RefreshColumn(dbDataGrid.Columns.IndexOf(col));
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

        private void RowHeaderContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu)
            {
                bool insertrowsavailable = true;
                if ((sender as ContextMenu).DataContext is DataRowView)
                {
                    insertrowsavailable = true;
                }

                foreach (MenuItem item in (sender as ContextMenu).Items)
                {
                    string itemheader = item.Header.ToString();
                    if (itemheader.Equals("Insert Row") || itemheader.Equals("Insert Multiple Rows"))
                    {
                        item.IsEnabled = insertrowsavailable;
                    }
                }
            }
        }

        private void RowHeaderInsertRow_Click(object sender, RoutedEventArgs e)
        {
            if (!readOnly && sender is MenuItem)
            {
                // Double check that whant triggered the event is what we expect.
                if ((sender as MenuItem).DataContext is DataRowView)
                {
                    // Determine visual and data index of calling row.
                    int datarowindex = dbDataGrid.Items.IndexOf(((sender as MenuItem).DataContext as DataRowView));
                    int visualrowindex = currentTable.Rows.IndexOf(((sender as MenuItem).DataContext as DataRowView).Row);

                    // Now that we have the lowest selected row index, and it's corresponding location in our data source, we can insert.
                    DataRow newrow = CurrentTable.NewRow();
                    CurrentTable.Rows.InsertAt(newrow, datarowindex);

                    UpdateVisibleRows();
                    dbDataGrid.Items.Refresh();
                }
                else
                {
                    // We'll end up here if the user is attempting to insert a row in front of the blank row, so we will simply add a new row
                    // at the end of the table, still we'll use a try block in case something unforseen happens.
                    try
                    {
                        DataRow newrow = CurrentTable.NewRow();
                        CurrentTable.Rows.Add(newrow);

                        UpdateVisibleRows();
                    }
                    catch (Exception ex)
                    {
                        ErrorDialog.ShowDialog(ex);
                    }
                }
            }
        }

        private void RowHeaderInsertManyRows_Click(object sender, RoutedEventArgs e)
        {
            if (!readOnly && sender is MenuItem)
            {
                // Request how many rows should be inserted from the user.
                InputBox insertrowsInputBox = new InputBox();
                insertrowsInputBox.ShowDialog();

                // Double check that whant triggered the event is what we expect.
                if (insertrowsInputBox.DialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    // Determine how many rows the user wants to add.
                    int numrows = 0;
                    try
                    {
                        numrows = int.Parse(insertrowsInputBox.Input);
                    }
                    catch (Exception ex)
                    {
                        if (ex is FormatException)
                        {
                            MessageBox.Show(String.Format("Input: {0}, is not a valid number of rows, please enter a whole number.", insertrowsInputBox.Input));
                        }
                    }

                    for (int i = 0; i < numrows; i++)
                    {
                        DataRow newrow = CurrentTable.NewRow();
                        if ((sender as MenuItem).DataContext is DataRowView)
                        {
                            // Determine data index of calling row, and insert.
                            int datarowindex = dbDataGrid.Items.IndexOf(((sender as MenuItem).DataContext as DataRowView));
                            CurrentTable.Rows.InsertAt(newrow, datarowindex);
                        }
                        else
                        {
                            // If the blank row is calling us, add to the end of the table.
                            CurrentTable.Rows.Add(newrow);
                        }
                    }

                    UpdateVisibleRows();
                    dbDataGrid.Items.Refresh();
                }
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

            editedFile.Entries.Add(new DBRow(EditedFile.CurrentType, dbfileconstructionRow));
            visibleRows.Add(System.Windows.Visibility.Visible);

            dataChanged = true;
            SendDataChanged();
        }

        void CurrentTable_RowDeleting(object sender, DataRowChangeEventArgs e)
        {
            int removalindex = e.Row.Table.Rows.IndexOf(e.Row);
            editedFile.Entries.RemoveAt(removalindex);

            // Additional error checking for the visibleRows internal list.
            if (removalindex >= visibleRows.Count)
            {
                UpdateVisibleRows();
            }

            visibleRows.RemoveAt(removalindex);

            dataChanged = true;
            SendDataChanged();
        }

        void CurrentTable_RowDeleted(object sender, DataRowChangeEventArgs e)
        {
            if (e.Row.RowState != DataRowState.Detached)
            {
                // Remove the row, because otherwise there will be indexing issues due to how the DataTable class handles row deletion.
                currentTable.Rows.Remove(e.Row);
            }

            dataChanged = true;
            SendDataChanged();
        }

        void CurrentTable_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            // Set row index as either the last row in edited file if we are creating a new row.
            int rowIndex = e.Row.RowState == DataRowState.Detached ? EditedFile.Entries.Count - 1 : e.Row.Table.Rows.IndexOf(e.Row);
            int colIndex = e.Column.Ordinal;

            editedFile.Entries[rowIndex][colIndex].Value = e.ProposedValue.ToString();

            dataChanged = true;
            SendDataChanged();
        }

        void DataGridContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            bool cellsselected = false;

            if (dbDataGrid.SelectedCells.Count > 0)
            {
                cellsselected = true;
            }

            ContextMenu menu = (ContextMenu)sender;

            foreach (MenuItem item in menu.Items.OfType<MenuItem>())
            {
                if (item.Header.Equals("Copy"))
                {
                    item.IsEnabled = cellsselected;
                }
                else if (item.Header.Equals("Paste"))
                {
                    if (readOnly)
                    {
                        item.IsEnabled = false;
                    }
                    else
                    {
                        item.IsEnabled = cellsselected;
                    }
                }
                else if (item.Header.Equals("Apply Expression to Selected Cells"))
                {
                    item.IsEnabled = cellsselected;
                    if (cellsselected)
                    {
                        Type columntype;
                        foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells)
                        {
                            columntype = currentTable.Columns[(string)cellinfo.Column.Header].DataType;
                            if (readOnly || !(columntype.Name.Equals("Single") || columntype.Name.Equals("Int32") || columntype.Name.Equals("Int16")))
                            {
                                item.IsEnabled = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DataGridCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Programmatically send a copy shortcut key event.
            System.Windows.Forms.SendKeys.Send("^c");
        }

        private void DataGridPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Programmatically send a paste shortcut key event.
            System.Windows.Forms.SendKeys.Send("^v");
        }

        private void DataGridApplyExpressionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ApplyExpressionWindow getexpwindow = new ApplyExpressionWindow();
            getexpwindow.ShowDialog();

            if (getexpwindow.DialogResult != null && (bool)getexpwindow.DialogResult)
            {
                foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells)
                {
                    // Determine current cells indecies, row and column
                    int columnindex = currentTable.Columns.IndexOf(cellinfo.Column.Header.ToString());
                    int rowindex = currentTable.Rows.IndexOf((cellinfo.Item as DataRowView).Row);

                    // Get the expression, replacing x for the current cell's value.
                    string expression = getexpwindow.EnteredExpression.Replace("x", string.Format("{0}", currentTable.Rows[rowindex][columnindex]));

                    // Compute spits out the new value after the current value is applied to the expression given.
                    object newvalue = currentTable.Compute(expression, "");
                    Type columntype = currentTable.Columns[columnindex].DataType;

                    // For integer based columns, do a round first if necessary.
                    if (columntype.Name.Equals("Int32") || columntype.Name.Equals("Int16"))
                    {
                        int newintvalue;
                        if (!Int32.TryParse(newvalue.ToString(), out newintvalue))
                        {
                            double tempvalue = Double.Parse(newvalue.ToString());
                            tempvalue = Math.Round(tempvalue, 0);
                            newintvalue = (int)tempvalue;
                        }

                        newvalue = newintvalue;
                    }
                    currentTable.Rows[rowindex][columnindex] = newvalue;

                    // Refresh the cell in the UI, using its visual coordinates.
                    RefreshCell(dbDataGrid.Items.IndexOf(cellinfo.Item), dbDataGrid.Columns.IndexOf(cellinfo.Column));
                }
            }
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

        private void SendDataChanged()
        {
            // This method is used to trip the packedFile's data changed notification, so that the PFM tree list updates
            // when data is changed, instead of once a user navigates away.
            if (dataChanged)
            {
                currentPackedFile.Modified = true;
            }
        }

        #region UI Helper Methods

        public DataGridCell GetCell(int row, int column, bool onlyvisible = false)
        {
            DataGridRow rowContainer = GetRow(row, onlyvisible);

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

        public DataGridRow GetRow(int index, bool onlyvisible = false)
        {
            DataGridRow row = (DataGridRow)dbDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null && !onlyvisible)
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
            var jtest = LogicalTreeHelper.GetChildren(parent);
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

        private void SelectCell(int rowindex, int colindex, bool scrollview = false)
        {
            // Add the cell to the selected cells list.
            DataGridCellInfo cellinfo = new DataGridCellInfo(dbDataGrid.Items[rowindex], dbDataGrid.Columns[colindex]);
            if (!dbDataGrid.SelectedCells.Contains(cellinfo))
            {
                dbDataGrid.SelectedCells.Add(cellinfo);
            }

            // Scroll cell into view if asked.
            if (scrollview)
            {
                dbDataGrid.ScrollIntoView(dbDataGrid.Items[rowindex], dbDataGrid.Columns[colindex]);
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
            savedconfig.FreezeKeyColumns = moveAndFreezeKeys;
            savedconfig.UseComboBoxes = useComboBoxes;
            savedconfig.ShowAllColumns = showAllColumns;
            savedconfig.ImportDirectory = importDirectory;
            savedconfig.ExportDirectory = exportDirectory;
            savedconfig.ShowFilters = showFilters;

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

                if (savedconfig.Filters.ContainsKey(editedFile.CurrentType.Name))
                {
                    // Also overwrite the filter list.
                    savedconfig.Filters[editedFile.CurrentType.Name].Clear();
                    savedconfig.Filters[editedFile.CurrentType.Name].AddRange(filterList);
                }
                else
                {
                    // Create a new list for the table.
                    savedconfig.Filters.Add(new KeyValuePair<string, List<DBFilter>>(editedFile.CurrentType.Name, new List<DBFilter>(filterList)));
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
            string clipboardText = GetClipboardText();

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
            string clipboardText = GetClipboardText();

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
            return !GetClipboardText().Contains('\t') && GetClipboardText().Count(n => n == '\n') == 1;
        }

        private string GetClipboardText()
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    return Clipboard.GetText(TextDataFormat.Text);
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }

            return null;
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
                    if (e is FormatException)
                    {
                        MessageBox.Show(String.Format("Could not paste '{0}' in column '{1}', row {2}, this cell requires an float!",
                                                        value.ToString(), dbDataGrid.Columns[columnIndex].Header.ToString(), rowIndex));
                    }
                    else
                    {
                        ErrorDialog.ShowDialog(e);
                    }
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
                    if (e is FormatException)
                    {
                        MessageBox.Show(String.Format("Could not paste '{0}' in column '{1}', row {2}, this cell requires True/False!",
                                                        value.ToString(), dbDataGrid.Columns[columnIndex].Header.ToString(), rowIndex));
                    }
                    else
                    {
                        ErrorDialog.ShowDialog(e);
                    }
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
                    if (e is FormatException)
                    {
                        MessageBox.Show(String.Format("Could not paste '{0}' in column '{1}', row {2}, this cell requires an integer value!",
                                                        value.ToString(), dbDataGrid.Columns[columnIndex].Header.ToString(), rowIndex));
                    }
                    else
                    {
                        ErrorDialog.ShowDialog(e);
                    }
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
                    if (e is FormatException)
                    {
                        MessageBox.Show(String.Format("Could not paste '{0}' in column '{1}', row {2}, this cell requires an integer value!",
                                                        value.ToString(), dbDataGrid.Columns[columnIndex].Header.ToString(), rowIndex));
                    }
                    else
                    {
                        ErrorDialog.ShowDialog(e);
                    }
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
                // Additional error checking for the visibleRows internal list.
                if (i >= visibleRows.Count)
                {
                    UpdateVisibleRows();
                }

                // Ignore collapsed (filtered) rows when building our test string.
                if (visibleRows[i] == System.Windows.Visibility.Collapsed)
                {
                    continue;
                }

                bool writeEndofLine = false;
                int minColumnIndex = dbDataGrid.SelectedCells.Min(n => n.Column.DisplayIndex);
                int maxColumnIndex = dbDataGrid.SelectedCells.Max(n => n.Column.DisplayIndex);

                foreach (DataGridCellInfo cellinfo in dbDataGrid.SelectedCells.Where(n => currentTable.Rows.IndexOf((n.Item as DataRowView).Row) == i))
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

        private bool ComboBoxColumnContainsValue(DataGridComboBoxColumn column, string tocheck)
        {
            if (column.ItemsSource.OfType<object>().Count(n => n.ToString().Equals(tocheck)) != 0)
            {
                return true;
            }

            return false;
        }

        private bool IsRowValid(string line)
        {
            List<string> fields = line.Split('\t').ToList();
            List<int> fieldorder = new List<int>();

            if (fields.Count != currentTable.Columns.Count)
            {
                PasteError("Error: could not paste following row into table, mismatched number of fields.\n\n" + line);
                return false;
            }

            if (moveAndFreezeKeys)
            {
                // Fields may be in altered visual order from data source.
                fieldorder.AddRange(dbDataGrid.Columns.OrderBy(n => n.DisplayIndex).Select(n => dbDataGrid.Columns.IndexOf(n)));
            }
            else
            {
                // Fields are displayed in original data order.
                for(int i = 0; i < currentTable.Columns.Count; i++)
                {
                    fieldorder.Add(i);
                }
            }

            for (int i = 0; i < currentTable.Columns.Count; i++)
            {
                Type fieldtype = currentTable.Columns[fieldorder[i]].DataType;

                if (fieldtype.Name == "String")
                {
                    continue;
                }
                else if (fieldtype.Name == "Single")
                {
                    float temp;
                    if (!float.TryParse(fields[i], out temp))
                    {
                        PasteError(String.Format("Error: could not paste line into table, as '{0}' is not a valid value for '{1}'.\n\n{2}",
                                                    fields[i], currentTable.Columns[fieldorder[i]].ColumnName, line));
                        return false;
                    }
                }
                else if (fieldtype.Name == "Int32")
                {
                    int temp;
                    if (!int.TryParse(fields[i], out temp))
                    {
                        PasteError(String.Format("Error: could not paste line into table, as '{0}' is not a valid value for '{1}'.\n\n{2}",
                                                    fields[i], currentTable.Columns[fieldorder[i]].ColumnName, line));
                        return false;
                    }
                }
                else if (fieldtype.Name == "Int16")
                {
                    short temp;
                    if (!short.TryParse(fields[i], out temp))
                    {
                        PasteError(String.Format("Error: could not paste line into table, as '{0}' is not a valid value for '{1}'.\n\n{2}",
                                                    fields[i], currentTable.Columns[fieldorder[i]].ColumnName, line));
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Filter Methods

        private void manageFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (filterListView.Visibility == System.Windows.Visibility.Collapsed)
            {
                filterListView.Visibility = System.Windows.Visibility.Visible;
            }
            else if (filterListView.Visibility == System.Windows.Visibility.Visible)
            {
                filterListView.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void filterListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var test = sender;
            if (filterListView.SelectedItems.Count == 1)
            {
                // Test to enable/disable filter management buttons.
                DBFilter selecteditem = (DBFilter)filterListView.SelectedItem;
                deleteFilterButton.IsEnabled = true;
                editFilterButton.IsEnabled = true;
            }
            else
            {
                deleteFilterButton.IsEnabled = false;
                editFilterButton.IsEnabled = false;
            }
        }

        private void filterListView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && filterListView.SelectedItems.Count == 1)
            {
                // User double left clicked a filter, do an edit.
                ManageFiltersWindow filterWindow = new ManageFiltersWindow(filterList.Select(n => n.Name).ToList());
                System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(filterWindow);
                filterWindow.CurrentTable = currentTable;
                filterWindow.Filter = (DBFilter)filterListView.SelectedItem;
                filterWindow.ShowDialog();

                if (filterWindow.DialogResult.Value)
                {
                    DBFilter editedfilter = filterWindow.Filter;
                    // Save the edited filter data.
                    int index = filterList.IndexOf((DBFilter)filterListView.SelectedItem);
                    filterList[index].Name = editedfilter.Name;
                    filterList[index].ApplyToColumn = editedfilter.ApplyToColumn;
                    filterList[index].FilterValue = editedfilter.FilterValue;
                    filterList[index].MatchMode = editedfilter.MatchMode;

                    if (editedfilter.IsActive)
                    {
                        UpdateVisibleRows();
                        dbDataGrid.Items.Refresh();
                    }
                }
            }
        }

        private void filterListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && filterListView.SelectedItems.Count == 1)
            {
                // User hit the delete key on a filter, so delete it.
                DBFilter filter = (DBFilter)filterListView.SelectedItem;
                filterList.RemoveAt(filterList.IndexOf(filter));

                if (filter.IsActive)
                {
                    UpdateVisibleRows();
                    dbDataGrid.Items.Refresh();
                }
            }
            
        }

        private void filterListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem && e.LeftButton == MouseButtonState.Pressed)
            {
                // Drag drop code, TODO: differentiate between a drag and a click some how.
                ListViewItem draggedItem = sender as ListViewItem;
                //DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                //draggedItem.IsSelected = true;
            }
        }

        private void filterListView_Drop(object sender, DragEventArgs e)
        {
            DBFilter droppedData = e.Data.GetData(typeof(DBFilter)) as DBFilter;
            DBFilter target = ((ListBoxItem)(sender)).DataContext as DBFilter;

            // Ignore request until an actual drop is requested.
            if (droppedData == target)
            {
                return;
            }

            int removedIdx = filterListView.Items.IndexOf(droppedData);
            int targetIdx = filterListView.Items.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                filterList.Insert(targetIdx + 1, droppedData);
                filterList.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (filterList.Count + 1 > remIdx)
                {
                    filterList.Insert(targetIdx, droppedData);
                    filterList.RemoveAt(remIdx);
                }
            }
        }

        private void editFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ManageFiltersWindow filterWindow = new ManageFiltersWindow(filterList.Select(n => n.Name).ToList());
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(filterWindow);
            filterWindow.CurrentTable = currentTable;

            if (filterListView.SelectedItems.Count == 1)
            {
                filterWindow.Filter = (DBFilter)filterListView.SelectedItem;
            }

            filterWindow.ShowDialog();

            if (filterWindow.DialogResult.Value)
            {
                DBFilter editedfilter = filterWindow.Filter;
                // Save the edited filter data.
                int index = filterList.IndexOf((DBFilter)filterListView.SelectedItem);
                filterList[index].Name = editedfilter.Name;
                filterList[index].ApplyToColumn = editedfilter.ApplyToColumn;
                filterList[index].FilterValue = editedfilter.FilterValue;
                filterList[index].MatchMode = editedfilter.MatchMode;

                if (editedfilter.IsActive)
                {
                    UpdateVisibleRows();
                    dbDataGrid.Items.Refresh();
                }
            }
        }

        private void addFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ManageFiltersWindow filterWindow = new ManageFiltersWindow(filterList.Select(n => n.Name).ToList());
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(filterWindow);
            filterWindow.CurrentTable = currentTable;
            filterWindow.Filter = new DBFilter();
            filterWindow.ShowDialog();

            if (filterWindow.DialogResult.Value)
            {
                DBFilter filter = filterWindow.Filter;
                // Attach event handler for checked/unchecked toggle.
                filter.FilterToggled += new EventHandler(filter_FilterToggled);
                // Only add the filter if the name is unique.
                filterList.Add(filter);
            }
        }

        private void deleteFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (filterListView.SelectedItems.Count == 1)
            {
                DBFilter filter = (DBFilter)filterListView.SelectedItem;
                filterList.RemoveAt(filterList.IndexOf(filter));

                if (filter.IsActive)
                {
                    UpdateVisibleRows();
                    dbDataGrid.Items.Refresh();
                }
            }
        }

        private void filter_FilterToggled(object sender, EventArgs e)
        {
            UpdateVisibleRows();
            dbDataGrid.Items.Refresh();
        }

        private void UpdateVisibleRows()
        {
            for (int i = 0; i < currentTable.Rows.Count; i++)
            {
                if (i >= visibleRows.Count)
                {
                    // We have a problem. Append additional items until we are ok again.
                    while (i >= visibleRows.Count)
                    {
                        visibleRows.Add(System.Windows.Visibility.Visible);
                    }
                }

                if (TestRow(i))
                {
                    visibleRows[i] = System.Windows.Visibility.Visible;
                }
                else
                {
                    visibleRows[i] = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private bool TestRow(int rowindex)
        {
            int colindex;

            foreach (DBFilter filter in filterList)
            {
                if (!filter.IsActive)
                {
                    continue;
                }

                colindex = currentTable.Columns.IndexOf(filter.ApplyToColumn);

                // Match the value exactly.
                if (filter.MatchMode == MatchType.Exact)
                {
                    if (!currentTable.Rows[rowindex][colindex].ToString().Equals(filter.FilterValue))
                    {
                        return false;
                    }
                }// Check for partial match.
                else if (filter.MatchMode == MatchType.Partial)
                {
                    if (!currentTable.Rows[rowindex][colindex].ToString().Contains(filter.FilterValue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Frozen Key Column Methods

        private void FreezeKeys()
        {
            // If there are no keys columns specified, return.
            if (currentTable.PrimaryKey.Count() == 0)
            {
                return;
            }

            // Figure out which columns are key columns.
            List<string> keycolumns = new List<string>();
            keycolumns.AddRange(currentTable.PrimaryKey.Select(n => n.ColumnName));

            for (int i = 0; i < keycolumns.Count; i++)
            {
                // Set the display index of the columns to left most column, retaining key order.
                dbDataGrid.Columns.Single(n => keycolumns[i].Equals(n.Header.ToString())).DisplayIndex = i;
            }

            dbDataGrid.FrozenColumnCount = keycolumns.Count;
        }

        private void UnfreezeKeys()
        {
            // If there are no keys columns specified, return.
            if (currentTable.PrimaryKey.Count() == 0)
            {
                return;
            }

            // Figure out which columns are key columns.
            List<string> keycolumns = new List<string>();
            keycolumns.AddRange(currentTable.PrimaryKey.Select(n => n.ColumnName));

            for (int i = 0; i < keycolumns.Count; i++)
            {
                // Reset the display index of the key columns back to their original positions.
                dbDataGrid.Columns.Single(n => keycolumns[i].Equals(n.Header.ToString())).DisplayIndex = currentTable.Columns.IndexOf(keycolumns[i]);
            }

            dbDataGrid.FrozenColumnCount = 0;
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
