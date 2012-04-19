using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace EsfLibrary {
    // 0x80 - 0x81
    [DebuggerDisplay("ParentNode: {Name}")]
    public abstract class ParentNode : EsfValueNode<List<EsfNode>>, INamedNode {
        public ParentNode() : base(new List<EsfNode>()) {
        }
        public ParentNode(byte code) : this() {
            originalCode = code;
        }
        
        public virtual string Name {
            get;
            set;
        }
        public byte Version {
            get;
            protected set;
        }
        public int Size {
            get;
            set;
        }
        public string GetName() {
            return Name;
        }
        public override bool Modified {
            get {
                return modified;
            }
            set {
                if (modified != value) {
                    modified = value;
                    RaiseModifiedEvent();
                    if (modified && Parent != null) {
                        Parent.Modified = value;
                    } else if (!modified) {
                        Value.ForEach(node => node.Modified = false);
                    }
                }
            }
        }

        private byte originalCode = 0;
        public byte OriginalTypeCode {
            get {
                return (originalCode == 0) ? (byte) TypeCode : originalCode;
            }
            set {
                originalCode = value;
            }
        }

        public override List<EsfNode> Value {
            get {
                return base.Value;
            }
            set {
                if (!Value.Equals (value)) {
                    // remove references from children
                    Value.ForEach(node => node.Parent = null);
                    base.Value = value;
                    //val = value;
                    Value.ForEach(node => node.Parent = this);
                    Modified = true;
                }
            }
        }
        public virtual List<EsfNode> AllNodes {
            get {
                return Value;
            }
        }
        public List<ParentNode> Children {
            get {
                List<ParentNode> result = new List<ParentNode>();
                Value.ForEach(node => { if ((node is ParentNode)) result.Add(node as ParentNode); });
                return result;
            }
        }
        public List<EsfNode> Values {
            get {
                List<EsfNode> result = new List<EsfNode>();
                Value.ForEach(node => { if (!(node is ParentNode)) result.Add(node); });
                return result;
            }
        }

        public override bool Equals(object obj) {
            bool result = false;
            ParentNode node = obj as ParentNode;
            if (node != null) {
                result = node.Name.Equals(Name);
                result &= node.AllNodes.Count == Value.Count;
                if (result) {
                    for(int i = 0; i < node.AllNodes.Count; i++) {
                        result &= node.AllNodes[i].Equals(Value[i]);
                        if (!result) {
                            break;
                        }
                    }
                }
            }
            if (!result) {
                return false;
            }
            return result;
        }

        public override string ToXml() {
            return ToXml(false);
        }
        public virtual string ToXml(bool end) {
            return end ? string.Format("</{0}>", TypeCode) : string.Format("<{0}\">", TypeCode);
        }
    }

    [DebuggerDisplay("Record: {Name}")]
    public class RecordNode : ParentNode, ICodecNode {
        public RecordNode(EsfCodec codec, byte originalCode = 0) : base(originalCode) {
            Codec = codec;
        }
        public virtual void Encode(BinaryWriter writer) {
            Codec.WriteRecordInfo(writer, (byte)TypeCode, Name, Version);
            Codec.EncodeSized(writer, AllNodes);
        }
        public override EsfType TypeCode {
            get {
                return EsfType.RECORD;
            }
            set {
                // ignore 
            }
        }
        public virtual void Decode(BinaryReader reader, EsfType unused) {
            string outName;
            byte outVersion;
            Codec.ReadRecordInfo(reader, OriginalTypeCode, out outName, out outVersion);
            Name = outName;
            Version = outVersion;
            Size = Codec.ReadSize(reader);
            Value = Codec.ReadToOffset(reader, reader.BaseStream.Position + Size);
        }
        public override string ToString() {
            return Name;
        }
        public override bool Equals(object obj) {
            bool result = false;
            RecordNode node = obj as RecordNode;
            result = (node != null) && base.Equals(obj);
            if (!result) {
            }
            return result;
        }
    }

    [DebuggerDisplay("RecordArray: {Name}")]
    public class RecordArrayNode : ParentNode, ICodecNode {
        public override List<EsfNode> Value {
            get {
                return base.Value;
            }
            set {
                for (int i = 0; i < value.Count; i++) {
                    (value[i] as RecordEntryNode).Name = string.Format("{0} - {1}", Name, i);
                }
                base.Value = value;
            }
        }
        
        public RecordArrayNode(EsfCodec codec, byte originalCode = 0) : base(originalCode) {
            Codec = codec;
        }
        public override EsfType TypeCode {
            get { return EsfType.RECORD_BLOCK; }
            set { }
        }
        public void Decode(BinaryReader reader, EsfType unused) {
            string name;
            byte version;
            Codec.ReadRecordInfo(reader, OriginalTypeCode, out name, out version);
            Name = name;
            Version = version;
            Size = (int) Codec.ReadSize(reader);
            int itemCount = Codec.ReadCount(reader);
            List<EsfNode> containedNodes = new List<EsfNode>(itemCount);
            for (int i = 0; i < itemCount; i++) {
                RecordEntryNode contained = new RecordEntryNode(Codec) {
                    Name = string.Format("{0} - {1}", Name, i),
                    TypeCode = EsfType.RECORD_BLOCK_ENTRY
                };
                contained.Decode(reader, EsfType.RECORD_BLOCK_ENTRY);
                containedNodes.Add(contained);
            }
            Value = containedNodes;
        }
        public void Encode(BinaryWriter writer) {
            Codec.WriteRecordInfo(writer, (byte)TypeCode, Name, Version);
            Codec.EncodeSized(writer, AllNodes, true);
        }
    }

    [DebuggerDisplay("Record Entry: {Name}")]
    public class RecordEntryNode : ParentNode, ICodecNode, INamedNode {
        public RecordEntryNode(EsfCodec codec) {
            TypeCode = EsfType.RECORD_BLOCK_ENTRY;
            Codec = codec;
        }
        public void Encode(BinaryWriter writer) {
            Codec.EncodeSized(writer, AllNodes);
        }
        public void Decode(BinaryReader reader, EsfType unused) {
            Size = (int) Codec.ReadSize(reader);
            Value = Codec.ReadToOffset(reader, reader.BaseStream.Position + Size);
        }
    }
}
