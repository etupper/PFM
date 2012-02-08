using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DecodeTool {

    public delegate void ValueChanged();

    class Decoder {
        byte[] bytes = {};
        List<TypeDescription> descriptions = new List<TypeDescription>();

        public event ValueChanged ChangeListener;

        public byte[] Bytes {
            set {
                bytes = value;
                if (ChangeListener != null) {
                    ChangeListener();
                }
            }
        }
        public List<string> CurrentDecoded {
            get {
                List<string> result = new List<string>();
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes))) {
                    descriptions.ForEach(delegate(TypeDescription d) { result.Add(Util.decodeSafe(d, reader)); });
                }
                return result;
            }
        }
        public int DecodedByteCount {
            get {
                int count = 0;
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes))) {
                    foreach (TypeDescription d in descriptions) {
                        try {
                            string decoded = d.Decode(reader);
                            count += d.GetLength(decoded);
                        } catch {
                            break;
                        }
                    }
                }
                return count;
            }
        }
        public void addType(TypeDescription type) {
            descriptions.Add(type);
            ChangeListener();
        }
    }
}
