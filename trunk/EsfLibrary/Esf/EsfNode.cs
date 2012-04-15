using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Coordinates2D = System.Tuple<float, float>;
using Coordinates3D = System.Tuple<float, float, float>;
using System.IO;

namespace EsfLibrary {
    public interface ICodecNode {
        void Decode(BinaryReader reader, EsfType readAs);
        void Encode(BinaryWriter writer);
    }

    public abstract class EsfNode {
        public delegate void Modification (EsfNode node);
        public event Modification ModifiedEvent;

        public EsfCodec Codec { get; set; }
        public virtual EsfType TypeCode { get; set; }

        #region Properties        
        public EsfNode Parent { get; set; }
        public Type SystemType { get; set; }
        
        // property Deleted; also sets Modified
        private bool deleted = false;
        public bool Deleted { 
            get {
                return deleted;
            }
            set {
                deleted = value;
                Modified = true;
            }
        }
        
        // property Modified; also sets parent to new value
        protected bool modified = false;
        public virtual bool Modified {
            get {
                return modified;
            }
            set {
                if (modified != value) {
                    modified = value; 
                    RaiseModifiedEvent();
                    if (modified && Parent != null) {
                        Parent.Modified = value;
                    }
                }
            }
        }
        protected void RaiseModifiedEvent() {
            if (ModifiedEvent != null) {
                ModifiedEvent(this);
            }
        }
        #endregion
        public virtual void FromString(string value) {
            throw new InvalidOperationException();
        }

        public abstract string ToXml();
    }

    [DebuggerDisplay("ValueNode: {Value}")]
    public abstract class EsfValueNode<T> : EsfNode {
        // public NodeStringConverter<T> Converter { get; set; }
        public delegate S Converter<S>(string value);
        protected Converter<T> ConvertString;
        
        public EsfValueNode() : this (null) {}

        public EsfValueNode(Converter<T> converter) : base() {
            SystemType = typeof(T);
            ConvertString = converter;
        }

        protected T val;
        public virtual T Value {
            get {
                return val;
            }
            set {
                if (!EqualityComparer<T>.Default.Equals (val, value)) {
                    val = value;
                    Modified = true;
                }
            }
        }

        public override void FromString(string value) {
            Value = ConvertString(value);
        }
        public override string ToString() {
            return val.ToString();
        }
        
        public override bool Equals(object o) {
            bool result = false;
            try {
                T otherValue = (o as EsfValueNode<T>).Value;
                result = (otherValue != null) && EqualityComparer<T>.Default.Equals(val, otherValue);
            } catch {}
            if (!result) {
            }
            return result;
        }

        public override string ToXml() {
            return string.Format("<{0} Value=\"{1}\"/>", TypeCode, Value);
        }
    }

    public abstract class CodecNode<T> : EsfValueNode<T>, ICodecNode {
        public CodecNode(Converter<T> conv) : base(conv) { }
        public void Decode(BinaryReader reader, EsfType readAs) {
            Value = ReadValue(reader, readAs);
        }
        protected abstract T ReadValue(BinaryReader reader, EsfType readAs);
        public void Encode(BinaryWriter writer) {
            writer.Write((byte)TypeCode);
            WriteValue(writer);
        }
        protected abstract void WriteValue(BinaryWriter writer);
    }

    public abstract class EsfArrayNode<T> : EsfValueNode<byte[]> {
        public EsfArrayNode() {
            // Converter = new ArrayNodeConverter<T>(new PrimitiveNodeConverter<T>());
        }
        protected EsfArrayNode(Converter<T> reader) : base(delegate(string s) { throw new InvalidOperationException(); }) {
        }
        public ValueReader<T> ItemReader {
            get; set;
        }
  
        public T[] Values {
            get {
                throw new InvalidOperationException("Cannot show items for " + TypeCode);
            }
            private set {
            }
        }
        
        static bool ArraysEqual<O> (O[] array1, O[] array2) {
            bool result = array1.Length == array2.Length;
            if (result) {
                for (int i = 0; i < array1.Length; i++) {
                    if (!EqualityComparer<O>.Default.Equals (array1[i], array2[i])) {
                        result = false;
                        break;
                    }
                }
            }
            if (!result) {
            }
            return result;
        }
        public override bool Equals(object o) {
            EsfArrayNode<T> otherNode = o as EsfArrayNode<T>;
            bool result = otherNode != null;
            result &= ArraysEqual(Value, otherNode.Value);
            return result;
        }
        
        static O[] ParseArray<O> (string value, Converter<O> convert) {
            string[] itemStrings = value.Split(' ');
            List<O> result = new List<O>(itemStrings.Length);
            foreach(string s in itemStrings) {
                result.Add(convert(s));
            }
            return result.ToArray();
        }
        public override string ToString() {
            string result = val.ToString();
            result = string.Format("{0}{1}]", result.Substring(0, result.Length-1), val.Length);
            return result;
//            foreach(T item in val) {
//                result += item.ToString() + " ";
//            }
//            return result.TrimEnd();
        }

