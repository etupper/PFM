using System;
using System.IO;
using System.Collections.Generic;

namespace EsfLibrary {
    public class MemoryMappedRecordNode : RecordNode {
        byte[] buffer;
        private int mapStart;
        private int byteCount;
        
        public bool InvalidateSiblings {
            get; set;
        }
        
        public override List<EsfNode> Value {
            get {
                List<EsfNode> values = Decoded.Value;
                return values;
            }
            set {
                Decoded.Value = value;
            }
        }
        protected bool invalid = false;
        public override bool Modified {
            get {
                return base.Modified;
            }
            set {
                base.Modified = value;
                invalid = Modified;
                if (Modified && InvalidateSiblings) {
                    bool invalidate = false;
                    // invalidate all nodes after this one
                    // because they have their addresses invalidated
                    (Parent as ParentNode).AllNodes.ForEach(node => {
                        if (node == this) {
                            invalidate = true;
                        } else if (invalidate && node is MemoryMappedRecordNode) {
                            (node as MemoryMappedRecordNode).invalid = true;
                        }
                    });
                }
            }
        }

        private RecordNode decoded = null;
        private RecordNode Decoded {
            get {
                if (decoded == null) {
                    using (var reader = new BinaryReader(new MemoryStream(buffer))) {
                        reader.BaseStream.Seek(mapStart+1, SeekOrigin.Begin);
                        decoded = Codec.ReadRecordNode(reader, OriginalTypeCode, true);
                    }
                    decoded.ModifiedEvent += ModifiedDelegate;
                    decoded.Modified = false;
                }
                return decoded;
            }
        }

        public MemoryMappedRecordNode (EsfCodec codec, byte[] bytes, int start) : base(codec, bytes[start-1]) {
            Codec = codec;
            buffer = bytes;
            mapStart = start-1;
            // byteCount = count;
        }

        public override void Decode(BinaryReader reader, EsfType type) {
            string name;
            byte remember;
            Codec.ReadRecordInfo(reader, OriginalTypeCode, out name, out remember);
            Name = name;
            Version = remember;
            int size = Codec.ReadSize(reader);
            int infoSize = (int)(reader.BaseStream.Position - mapStart);
            byteCount = size + infoSize;
            //mapStart = (int) reader.BaseStream.Position;
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }

        public override void Encode(BinaryWriter writer) {
            if (decoded == null && !invalid) {
                writer.Write(buffer, mapStart, byteCount);
            } else {
                Decoded.Encode(writer);
            }
        }
        
        private void ModifiedDelegate(EsfNode node) {
            //Console.WriteLine("modification!");
            RaiseModifiedEvent();
            Modified = node.Modified;
        }
    }
}

