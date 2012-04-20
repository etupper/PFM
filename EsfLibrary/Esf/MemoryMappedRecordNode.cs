using System;
using System.IO;
using System.Collections.Generic;

namespace EsfLibrary {
    public abstract class DelegatingNode : RecordNode {
        protected DelegatingNode(EsfCodec codec, byte originalCode = 0) : base(codec, originalCode) {
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
        protected RecordNode decoded;
        protected RecordNode Decoded {
            get {
                if (decoded == null) {
                    decoded = DecodeDelegate();
                    decoded.Parent = this;
                    decoded.Modified = false;
                    decoded.ModifiedEvent += ModifiedDelegate;
                }
                return decoded;
            }
        }
        protected abstract RecordNode DecodeDelegate();

        private void ModifiedDelegate(EsfNode node) {
            Modified = node.Modified;
        }
    }
 
    /**
     * A record node that keeps track where in memory it was loaded from
     * and only evaluates that data when its child nodes are requested.
     */
    public class MemoryMappedRecordNode : DelegatingNode {
        byte[] buffer;
        private int mapStart;
        private int byteCount;

        public MemoryMappedRecordNode (EsfCodec codec, byte[] bytes, int start) : base(codec, bytes[start-1]) {
            Codec = codec;
            buffer = bytes;
            mapStart = start-1;
            // byteCount = count;
        }
        
        // if this is set, the siblings following this node will also be invalidated
        // when a modification of this node occurs
        public bool InvalidateSiblings {
            get; set;
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

        protected override RecordNode DecodeDelegate() {
            RecordNode result;
            using (var reader = new BinaryReader(new MemoryStream(buffer))) {
                reader.BaseStream.Seek(mapStart+1, SeekOrigin.Begin);
                result = Codec.ReadRecordNode(reader, OriginalTypeCode, true);
            }
            return result;
        }

        public override void Decode(BinaryReader reader, EsfType type) {
            // we need to keep track of the original data to give it to the actual parser later
            string name;
            byte remember;
            Codec.ReadRecordInfo(reader, OriginalTypeCode, out name, out remember);
            Name = name;
            Version = remember;
            int size = Codec.ReadSize(reader);
            int infoSize = (int)(reader.BaseStream.Position - mapStart);
            byteCount = size + infoSize;
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }

        public override void Encode(BinaryWriter writer) {
            if (invalid) {
                Decoded.Encode(writer);
            } else {
                writer.Write(buffer, mapStart, byteCount);
            }
        }
    }
}

