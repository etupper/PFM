using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace EsfLibrary {
    public class AbcaFileCodec : AbcfFileCodec {
        #region Marker Bits
        static byte RECORD_BIT = 0x80; // 10000000
        // if set, this is a array of records
        static ushort BLOCK_BIT = 0x40;  // 01000000
        // if not set, record info is encodec in 2 bytes
        static byte LONG_INFO = 0x20; // 00100000
        #endregion

        #region Added Readers
        static ValueReader<bool> TrueValue = delegate(BinaryReader reader) { return true; };
        static ValueReader<bool> FalseValue = delegate(BinaryReader reader) { return false; };
        static ValueReader<uint> UIntZero = delegate(BinaryReader reader) { return 0; };
        static ValueReader<uint> UIntOne = delegate(BinaryReader reader) { return 1; };
        static ValueReader<float> FloatZero = delegate(BinaryReader reader) { return 0; };
        static ValueReader<int> IntByteReader = delegate(BinaryReader reader) { return reader.ReadSByte(); };
        static ValueReader<int> Int16Reader = delegate(BinaryReader reader) { return reader.ReadInt16(); };
        static ValueReader<uint> UIntByteReader = delegate(BinaryReader reader) { return reader.ReadByte(); };
        static ValueReader<uint> UInt16Reader = delegate(BinaryReader reader) { return reader.ReadUInt16(); };
        static uint UInt24Reader(BinaryReader reader) {
            uint value = 0;
            for(int i = 0; i < 3; i++) {
                value = (value << 8) + reader.ReadByte();
            }
            return value;
        }
        static ValueReader<int> Int24Reader = delegate(BinaryReader reader) { 
            int value = reader.ReadByte();
            bool sign = (value & 0x80) != 0;
            value = value & 0x7f;
            for (int i = 0; i < 2; i++) {
                value = (value << 8) + reader.ReadByte();
            }
            if (sign) {
                value = -value;
            }
            return value;
        };
        #endregion
        #region Added Writers
        protected void WriteBoolNoop(BinaryWriter writer, bool value) { }
        protected void WriteFloatNoop(BinaryWriter writer, float value) { }
        #endregion

        public AbcaFileCodec() : base(0xABCA) {}
        
        public override EsfNode Decode(BinaryReader reader, byte typeCode) {
            EsfNode result;
            byte recordBit = (byte)(typeCode & RECORD_BIT);
            if (recordBit == 0 || reader.BaseStream.Position == headerLength + 1) {
                switch ((EsfType) typeCode) {
                    case EsfType.INT32_ZERO:
                    case EsfType.INT32_BYTE:
                    case EsfType.INT32_SHORT:
                    case EsfType.INT32_24BIT:
                    case EsfType.INT32:
                        result = new OptimizedIntNode();
                        (result as OptimizedIntNode).Decode(reader, (EsfType) typeCode);
                        break;
                    default:
                        // for non-blocks and root node, previous decoding is used
                        result = base.Decode(reader, typeCode);
                        break;
                }
            } else {
                bool blockBit = ((typeCode & BLOCK_BIT) != 0);
                //Debug.WriteLine(string.Format("Reading section {0}node at {1:x}", blockBit ? "block " : "", reader.BaseStream.Position-1));
                // use new block decoding
                result = blockBit
                    ? ReadRecordArrayNode(reader, typeCode)
                    : ReadRecordNode(reader, typeCode);
            }
            return result;
        }
  
        protected override EsfNode ReadRecordArrayNode(BinaryReader reader, byte typeCode) {
            RecordArrayNode result = new RecordArrayNode(this, typeCode);
            result.Decode(reader, EsfType.RECORD_BLOCK);
            return result;
        }

        // Adds readers for optimized values
        public override EsfNode ReadValueNode(BinaryReader reader, EsfType typeCode) {
            EsfNode result;
            switch (typeCode) {
            case EsfType.BOOL:
            case EsfType.BOOL_TRUE:
            case EsfType.BOOL_FALSE:
                result = new OptimizedBoolNode();
                break;
            case EsfType.UINT32_ZERO:
            case EsfType.UINT32_ONE:
            case EsfType.UINT32_BYTE:
            case EsfType.UINT32_SHORT:
            case EsfType.UINT32_24BIT:
            case EsfType.UINT32:
                result = new OptimizedUIntNode();
                break;
            case EsfType.INT32_ZERO:
            case EsfType.INT32_BYTE:
            case EsfType.INT32_SHORT:
            case EsfType.INT32_24BIT:
            case EsfType.INT32:
                result = new OptimizedIntNode();
                break;
            case EsfType.SINGLE_ZERO:
            case EsfType.SINGLE:
                result = new OptimizedFloatNode();
                break;
            default:
                return base.ReadValueNode(reader, typeCode);
            }
            (result as ICodecNode).Decode(reader, typeCode);
            return result;
        }

        #region Array Nodes
        protected override EsfNode ReadArrayNode(BinaryReader reader, EsfType typeCode) {
            EsfNode result;
            // support array types for new primitives
            // this sets the type code of the base type to later have an easier time
            switch (typeCode) {
                case EsfType.BOOL_TRUE_ARRAY:
                case EsfType.BOOL_FALSE_ARRAY:
                case EsfType.UINT_ZERO_ARRAY:
                case EsfType.UINT_ONE_ARRAY:
                case EsfType.INT32_ZERO_ARRAY:
                case EsfType.SINGLE_ZERO_ARRAY:
                // trying to read this should result in an infinite loop
                throw new InvalidDataException(string.Format("Array {0:x} of zero-byte entries makes no sense", typeCode));
                case EsfType.UINT32_BYTE_ARRAY:
                    result = new UIntArrayNode(this) { ItemReader = UIntByteReader };
                    // typeCode = EsfType.UINT32_ARRAY;
                    break;
                case EsfType.UINT32_SHORT_ARRAY:
                    result = new UIntArrayNode(this) { ItemReader = UInt16Reader };
                    // typeCode = EsfType.UINT32_ARRAY;
                    break;
                case EsfType.UINT32_24BIT_ARRAY:
                    result = new UIntArrayNode(this) { ItemReader = UInt24Reader };
                    // typeCode = EsfType.UINT32_ARRAY;
                    break;
                case EsfType.INT32_BYTE_ARRAY:
                    result = new IntArrayNode(this) { ItemReader = IntByteReader };
                    // typeCode = EsfType.INT32_ARRAY;
                    break;
                case EsfType.INT32_SHORT_ARRAY:
                    result = new IntArrayNode(this) { ItemReader = Int16Reader };
                    // typeCode = EsfType.INT32_ARRAY;
                    break;
                case EsfType.INT32_24BIT_ARRAY:
                    result = new IntArrayNode(this) { ItemReader = Int24Reader };
                    // typeCode = EsfType.INT32_ARRAY;
                    break;
                default:
                    result = base.ReadArrayNode(reader, typeCode);
                    return result;
            }
            (result as ICodecNode).Decode(reader, typeCode);
            result.TypeCode = (EsfType) typeCode;
            return result;
        }
        
        protected override byte[] ReadArray(BinaryReader reader) {
            //long targetOffset = ReadSize(reader) + reader.BaseStream.Position;
            long size = ReadSize(reader);
            return reader.ReadBytes((int) size);
        }
        #endregion

        #region Record Nodes
        // Section can now be compressed
        protected override EsfNode ReadRecordNode(BinaryReader reader, byte typeCode) {
            RecordNode node = new RecordNode(this, typeCode);
            node.Decode(reader, EsfType.RECORD);
            // RecordNode result = base.ReadRecordNode(reader, typeCode) as RecordNode;
            if (node.Name == CompressedNode.TAG_NAME) {
                // decompress node
                node = new CompressedNode(this, node);
            }
            return node;
        }
        #endregion
  
        #region Version-dependent overridables ABCA
        public override int ReadSize(BinaryReader reader) {
            byte read = reader.ReadByte();
            long result = 0;
            while ((read & 0x80) != 0) {
                result = (result << 7) + (read & (byte)0x7f);
                read = reader.ReadByte();
            }
            result = (result << 7) + (read & (byte)0x7f);
            // Debug.WriteLine(string.Format("size is {0}, end of size at {1:x}", result, reader.BaseStream.Position));
            return (int) result;
        }
        public override int ReadCount(BinaryReader reader) {
            return ReadSize(reader);
        }
        public override void WriteSize(BinaryWriter writer, long size) {
            if (size == 0) {
                writer.Write((byte)0);
                return;
            }
            byte leftmostBitsClear = 0x80;
            byte leftmostBitsSet = 0x7f;
            
            // store rightmost to leftmost bytes
            Stack<byte> encoded = new Stack<byte>();
            while (size != 0) {
                // only keep 7 leftmost bits
                byte leftmost = (byte)(size & leftmostBitsSet);
                encoded.Push(leftmost);
                // and throw them away from the original
                size = size >> 7;
            }
            // and write them the other way around
            while(encoded.Count != 0) {
                byte write = encoded.Pop();
                write |= (encoded.Count != 0) ? leftmostBitsClear : (byte)0;
                writer.Write(write);
            }
        }
        public override void WriteOffset(BinaryWriter writer, long offset) {
            WriteSize(writer, offset);
        }

        // allow de/encoding of short info (2 byte)
        public override void ReadRecordInfo(BinaryReader reader, byte encoded, out string name, out byte version) {
            // root node (and only root node) is stored with long name/version info...
            if (reader.BaseStream.Position == headerLength + 1 || (encoded & LONG_INFO) != 0) {
                base.ReadRecordInfo(reader, encoded, out name, out version);
            } else {
                // Debug.WriteLine(string.Format("Reading short node info from {0:x}", reader.BaseStream.Position - 1));
                version = (byte)((encoded & 31) >> 1);
                ushort nameIndex = (ushort)((encoded & 1) << 8);
                nameIndex += reader.ReadByte();
                name = GetNodeName(nameIndex);
                // Debug.WriteLine(string.Format("Name {0}, version {1} (ABCA), position now {2:x}", nodeNames[nameIndex], version, reader.BaseStream.Position));
            }
//            if (reader.BaseStream.Position > 0x39cf43 && name.Equals("CAI_MILITARY_REGION_GROUP_REGION_ANALYSIS")) {
//                Console.WriteLine("record found");
//            }
        }
        public override void WriteRecordInfo(BinaryWriter writer, byte typeCode, string name, byte version) {
            ushort nameIndex = GetNodeNameIndex(name);
            // always encode root node with long (4 byte) info
            bool canUseShort = nameIndex != 0;
            // we only have 9 bits for type in short encoding
            canUseShort &= (nameIndex < 0x200);
            // and 4 for version
            canUseShort &= version < 0x10;
            if (canUseShort) {
                ushort shortInfo = encodeShortRecordInfo(typeCode, nameIndex, version);
                byte write = (byte) ((shortInfo >> 8) & 0xff);
                writer.Write(write);
                writer.Write((byte)shortInfo);
            } else {
                switch ((EsfType) typeCode) {
                case EsfType.RECORD:
                    typeCode = (byte)((nameIndex == 0) ? EsfType.RECORD : EsfType.LONG_RECORD);
                    break;
                case EsfType.RECORD_BLOCK:
                    typeCode = (byte) EsfType.LONG_RECORD_BLOCK;
                    break;
                default:
                    throw new InvalidDataException(string.Format("Trying to encode record info for wrong type code {0}", typeCode));
                }
                base.WriteRecordInfo (writer, typeCode, name, version);
            }
        }
        public static ushort encodeShortRecordInfo(byte typeCode, ushort nameIndex, byte version) {
            ushort shortInfo = (ushort) (version << 9); // shift left to leave place for the type
            shortInfo |= nameIndex; // type uses rightmost 9 bits
            shortInfo |= (((EsfType)typeCode == EsfType.RECORD_BLOCK) ? (ushort) (BLOCK_BIT << 8) : (ushort) 0);  // set block bit for record arrays
            shortInfo |= (ushort) (RECORD_BIT << 8);
            return shortInfo;
        }

        public override void EncodeSized(BinaryWriter writer, List<EsfNode> nodes, bool writeCount = false) {
            byte[] encoded;
            MemoryStream bufferStream = new MemoryStream();
            using (BinaryWriter w = new BinaryWriter(bufferStream)) {
                foreach (EsfNode node in nodes) {
                    Encode(w, node);
                }
                encoded = bufferStream.ToArray();
            }
            WriteSize(writer, encoded.LongLength);
            if (writeCount) {
                WriteSize(writer, nodes.Count);
            }
            writer.Write(encoded);
            encoded = null;
            GC.Collect();
        }
        #endregion
    }
}
