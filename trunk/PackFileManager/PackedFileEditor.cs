using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Common;
using Filetypes;

namespace PackFileManager {
    public interface IPackedFileEditor {
        PackedFile CurrentPackedFile {
            get; set;
        }
        bool CanEdit(PackedFile file);
        void Commit();
    }
    
    public abstract class PackedFileEditor<T> : UserControl, IPackedFileEditor {
        protected readonly Codec<T> codec;
        public virtual T EditedFile { get; set; }
        PackedFile currentPacked;

        protected virtual bool DataChanged {
            get;
            set;
        }

        protected PackedFileEditor(Codec<T> c) {
            codec = c;
        }
        
        // interface method to give the editor something to edit
        public virtual PackedFile CurrentPackedFile {
            set {
                if (currentPacked != null && DataChanged) {
                    Commit();
                }
                if (value != null) {
                    byte[]data = value.Data;
                    using (MemoryStream stream = new MemoryStream(data, 0, data.Length)) {
                        EditedFile = codec.Decode(stream);
                    }
                } else {
                    EditedFile = default(T);
                }
                DataChanged = false;
                currentPacked = value;
            }
            get {
                return currentPacked;
            }
        }

        // interface to query if given file can be edited
        public abstract bool CanEdit(PackedFile file);

        // interface method to save to pack if data has changed in this editor
        public void Commit() {
            if (DataChanged) {
                SetData();
                DataChanged = false;
            }
        }

        // implementation method to actually save data
        protected virtual void SetData() {
            using (MemoryStream stream = new MemoryStream()) {
                codec.Encode(stream, EditedFile);
                CurrentPackedFile.Data = stream.ToArray();
            }
        }
        
        // utility method for tsv export
        public static void WriteToTSVFile(List<string> strings) {
            SaveFileDialog dialog = new SaveFileDialog {
                Filter = IOFunctions.TSV_FILTER
            };
            if (dialog.ShowDialog() == DialogResult.OK) {
                using (StreamWriter writer = new StreamWriter(dialog.FileName)) {
                    foreach (string str in strings) {
                        writer.WriteLine(str);
                    }
                }
            }
        }

        public static bool HasExtension(PackedFile file, string[] extensions) {
            bool result = false;
            if (file != null) {
                foreach (string ext in extensions) {
                    if (Path.GetExtension(file.FullPath).Equals(ext)) {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
