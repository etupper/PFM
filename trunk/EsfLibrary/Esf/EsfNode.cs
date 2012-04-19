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
    public interface INamedNode {
        string GetName();
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

    [DebuggerDisplay("ValueNode: {TypeCode}")]
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

}

