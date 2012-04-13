using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Coordinates2D = System.Tuple<float, float>;
using Coordinates3D = System.Tuple<float, float, float>;

namespace EsfLibrary {
    public delegate T ValueReader<T>(BinaryReader reader);
    public delegate void ValueWriter<T>(BinaryWriter writer, T value);
    public delegate void NodeWriter(BinaryWriter writer, EsfNode node);

    public class EsfCodecUtil {

        public static EsfCodec GetCodec(Stream stream) {
            EsfCodec codec = null;
            byte[] magicBuffer = new byte[4]; 
            stream.Read(magicBuffer, 0, 4);
            using (BinaryReader reader = new BinaryReader(new MemoryStream(magicBuffer))) {
                uint magic = reader.ReadUInt32();
                switch (magic) {
                    case 0xABCE:
                        codec = new AbceCodec();
                        break;
                    case 0xABCF:
                        codec = new AbcfFileCodec();
                        break;
                    case 0xABCA:
                        codec = new AbcaFileCodec();
                        break;
                }
            }
            return codec;
        }
        public static void WriteEsfFile(string filename, EsfFile file) {
            using (BinaryWriter writer = new BinaryWriter(File.Create(filename))) {
                file.Codec.EncodeRootNode(writer, file.RootNode);
            }
        }
    }

    public class EsfFile {
        public EsfNode RootNode {
            get;
            private set;
        }
        public EsfCodec Codec {
            get;
            set;
        }
        public EsfFile(NamedNode rootNode, EsfCodec codec) {
            Codec = codec;
            RootNode = rootNode;
        }
        public EsfFile(Stream stream, EsfCodec codec) {
            using (var reader = new BinaryReader(stream)) {
                Codec = codec;
                RootNode = Codec.Parse(reader);
            }
        }
        public override bool Equals(object obj) {
            bool result = false;
            EsfFile file = obj as EsfFile;
            if (file != null) {
                result = Codec.ID == file.Codec.ID;
                result &= (RootNode as NamedNode).Equals(file.RootNode);
            }
            return result;
        }
    }

    #region Headers
    public class EsfHeader {
        public uint ID { get; set; }
    }
    #endregion

    public abstract class EsfCodec {
        public EsfCodec(uint id) {
            ID = id;
        }
        
        public readonly uint ID;

        public abstract void WriteHeader(BinaryWriter writer);
        
        public delegate void NodeStarting(byte typeCode, long readerPosition);
        public delegate void NodeRead(EsfNode node, long readerPosition);
        
        public event NodeStarting NodeReadStarting;
        public event NodeRead NodeReadFinished;

        protected SortedList<int, string> nodeNames;

