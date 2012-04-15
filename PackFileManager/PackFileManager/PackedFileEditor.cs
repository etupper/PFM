using System;
using System.IO;
using System.Windows.Forms;
using Common;

namespace PackFileManager {
    public class PackedFileEditor<T> : UserControl {
        Codec<T> codec;
        protected virtual T EditedFile { get; set; }
        PackedFile currentPacked;

        public virtual bool DataChanged {
            get;
            set;
        }

        protected PackedFileEditor(Codec<T> c) {
            codec = c;
        }

        public PackedFile CurrentPackedFile {
            set {
                if (value != null) {
                    byte[]data = value.Data;
                    using (MemoryStream stream = new MemoryStream(data, 0, data.Length)) {
                        EditedFile = codec.Decode(stream);
                    }
                } else {
                    EditedFile = default(T);
                }
                currentPacked = value;
            }
            get {
                return currentPacked;
            }
        }

        // interface method to save to pack if data has changed
        public void Commit() {
            if (DataChanged) {
                SetData();
                DataChanged = false;
            }
        }

        // implementation method to actually save data
        protected void SetData() {
            using (MemoryStream stream = new MemoryStream()) {
                codec.Encode(stream, EditedFile);
                CurrentPackedFile.Data = stream.ToArray();
            }
        }
    }
}
