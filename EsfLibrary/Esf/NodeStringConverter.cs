using System;

namespace EsfLibrary {
    public interface NodeStringConverter<T> {
        string ToString(T value);
        T GetValue(string value);
    }
    
    public abstract class PrimitiveNodeConverter<T> : NodeStringConverter<T> {
        public string ToString(T value) {
            return value.ToString();
        }
        public abstract T GetValue(string value);
    }
    
    public class ArrayNodeConverter<T> : NodeStringConverter<T[]> {
        private NodeStringConverter<T> ItemDelegate;
        public ArrayNodeConverter(NodeStringConverter<T> itemConverter) {
            ItemDelegate = itemConverter;
        }
        public string ToString(T[] values) {
            string result = values.GetType().ToString();
            result = result.Substring(0, result.Length-1);
            result = string.Format("{0}{1}]", result, values.Length);
            return result;
        }
        public T[] GetValue(string value) {
            throw new NotImplementedException();
        }
    }
    
    public class IntNodeConverter : PrimitiveNodeConverter<int> {
        public override int GetValue(string value) {
            return int.Parse(value);
        }
    }
    public class UIntNodeConverter : PrimitiveNodeConverter<uint> {
        public override uint GetValue(string value) {
            return uint.Parse(value);
        }
    }
}