        public override string ToXml() {
            return string.Format("<{0} Length=\"{1}\"/>", TypeCode, Value.Length);
        }
    }
    #region Typed Array Nodes
    public class BoolArrayNode : EsfArrayNode<bool> {
        public BoolArrayNode() : base(bool.Parse) {}
    }
    public class ByteArrayNode : EsfArrayNode<byte> {
        public ByteArrayNode() : base(byte.Parse) {}
    }
    public class SByteArrayNode : EsfArrayNode<sbyte> {
        public SByteArrayNode() : base(sbyte.Parse) {}
    }
    public class ShortArrayNode : EsfArrayNode<short> {
        public ShortArrayNode() : base(short.Parse) {}
    }
    public class UShortArrayNode : EsfArrayNode<ushort> {
        public UShortArrayNode() : base(ushort.Parse) {}
    }
    public class IntArrayNode : EsfArrayNode<int> {
        public IntArrayNode() : base(int.Parse) {}
    }
    public class LongArrayNode : EsfArrayNode<long> {
        public LongArrayNode() : base(long.Parse) {}
    }
    public class ULongArrayNode : EsfArrayNode<ulong> {
        public ULongArrayNode() : base(ulong.Parse) {}
    }
    public class UIntArrayNode : EsfArrayNode<uint> {
        public UIntArrayNode() : base(uint.Parse) {}
    }
    public class StringArrayNode : EsfArrayNode<string> {
        public StringArrayNode() : base(delegate(string v) { return v; }) {}
    }
    public class FloatArrayNode : EsfArrayNode<float> {
        public FloatArrayNode() : base(float.Parse) {}
    }   
    public class DoubleArrayNode : EsfArrayNode<double> {
        public DoubleArrayNode() : base(double.Parse) {}
    }
    public class Coordinate2DArrayNode : EsfArrayNode<Coordinates2D> {
        static Coordinates2D Parse(string value) {
            string removedBrackets = value.Substring(1, value.Length-1);
            string[] coords = removedBrackets.Split(',');
            Coordinates2D result = new Coordinates2D (
                float.Parse(coords[0].Trim()),
                float.Parse(coords[1].Trim())
            );
            return result;
        }
        public Coordinate2DArrayNode() : base(Parse) {}
    }
    public class Coordinates3DArrayNode : EsfArrayNode<Coordinates3D> {
        static Coordinates3D Parse(string value) {
            string removedBrackets = value.Substring(1, value.Length-1);
            string[] coords = removedBrackets.Split(',');
            Coordinates3D result = new Coordinates3D (
                float.Parse(coords[0].Trim()),
                float.Parse(coords[1].Trim()),
                float.Parse(coords[2].Trim())
            );
            return result;
        }
        public Coordinates3DArrayNode() : base(Parse) {}
    }
    #endregion

    // 0x80 - 0x81
    [DebuggerDisplay("NamedNode: {Name}")]
    public class NamedNode : EsfNode {
        public string Name {
            get;
            set;
        }
        public byte Version {
            get;
            set;
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
                        val.ForEach(node => node.Modified = false);
                    }
                }
            }
        }

        List<EsfNode> val = new List<EsfNode> ();

        public List<EsfNode> Value {
            private get {
                return val;
            }
            set {
                if (!val.Equals (value)) {
                    val.ForEach(node => node.Parent = null);
                    val = value;
                    val.ForEach(node => node.Parent = this);
                    Modified = true;
                }
            }
        }
        public List<EsfNode> AllNodes {
            get {
                return val;
            }
        }
        public List<NamedNode> Children {
            get {
                List<NamedNode> result = new List<NamedNode>();
                foreach (EsfNode node in Value) {
                    if (node is NamedNode) {
                        result.Add(node as NamedNode);
                    }
                }
                return result;
            }
        }
        public List<EsfNode> Values {
            get {
                List<EsfNode> result = new List<EsfNode>();
                foreach (EsfNode node in Value) {
                    if (!(node is NamedNode)) {
                        result.Add(node);
                    }
                }
                return result;
            }
        }

        public override string ToString() {
            return Name;
        }
        
        public override bool Equals(object obj) {
            bool result = false;
            NamedNode node = obj as NamedNode;
            if (node != null) {
                result = node.AllNodes.Count == val.Count;
                result &= node.Name == Name;
                if (result) {
                    for(int i = 0; i < node.AllNodes.Count; i++) {
                        result &= node.AllNodes[i].Equals(val[i]);
                        if (!result) {
                            break;
                        }
                    }
                }
            }
            if (!result) {
            }
            return result;
        }

        public override string ToXml() {
            return ToXml(false);
        }
        public string ToXml(bool end) {
            return end ? string.Format("</{0}>  <!-- {1} -->", TypeCode, Name)
                : string.Format("<{0} Name=\"{1}\">", TypeCode, Name);
        }
    }

    public class CompressedNode : NamedNode {
        public static readonly string TAG_NAME = "COMPRESSED_DATA";
        public static readonly string INFO_TAG = "COMPRESSED_DATA_INFO";
        public byte[] EncodeProperties { get; set; }
        public EsfNode RootNode {
            get { return AllNodes[0]; }
            set { AllNodes.Clear(); AllNodes.Add(value); }
        }
        public override bool Equals(object obj) {
            bool result = false;
            CompressedNode compressed = obj as CompressedNode;
            if (compressed != null) {
                result = compressed.RootNode.Equals(RootNode);
            }
            if (!result) {
            }
            return result;
        }
    }
}