        #region Reader Methods
        protected bool ReadBool(BinaryReader reader) { return reader.ReadBoolean(); }
        protected sbyte ReadSbyte(BinaryReader reader) { return reader.ReadSByte(); }
        protected short ReadShort(BinaryReader reader) { return reader.ReadInt16(); }
        protected int ReadInt(BinaryReader reader) { return reader.ReadInt32(); }
        protected long ReadLong(BinaryReader reader) { return reader.ReadInt64(); }
        protected byte ReadByte(BinaryReader reader) { return reader.ReadByte(); }
        protected ushort ReadUshort(BinaryReader reader) { return reader.ReadUInt16(); }
        protected uint ReadUint(BinaryReader reader) { return reader.ReadUInt32(); }
        protected ulong ReadUlong(BinaryReader reader) { return reader.ReadUInt64(); }
        protected float ReadFloat(BinaryReader reader) { return reader.ReadSingle(); }
        protected double ReadDouble(BinaryReader reader) { return reader.ReadDouble(); }
        protected Coordinates2D ReadCoordinates2D(BinaryReader r) { 
            return new Coordinates2D(r.ReadSingle(), r.ReadSingle());
        }
        protected Coordinates3D ReadCoordinates3D(BinaryReader r) {
            return new Coordinates3D(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }
        // virtual to be able to override
        protected virtual string ReadUtf16String(BinaryReader reader) { return ReadUtf16(reader); }
        protected virtual string ReadAsciiString(BinaryReader reader) { return ReadAscii (reader); }
        #endregion

        #region Writer Methods
        #region Values
        protected virtual void WriteBool(BinaryWriter w, bool b) { w.Write(b); }
        protected virtual void WriteInt(BinaryWriter w, int i) { w.Write(i); }
        protected virtual void WriteSByte(BinaryWriter w, sbyte b) { w.Write(b); }
        protected virtual void WriteShort(BinaryWriter w, short v) { w.Write(v); }
        protected virtual void WriteUShort(BinaryWriter w, ushort v) { w.Write(v); }
        protected virtual void WriteByte(BinaryWriter w, byte b) { w.Write(b); }
        protected virtual void WriteLong(BinaryWriter w, long v) { w.Write(v); }
        protected virtual void WriteULong(BinaryWriter w, ulong v) { w.Write(v); }
        protected virtual void WriteUInt(BinaryWriter w, uint ui) { w.Write(ui); }
        protected virtual void WriteFloat(BinaryWriter w, float ui) { w.Write(ui); }
        protected virtual void WriteDouble(BinaryWriter w, double ui) { w.Write(ui); }
        protected virtual void WriteCoordinates2D(BinaryWriter w, Coordinates2D t) { w.Write(t.Item1); w.Write(t.Item2); }
        protected virtual void WriteCoordinates3D(BinaryWriter w, Coordinates3D t) { w.Write(t.Item1); w.Write(t.Item2); w.Write(t.Item3); }

        protected virtual void WriteUtf16(BinaryWriter w, string s) {
            WriteUtf16Helper (w, s);
        }
        protected virtual void WriteAscii(BinaryWriter w, string s) {
            WriteAsciiHelper (w, s);
        }
        #endregion
        #region Nodes
        protected virtual void WriteBoolNode(BinaryWriter w, EsfNode node) { 
            w.Write((byte)0x01);
            w.Write ((node as EsfValueNode<bool>).Value);
        }

        protected void WriteSbyteNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x02); w.Write((node as EsfValueNode<sbyte>).Value); }
        protected void WriteShortNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x03); w.Write((node as EsfValueNode<short>).Value); }
        protected virtual void WriteIntNode(BinaryWriter w, EsfNode node) { w.Write ((byte)0x04); w.Write ((node as EsfValueNode<int>).Value); }
        protected virtual void WriteUIntNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x08); w.Write((node as EsfValueNode<uint>).Value); }
        protected void WriteLongNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x05); w.Write((node as EsfValueNode<long>).Value); }
        protected void WriteByteNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x06); w.Write((node as EsfValueNode<byte>).Value); }
        protected void WriteUshortNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x07); w.Write((node as EsfValueNode<ushort>).Value); }
        protected void WriteUlongNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x09); w.Write((node as EsfValueNode<ulong>).Value); }
        protected virtual void WriteFloatNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x0a); w.Write((node as EsfValueNode<float>).Value); }
        protected void WriteDoubleNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x0b); w.Write((node as EsfValueNode<double>).Value); }
        protected void WriteCoordinates2DNode(BinaryWriter w, EsfNode node) { 
            w.Write((byte)0x0c); 
            WriteCoordinates2D(w, (node as EsfValueNode<Coordinates2D>).Value); 
        }
        protected void WriteCoordinates3DNode(BinaryWriter w, EsfNode node) {
            w.Write ((byte)0x0d);
            WriteCoordinates3D (w, (node as EsfValueNode<Coordinates3D>).Value);
        }

        protected void WriteUtf16Node(BinaryWriter w, EsfNode node) { w.Write((byte)0x0e); WriteUtf16(w, (node as EsfValueNode<string>).Value); }
        protected void WriteAsciiNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x0f); WriteAscii(w, (node as EsfValueNode<string>).Value); }

        protected void WriteAngleNode(BinaryWriter w, EsfNode node) { w.Write((byte)0x10); w.Write((ushort)(node as EsfValueNode<ushort>).Value); }

        public static readonly NodeWriter NotImplemented = delegate(BinaryWriter w, EsfNode node) { throw new NotImplementedException(); };
        #endregion
        #endregion

        #region String Readers/Writers
        public static string ReadUtf16(BinaryReader reader) {
            ushort strLength = reader.ReadUInt16();
            return Encoding.Unicode.GetString(reader.ReadBytes(strLength * 2));
        }
        public static void WriteUtf16Helper(BinaryWriter writer, string toWrite) {
            writer.Write((ushort)toWrite.Length);
            writer.Write(Encoding.Unicode.GetBytes(toWrite));
        }
        public static string ReadAscii(BinaryReader reader) {
            ushort strLength = reader.ReadUInt16();
            return Encoding.ASCII.GetString(reader.ReadBytes(strLength));
        }
        public static void WriteAsciiHelper(BinaryWriter writer, string toWrite) {
            writer.Write((ushort)toWrite.Length);
            writer.Write(Encoding.ASCII.GetBytes(toWrite));
        }
        // a string reader looking up a string by its key
        public static string ReadStringReference(BinaryReader reader, Dictionary<string, int> list) {
            int referenceId = reader.ReadInt32();
            string result = null;
            foreach(string key in list.Keys) {
                int candidate = list[key];
                if (candidate == referenceId) {
                    result = key;
                    break;
                }
            }
            return result;
        }
        #endregion

        public EsfHeader Header {
            get;
            set;
        }

        // decodes the full stream, returning the root node
        public EsfNode Parse(BinaryReader reader) {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Header = ReadHeader(reader);
            uint nodeNameOffset = reader.ReadUInt32();
            long restorePosition = reader.BaseStream.Position;
            reader.BaseStream.Seek(nodeNameOffset, SeekOrigin.Begin);
            ReadNodeNames(reader);
            reader.BaseStream.Seek(restorePosition, SeekOrigin.Begin);
            return Decode(reader);
        }

        public abstract EsfHeader ReadHeader(BinaryReader reader);

        #region General node decoding
        // reads a node from the reader at its current position
        public EsfNode Decode(BinaryReader reader) {
            // read type code
            byte typeCode = reader.ReadByte();
            if (NodeReadStarting != null) {
                NodeReadStarting(typeCode, reader.BaseStream.Position-1);
            }
            EsfNode result = Decode(reader, typeCode);
            if (NodeReadFinished != null) {
                NodeReadFinished(result, reader.BaseStream.Position);
            }
            return result;
        }
        public virtual EsfNode Decode(BinaryReader reader, byte code) {
            // create node appropriate to type code
            EsfNode result;
            try {
                EsfType typeCode = (EsfType) code;
                // writeDebug = reader.BaseStream.Position > 0xd80000;
                if (typeCode < EsfType.BOOL_ARRAY) {
                    result = ReadValueNode(reader, typeCode);
                } else if (typeCode < EsfType.RECORD) {
                    result = ReadArrayNode(reader, typeCode);
                } else if (typeCode == EsfType.RECORD) {
                    result = ReadRecordNode(reader, code);
                } else if (typeCode == EsfType.RECORD_BLOCK) {
                    result = ReadRecordArrayNode(reader, code);
                } else {
                    throw new InvalidDataException(string.Format("Type code {0:x} at {1:x} invalid", typeCode, reader.BaseStream.Position - 1));
                }
                // Debug.WriteLine(string.Format("Read node {0} / {1}", result, result.TypeCode));
            } catch (Exception e) {
                Debug.WriteLine(string.Format("Exception at {0:x}: {1}", reader.BaseStream.Position, e));
                throw e;
            }
            return result;
        }
        #endregion

        public void EncodeRootNode(BinaryWriter writer, EsfNode rootNode) {
            WriteHeader(writer);
            long currentPosition = writer.BaseStream.Position;
            writer.Write(0);
            WriteRecordNode(writer, rootNode);
            // remember the offset of the node name list
            long nodeNamePosition = writer.BaseStream.Position;
            WriteNodeNames(writer);
            writer.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
            writer.Write((uint)nodeNamePosition);
        }

        // encodes the given node
        public void Encode(BinaryWriter writer, EsfNode node) {
            try {
                //NamedNode named = node as NamedNode;
                if (node.TypeCode == EsfType.RECORD_BLOCK_ENTRY) {
                    EncodeSized(writer, (node as NamedNode).AllNodes);
                } else if (node.TypeCode < EsfType.BOOL_ARRAY) {
                    WriteValueNode(writer, node);
                } else if (node.TypeCode < EsfType.RECORD) {
                    WriteArrayNode(writer, node);
                } else if (node.TypeCode == EsfType.RECORD) {
                    WriteRecordNode(writer, node);
                } else if (node.TypeCode == EsfType.RECORD_BLOCK) {
                    WriteRecordArrayNode(writer, node);
                } else {
                    throw new NotImplementedException(string.Format("Cannot write type code {0:x} at {1:x}", node.TypeCode));
                }
            } catch {
                Debug.WriteLine(string.Format("failed to write node {0}", node));
                throw;
            }
        }

        #region Value Nodes
        // read
        public virtual EsfNode ReadValueNode(BinaryReader reader, EsfType typeCode) {
            EsfNode result = null;
            switch (typeCode) {
            case EsfType.BOOL:
                result = new BoolValueNode { Value = ReadBool(reader) };
                break;
            case EsfType.INT8:
                result = new SByteValueNode { Value = ReadSbyte(reader) };
                break;
            case EsfType.INT16:
                result = new ShortValueNode { Value = ReadShort(reader) };
                break;
            case EsfType.INT32:
                result = new IntValueNode { Value = ReadInt(reader) };
                break;
            case EsfType.INT64:
                result = new LongValueNode { Value = ReadLong(reader) };
                break;
            case EsfType.UINT8:
                result = new ByteValueNode { Value = ReadByte(reader) };
                break;
            case EsfType.UINT16:
                result = new UShortValueNode { Value = ReadUshort(reader) };
                break;
            case EsfType.UINT32:
                result = new UIntValueNode { Value = ReadUint(reader) };
                break;
            case EsfType.UINT64:
                result = new ULongValueNode { Value = ReadUlong(reader) };
                break;
            case EsfType.SINGLE:
                result = new FloatValueNode { Value = ReadFloat(reader) };
                break;
            case EsfType.DOUBLE:
                result = new DoubleValueNode { Value = ReadDouble(reader) };
                break;
            case EsfType.COORD2D:
                result = new Coordinate2DValueNode { Value = ReadCoordinates2D(reader) };
                break;
            case EsfType.COORD3D:
                result = new Coordinates3DValueNode { Value = ReadCoordinates3D(reader) };
                break;
            case EsfType.UTF16:
                result = new StringValueNode { Value = ReadUtf16String(reader) };
                break;
            case EsfType.ASCII:
                result = new StringValueNode { Value = ReadAsciiString(reader) };
                break;
            case EsfType.ANGLE:
                result = new UShortValueNode { Value = ReadUshort(reader) };
                break;
            default:
                throw new InvalidDataException(string.Format("Invalid type code {0:x} at {1:x}", typeCode, reader.BaseStream.Position));
            }
            result.TypeCode = typeCode;
            return result;
        }
