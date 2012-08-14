using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using Common;
using Filetypes;

namespace PackFileManager {
    public partial class BuildingModelEditor : UserControl, IPackedFileEditor {
        public BuildingModelEditor() {
            InitializeComponent();

            modelGridView.SelectionChanged += new EventHandler(SetEntrySource);
            entryGridView.SelectionChanged += new EventHandler(SetCoordinates);
        }

        public bool CanEdit(PackedFile file) {
            return DBFile.typename(file.FullPath).Equals("models_building_tables");
        }

        private PackedFile packedFile;
        public PackedFile CurrentPackedFile {
            get {
                return packedFile;
            }
            set {
                EditedFile = BuildingModelCodec.Instance.Decode(new MemoryStream(value.Data));
                packedFile = value;
            }
        }

        public void Commit() {
        }

        ModelFile<BuildingModel> file;
        public ModelFile<BuildingModel> EditedFile {
            get {
                return file;
            }
            set {
                file = value;
                modelSource.DataSource = file.Models;
            }
        }

        private void SetEntrySource(object o, EventArgs args) {
            int index = -1;
            if (EditedFile != null) {
                index = SelectedRowIndex(modelGridView);
            }
            if (index != -1) {
                entrySource.DataSource = EditedFile.Models[index].Entries;
            } else {
                entrySource.DataSource = new List<BuildingModel>();
            }
        }

        private void SetCoordinates(object o, EventArgs args) {
            int index = SelectedRowIndex(entryGridView);
            if (index != -1) {
                BuildingModelEntry entry = ((List<BuildingModelEntry>)entrySource.DataSource)[index];
                angle1Source.DataSource = new Coordinates[] { entry.Coordinates1 };
                angle2Source.DataSource = new Coordinates[] { entry.Coordinates2 };
                angle3Source.DataSource = new Coordinates[] { entry.Coordinates3 };
            } else {
                angle1Source.DataSource = angle2Source.DataSource = angle3Source.DataSource = new List<Coordinates>();
            }
        }

        private int SelectedRowIndex(DataGridView gridView) {
            int index = -1;
            if (gridView.SelectedRows.Count > 0) {
                index = gridView.SelectedRows[0].Index;
            } else if (gridView.SelectedCells.Count > 0) {
                index = gridView.SelectedCells[0].RowIndex;
            }
            return index;
        }
    }
}
