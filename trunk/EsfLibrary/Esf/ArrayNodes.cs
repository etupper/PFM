using System;
using System.IO;
using System.Collections.Generic;

using Coordinates2D = System.Tuple<float, float>;
using Coordinates3D = System.Tuple<float, float, float>;

namespace EsfLibrary {
    public abstract class EsfArrayNode<T> : EsfValueNode<byte[]>, ICodecNode {
        protected EsfArrayNode(EsfCodec codec, Converter<T> reader) : base(delegate(string s) { throw new InvalidOperationException(); }) {
            Codec = codec;
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
        }

        public override string ToXml() {
            return string.Format("<{0} Length=\"{1}\"/>", TypeCode, Value.Length);
        }
        
        public void Decode(BinaryReader reader, EsfType type) {
            int size = Codec.ReadSize(reader);
            Value = reader.ReadBytes(size);
        }
        public void Encode(BinaryWriter writer) {
            writer.Write ((byte) TypeCode);
            Codec.WriteOffset(writer, val.Length);
            writer.Write(val);
        }
    }
    #region Typed Array Nodes
    public class BoolArrayNode : EsfArrayNode<bool> {
        public BoolArrayNode(EsfCodec codec) : base(codec, bool.Parse) {}
    }
    public class ByteArrayNode : EsfArrayNode<byte> {
        public ByteArrayNode(EsfCodec codec) : base(codec, byte.Parse) {}
    }
    public class SByteArrayNode : EsfArrayNode<sbyte> {
        public SByteArrayNode(EsfCodec codec) : base(codec, sbyte.Parse) {}
    }
    public class ShortArrayNode : EsfArrayNode<short> {
        public ShortArrayNode(EsfCodec codec) : base(codec, short.Parse) {}
    }
    public class UShortArrayNode : EsfArrayNode<ushort> {
        public UShortArrayNode(EsfCodec codec) : base(codec, ushort.Parse) {}
    }
    public class IntArrayNode : EsfArrayNode<int> {
        public IntArrayNode(EsfCodec codec) : base(codec, int.Parse) {}
    }
    public class LongArrayNode : EsfArrayNode<long> {
        public LongArrayNode(EsfCodec codec) : base(codec, long.Parse) {}
    }
    public class ULongArrayNode : EsfArrayNode<ulong> {
        public ULongArrayNode(EsfCodec codec) : base(codec, ulong.Parse) {}
    }
    public class UIntArrayNode : EsfArrayNode<uint> {
        public UIntArrayNode(EsfCodec codec) : base(codec, uint.Parse) {}
    }
    public class StringArrayNode : EsfArrayNode<string> {
        public StringArrayNode(EsfCodec codec) : base(codec, delegate(string v) { return v; }) {}
    }
    public class FloatArrayNode : EsfArrayNode<float> {
        public FloatArrayNode(EsfCodec codec) : base(codec, float.Parse) {}
    }   
    public class DoubleArrayNode : EsfArrayNode<double> {
        public DoubleArrayNode(EsfCodec codec) : base(codec, double.Parse) {}
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
        public Coordinate2DArrayNode(EsfCodec codec) : base(codec, Parse) {}
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
        public Coordinates3DArrayNode(EsfCodec codec) : base(codec, Parse) {}
    }
    #endregion
}