//        protected EsfValueNode<T> CreateValueNode<T>(BinaryReader reader, ValueReader<T> ReadValue, EsfType typeCode) {
//            return new EsfValueNode<T> { Value = ReadValue(reader), TypeCode = typeCode };
//        }

        public virtual void WriteValueNode(BinaryWriter writer, EsfNode node) {
            switch (node.TypeCode) {
                case EsfType.BOOL:
                    WriteBoolNode(writer, node);
                    break;
                case EsfType.INT8:
                    WriteSbyteNode(writer, node);
                    break;
                case EsfType.INT16:
                    WriteShortNode(writer, node);
                    break;
                case EsfType.INT32:
                    WriteIntNode(writer, node);
                    break;
                case EsfType.INT64:
                    WriteLongNode(writer, node);
                    break;
                case EsfType.UINT8:
                    WriteByteNode(writer, node);
                    break;
                case EsfType.UINT16:
                    WriteUshortNode(writer, node);
                    break;
                case EsfType.UINT32:
                    WriteUIntNode(writer, node);
                    break;
                case EsfType.UINT64:
                    WriteUlongNode(writer, node);
                    break;
                case EsfType.SINGLE:
                    WriteFloatNode(writer, node);
                    break;
                case EsfType.DOUBLE:
                    WriteDoubleNode(writer, node);
                    break;
                case EsfType.COORD2D:
                    WriteCoordinates2DNode(writer, node);
                    break;
                case EsfType.COORD3D:
                    WriteCoordinates3DNode(writer, node);
                    break;
                case EsfType.UTF16:
                    WriteUtf16Node(writer, node);
                    break;
                case EsfType.ASCII:
                    WriteAsciiNode(writer, node);
                    break;
                case EsfType.ANGLE:
                    WriteAngleNode(writer, node);
                    break;
                default:
                    throw new InvalidDataException(string.Format("Invalid type code {0:x} at {1:x}", node.TypeCode, writer.BaseStream.Position));
            }
        }
        #endregion

        #region Array Nodes
        protected virtual EsfNode ReadArrayNode(BinaryReader reader, EsfType typeCode) {
            EsfNode result = null;
            switch (typeCode) {
            case EsfType.BOOL_ARRAY:
                result = new BoolArrayNode { Value = ReadArray(reader), ItemReader = ReadBool };
                    // CreateArrayNode<bool>(reader), ItemReader = ReadBool);
                break;
            case EsfType.INT8_ARRAY:
                result = new SByteArrayNode { Value = ReadArray(reader), ItemReader = ReadSbyte };
                break;
            case EsfType.INT16_ARRAY:
                result = new ShortArrayNode { Value = ReadArray(reader), ItemReader = ReadShort };
                break;
            case EsfType.INT32_ARRAY:
                result = new IntArrayNode { Value = ReadArray(reader), ItemReader = ReadInt };
                break;
            case EsfType.INT64_ARRAY:
                result = new LongArrayNode { Value = ReadArray(reader), ItemReader = ReadLong };
                break;
            case EsfType.UINT8_ARRAY:
                result = new ByteArrayNode { Value = ReadArray(reader), ItemReader = ReadByte };
                break;
            case EsfType.UINT16_ARRAY:
                result = new UShortArrayNode { Value = ReadArray(reader), ItemReader = ReadUshort };
                break;
            case EsfType.UINT32_ARRAY:
                result = new UIntArrayNode { Value = ReadArray(reader), ItemReader = ReadUint };
                break;
            case EsfType.UINT64_ARRAY:
                result = new ULongArrayNode { Value = ReadArray(reader), ItemReader = ReadUlong };
                break;
            case EsfType.SINGLE_ARRAY:
                result = new FloatArrayNode { Value = ReadArray(reader), ItemReader = ReadFloat };
                break;
            case EsfType.DOUBLE_ARRAY:
                result = new DoubleArrayNode { Value = ReadArray(reader), ItemReader = ReadDouble };
                break;
            case EsfType.COORD2D_ARRAY:
                result = new Coordinate2DArrayNode { Value = ReadArray(reader), ItemReader = ReadCoordinates2D };
                break;
            case EsfType.COORD3D_ARRAY:
                result = new Coordinates3DArrayNode { Value = ReadArray(reader), ItemReader = ReadCoordinates3D };
                break;
            case EsfType.UTF16_ARRAY:
                result = new StringArrayNode { Value = ReadArray(reader), ItemReader = ReadUtf16String };
                break;
            case EsfType.ASCII_ARRAY:
                result = new StringArrayNode { Value = ReadArray(reader), ItemReader = ReadAsciiString };
                break;
            case EsfType.ANGLE_ARRAY:
                result = new UShortArrayNode { Value = ReadArray(reader), ItemReader = ReadUshort };
                break;
            default:
                throw new InvalidDataException(string.Format("Unknown array type code {0} at {1:x}", typeCode, reader.BaseStream.Position));
            }
            result.TypeCode = typeCode;
            return result;
        }
        protected virtual byte[] ReadArray(BinaryReader reader) {
            long targetOffset = ReadSize(reader);
            return reader.ReadBytes((int) (targetOffset - reader.BaseStream.Position));
        }
        
