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
        public override bool Modified {
            get {
                return base.Modified;
            }
            set {
                // don't set modified if we haven't decoded our delegate yet
                // so we don't decode it now without the value having changed
                if (decoded != null) {
                    base.Modified = value;
                }
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
            InvalidateSiblings = true;
            // byteCount = count;
        }
        
        // if this is set, the siblings following this node will also be invalidated
        // when a modification of this node occurs
        public bool InvalidateSiblings {
            get; set;
        }
  
        bool invalid;
        public bool Invalid { 
            get { return invalid; }
            set {
                if (invalid != value) {
                    invalid = value;
                    if (value) {
                        ParentNode p = Parent as ParentNode;
                        while (p != null) {
                            MemoryMappedRecordNode mapped = p as MemoryMappedRecordNode;
                            if (mapped != null) {
                                mapped.Invalid = true;
                            }
                            p = p.Parent as ParentNode;
                        }
                    }
                }
            }
        }
        public override bool Modified {
            get {
                return base.Modified;
            }
            set {
                base.Modified = value;
                
                // if we are invalidated already, stay so; we will need to
                // encode fully because we may have been set to not modified
                // in the meantime, and overwrite the changes in between
                Invalid |= Modified;
                if (Modified && InvalidateSiblings) {
                    new SiblingInvalidator(this).Invalidate();
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
            if (Invalid) {
                Decoded.Encode(writer);
                // Console.WriteLine("actually encoding {0}", Name);
            } else {
                // Console.WriteLine("encoding by memory mapping {0}", Name);
                writer.Write(buffer, mapStart, byteCount);
            }
        }

        public override EsfNode CreateCopy() {
            return Decoded.CreateCopy();
        }
    }

    public class SiblingInvalidator {
        private ParentNode Reference;
        public SiblingInvalidator(ParentNode reference) {
            Reference = reference;
        }
        public void Invalidate() {
            bool invalidate = false;

            // invalidate all nodes after this one
            // because they have their addresses invalidated
            ParentNode parent = ((Reference as ParentNode).Parent as ParentNode);
            if (parent != null) {
                parent.AllNodes.ForEach(node => {
                    if (node == Reference) {
                        invalidate = true;
                    } else if (invalidate && node is MemoryMappedRecordNode) {
                        (node as MemoryMappedRecordNode).Modified = true;
                    }
                });
            }
        }
    }

    public class ParentInvalidator {
        public static void Invalidate(EsfNode node) {

        }
    }
    
    public class DeepInvalidator {
//        private ParentNode InvalidateBelow;
        public DeepInvalidator() {
//            InvalidateBelow = node;
        }
        public void Invalidate(ParentNode parent) {
            if (parent != null) {
                MemoryMappedRecordNode mapped = parent as MemoryMappedRecordNode;
                if (mapped != null) {
                    mapped.Invalid = true;
                }
                parent.AllNodes.ForEach(node => {
                    ParentNode p = node as ParentNode;
                    if (p != null) {
                        Invalidate (p);
                    }
                });
            }
        }
    }
}