//        public T[] ReadArrayItems<T>(BinaryReader reader, ValueReader<T> ReadValue) {
//            long targetOffset = ReadSize(reader);
//            List<T> items = new List<T>();
//            while (reader.BaseStream.Position < targetOffset) {
//                items.Add(ReadValue(reader));
//            }
//            return items.ToArray();
//        }

        protected virtual void WriteArrayNode(BinaryWriter writer, EsfNode arrayNode) {
            byte[] encoded;
            switch (arrayNode.TypeCode) {
            case EsfType.BOOL_ARRAY:
                encoded = (arrayNode as EsfArrayNode<bool>).Value;
                break;
            case EsfType.INT8_ARRAY:
                EsfArrayNode<sbyte> byteNode = (arrayNode as EsfArrayNode<sbyte>);
                encoded = (arrayNode as EsfArrayNode<sbyte>).Value;
                break;
            case EsfType.INT16_ARRAY:
                encoded = (arrayNode as EsfArrayNode<short>).Value;
                break;
            case EsfType.INT32_ARRAY:
                encoded = (arrayNode as EsfArrayNode<int>).Value;
                break;
            case EsfType.INT64_ARRAY:
                encoded = (arrayNode as EsfArrayNode<long>).Value;
                break;
            case EsfType.UINT8_ARRAY:
                encoded = (arrayNode as EsfArrayNode<byte>).Value;
                break;
            case EsfType.UINT16_ARRAY:
                encoded = (arrayNode as EsfArrayNode<ushort>).Value;
                break;
                case EsfType.UINT32_ARRAY:
                encoded = (arrayNode as EsfArrayNode<uint>).Value;
                break;
            case EsfType.UINT64_ARRAY:
                encoded = (arrayNode as EsfArrayNode<ulong>).Value;
                break;
            case EsfType.SINGLE_ARRAY:
                encoded = (arrayNode as EsfArrayNode<float>).Value;
                break;
            case EsfType.DOUBLE_ARRAY:
                encoded = (arrayNode as EsfArrayNode<double>).Value;
                break;
                case EsfType.COORD2D_ARRAY:
                encoded = (arrayNode as EsfArrayNode<Coordinates2D>).Value;
                break;
            case EsfType.COORD3D_ARRAY:
                encoded = (arrayNode as EsfArrayNode<Coordinates3D>).Value;
                break;
            case EsfType.UTF16_ARRAY:
                encoded = (arrayNode as EsfArrayNode<string>).Value;
                break;
            case EsfType.ASCII_ARRAY:
                encoded = (arrayNode as EsfArrayNode<string>).Value;
                break;
            case EsfType.ANGLE_ARRAY:
                encoded = (arrayNode as EsfArrayNode<ushort>).Value;
                break;
            default:
                throw new InvalidDataException(string.Format("Invalid type code {0:x} at {1:x}", arrayNode.TypeCode, writer.BaseStream.Position));
            }
            writer.Write((byte) arrayNode.TypeCode);
            WriteOffset(writer, encoded.LongLength);
            writer.Write(encoded);
        }
//        protected byte[] EncodeArrayNode<T>(T[] array, ValueWriter<T> WriteItem) {
//            byte[] result;
//            MemoryStream bufferStream = new MemoryStream();
//            using (BinaryWriter writer = new BinaryWriter(bufferStream)) {
//                for (int i = 0; i < array.Length; i++) {
//                    WriteItem(writer, array[i]);
//                }
//                result = bufferStream.ToArray();
//            }
//            return result;
//        }
        #endregion

        #region Record Nodes
        // read an identified node from the reader at its current position
        protected EsfNode ReadRecordNode(BinaryReader reader, byte typeCode) {
            string name;
            byte version;
            ReadRecordInfo(reader, typeCode, out name, out version);
            long targetOffset = ReadSize(reader);
            List<EsfNode> childNodes = ReadToOffset(reader, targetOffset);
            return CreateRecordNode(name, version, childNodes);
        }
        protected virtual EsfNode CreateRecordNode(string name, byte version, List<EsfNode> childNodes) {
            return new NamedNode { Name = name, Version = version, Value = childNodes, TypeCode = EsfType.RECORD };
        }

        protected virtual void WriteRecordNode(BinaryWriter writer, EsfNode node) {
            NamedNode recordNode = node as NamedNode;
            ushort nameIndex = (ushort) nodeNames.IndexOfValue(recordNode.Name);
            WriteRecordInfo(writer, 0x80, nameIndex, recordNode.Version);
            EncodeSized(writer, recordNode.AllNodes);
        }
        #endregion

        #region Record Block Nodes
        protected EsfNode ReadRecordArrayNode(BinaryReader reader, byte typeCode) {
            string name;
            byte version;
            ReadRecordInfo(reader, typeCode, out name, out version);
            long targetOffset = ReadSize(reader);
            int itemCount = (int)ReadSize(reader);
            List<EsfNode> containedNodes = new List<EsfNode>(itemCount);
            for (int i = 0; i < itemCount; i++) {
                targetOffset = ReadSize(reader);
                List<EsfNode> items = ReadToOffset(reader, targetOffset);
                NamedNode contained = new NamedNode { Name = string.Format("{0} - {1}", name, i), Value = items, TypeCode = EsfType.RECORD_BLOCK_ENTRY };
                containedNodes.Add(contained);
            }
            NamedNode result = new NamedNode { Name = name, Version = version, TypeCode = EsfType.RECORD_BLOCK, Value = containedNodes };
            return result;
        }

        protected virtual void WriteRecordArrayNode(BinaryWriter writer, EsfNode node) {
            NamedNode recordBlockNode = node as NamedNode;
            ushort nameIndex = (ushort)nodeNames.IndexOfValue(recordBlockNode.Name);
            WriteRecordInfo(writer, 0x81, nameIndex, recordBlockNode.Version);
            long beforePosition = writer.BaseStream.Position;
            writer.Seek(4, SeekOrigin.Current);
            // write number of entries
            WriteSize(writer, recordBlockNode.AllNodes.Count);
            foreach (EsfNode child in recordBlockNode.AllNodes) {
                EncodeSized(writer, (child as NamedNode).AllNodes);
            }
//            EncodeSized(writer, recordBlockNode.AllNodes);
            long afterPosition = writer.BaseStream.Position;
            writer.BaseStream.Seek(beforePosition, SeekOrigin.Begin);
            WriteSize(writer, afterPosition);
            writer.BaseStream.Seek(afterPosition, SeekOrigin.Begin);
        }
        #endregion

        #region Version-dependent Overridables
        // Reading data
        protected virtual void ReadNodeNames(BinaryReader reader) {
            int count = reader.ReadInt16();
            nodeNames = new SortedList<int, string>(count);
            for (ushort i = 0; i < count; i++) {
                nodeNames.Add(i, ReadAscii(reader));
            }
        }
        protected virtual void WriteNodeNames(BinaryWriter writer) {
            writer.Write((short)nodeNames.Count);
            for (int i = 0; i < nodeNames.Count; i++) {
                WriteAsciiHelper(writer, nodeNames[i]);
            }
        }
        
        protected virtual long ReadSize(BinaryReader reader) {
            return reader.ReadInt32();
        }
        protected virtual void WriteSize(BinaryWriter writer, long size) {
            writer.Write((uint)size);
        }
        protected virtual void WriteOffset(BinaryWriter writer, long offset) {
            // write the target offset: position + 4 byte size + byte count
            WriteSize(writer, offset + writer.BaseStream.Position + 4);
        }

        protected virtual void ReadRecordInfo(BinaryReader reader, byte typeCode, out string name, out byte version) {
            ushort nameIndex = reader.ReadUInt16();
            name = GetNodeName(nameIndex);
            version = reader.ReadByte();
        }
        protected virtual void WriteRecordInfo(BinaryWriter writer, byte typeCode, ushort nameIndex, byte version) {
            writer.Write((byte)typeCode);
            writer.Write(nameIndex);
            writer.Write(version);
        }

        protected virtual List<EsfNode> ReadToOffset(BinaryReader reader, long targetOffset) {
            List<EsfNode> result = new List<EsfNode>();
            while (reader.BaseStream.Position < targetOffset) {
                result.Add(Decode(reader));
            }
            return result;
        }
        protected virtual void EncodeSized(BinaryWriter writer, List<EsfNode> nodes) {
            long sizePosition = writer.BaseStream.Position;
            writer.Seek(4, SeekOrigin.Current);
            foreach (EsfNode node in nodes) {
                Encode(writer, node);
            }
            long positionAfter = writer.BaseStream.Position;
            // go back to before the encoded contents and write the size
            writer.BaseStream.Seek(sizePosition, SeekOrigin.Begin);
            WriteSize(writer, positionAfter);
            writer.BaseStream.Seek(positionAfter, SeekOrigin.Begin);
        }
        #endregion

        #region Helpers
        // helper methods
        protected void writeValue<T>(BinaryWriter writer, T value, ValueWriter<T> WriteValue) {
            //writer.Write(typeCode);
            WriteValue(writer, value);
        }
        protected string GetNodeName(ushort nameIndex) {
            // return string.Format("Node with name index {0}", nameIndex);
            try {
                return nodeNames[nameIndex];
            } catch {
                Console.WriteLine(string.Format("Exception: invalid node index {0}", nameIndex));
                throw;
            }
        }
        protected NodeRead CreateEventDelegate() {
            NodeRead result = null;
            if (NodeReadFinished != null) {
                result = delegate(EsfNode node, long position) { NodeReadFinished(node, position); };
            }
            return result;
        }
        #endregion
    }
}

