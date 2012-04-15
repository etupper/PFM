using System;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression;

using LzmaDecoder = SevenZip.Compression.LZMA.Decoder;
using LzmaEncoder = SevenZip.Compression.LZMA.Encoder;
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
  
        // Adds readers for optimized values
        public override EsfNode ReadValueNode(BinaryReader reader, EsfType typeCode) {
            ICodecNode result;
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
            result.Decode(reader, typeCode);
            return result as EsfNode;
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
                    result = new UIntArrayNode { Value = ReadArray(reader), ItemReader = UIntByteReader };
                    // typeCode = EsfType.UINT32_ARRAY;
                    break;
                case EsfType.UINT32_SHORT_ARRAY:
                    result = new UIntArrayNode { Value = ReadArray(reader), ItemReader = UInt16Reader };
                    // typeCode = EsfType.UINT32_ARRAY;
                    break;
                case EsfType.UINT32_24BIT_ARRAY:
                    result = new UIntArrayNode { Value = ReadArray(reader), ItemReader = UInt24Reader };
                    // typeCode = EsfType.UINT32_ARRAY;
                    break;
                case EsfType.INT32_BYTE_ARRAY:
                    result = new IntArrayNode { Value = ReadArray(reader), ItemReader = IntByteReader };
                    // typeCode = EsfType.INT32_ARRAY;
                    break;
                case EsfType.INT32_SHORT_ARRAY:
                    result = new IntArrayNode { Value = ReadArray(reader), ItemReader = Int16Reader };
                    // typeCode = EsfType.INT32_ARRAY;
                    break;
                case EsfType.INT32_24BIT_ARRAY:
                    result = new IntArrayNode { Value = ReadArray(reader), ItemReader = Int24Reader };
                    // typeCode = EsfType.INT32_ARRAY;
                    break;
                default:
                    result = base.ReadArrayNode(reader, typeCode);
                    break;
            }
            result.TypeCode = (EsfType) typeCode;
            return result;
        }
        // Size of array now reads item count instead of target position
        protected override void WriteArrayNode(BinaryWriter writer, EsfNode arrayNode) {
            byte[] encoded;
            // it doesn't really make sense to have length-encoded arrays of 0-byte entries
            switch (arrayNode.TypeCode) {
                // use optimized encoding for the appropriate types
                case EsfType.INT32_BYTE_ARRAY:
                case EsfType.INT32_SHORT_ARRAY:
                case EsfType.INT32_24BIT_ARRAY:
                case EsfType.INT32_ARRAY:
                    encoded = (arrayNode as IntArrayNode).Value;
                    //foreach (int i in intArray) {
                    //    minBytes = Math.Max(minBytes, RelevantBytesInt(i));
                    //}
                    //// optimized int starts at 0x1a, then 0x1b, 0x1c; array adds 0x40
                    //typeCode = (byte)((minBytes == 4) ? (byte) EsfType.INT32_ARRAY : (0x40 + 0x1a + minBytes - 1));
                    //if (typeCode != (byte)EsfType.INT32_ARRAY && typeCode < (byte)EsfType.INT32_BYTE_ARRAY) {
                    //    throw new InvalidDataException();
                    //}
                    //ValueWriter<int> valueWriter = FromRelevantBytesInt(minBytes);
                    //encoded = EncodeArrayNode<int>(intArray, valueWriter);
                    break;
                case EsfType.UINT32_BYTE_ARRAY:
                case EsfType.UINT32_SHORT_ARRAY:
                case EsfType.UINT32_24BIT_ARRAY:
                case EsfType.UINT32_ARRAY:
                    encoded = (arrayNode as UIntArrayNode).Value;
//                    foreach (uint i in array) {
//                        minBytes = Math.Max(minBytes, RelevantBytesUInt(i));
//                    }
//                    // optimized uint starts at 0x16, then 0x17, 0x18; array adds 0x40
//                    typeCode = (byte)((minBytes == 4) ? (byte)EsfType.UINT32_ARRAY : (0x40 + 0x16 + minBytes - 1));
//                    ValueWriter<uint> uintWriter = FromRelevantBytesUInt(minBytes);
//                    encoded = EncodeArrayNode<uint>(array, uintWriter);
                    break;
                default:
                    base.WriteArrayNode(writer, arrayNode);
                    return;
            }
            writer.Write((byte)arrayNode.TypeCode);
            WriteSize(writer, encoded.Length);
            writer.Write(encoded);
        }
        
        protected override byte[] ReadArray(BinaryReader reader) {
            //long targetOffset = ReadSize(reader) + reader.BaseStream.Position;
            long size = ReadSize(reader);
            return reader.ReadBytes((int) size);
        }
        #endregion

        #region Record Nodes
        // Section can now be compressed
        protected override EsfNode CreateRecordNode(string name, byte version, List<EsfNode> childNodes) {
            NamedNode result = base.CreateRecordNode(name, version, childNodes) as NamedNode;
            if (name == CompressedNode.TAG_NAME) {
                // decompress node
                result = Decompress(result) as NamedNode;
            }
            return result;
        }
        protected override void WriteRecordNode(BinaryWriter writer, EsfNode node) {
            if (node is CompressedNode) {
                Compress(writer, node as CompressedNode);
            } else {
                base.WriteRecordNode(writer, node);
            }
        }
        #endregion
  
        #region Record Block Nodes
        protected override void WriteRecordArrayNode(BinaryWriter writer, EsfNode node) {
            NamedNode recordBlockNode = node as NamedNode;
            // Debug.WriteLine(string.Format("Writing record array node {0}", node));
            ushort nameIndex = (ushort)nodeNames.IndexOfValue(recordBlockNode.Name);
            WriteRecordInfo(writer, 0x81, nameIndex, recordBlockNode.Version);
            byte[] encodedContents;
            MemoryStream stream = new MemoryStream();
            using (BinaryWriter w = new BinaryWriter(stream)) {
                foreach (EsfNode child in recordBlockNode.AllNodes) {
                    EncodeSized(w, (child as NamedNode).AllNodes);
                }
                encodedContents = stream.ToArray();
            }
            WriteSize(writer, encodedContents.LongLength);
            WriteSize(writer, recordBlockNode.AllNodes.Count);
            writer.Write(encodedContents);
        }
        #endregion

        #region Compression
        //re-compress node
        void Compress(BinaryWriter writer, CompressedNode node) {
            // encode the node into bytes
            byte[] data;
            MemoryStream uncompressedStream = new MemoryStream();
            using (BinaryWriter w = new BinaryWriter(uncompressedStream)) {
                // use the node's own codec or we'll mess up the string lists
                node.Codec.EncodeRootNode(w, node.RootNode);
                data = uncompressedStream.ToArray();
            }
            uint uncompressedSize = (uint) data.LongLength;
            
            // compress the encoded data
            MemoryStream outStream = new MemoryStream();
            LzmaEncoder encoder = new LzmaEncoder();
            using (uncompressedStream = new MemoryStream(data)) {
                encoder.Code(uncompressedStream, outStream, data.Length, long.MaxValue, null);
                data = outStream.ToArray();
            }
   
            // prepare decoding information
            List<EsfNode> infoItems = new List<EsfNode>();
            infoItems.Add(new UIntNode { Value = uncompressedSize, TypeCode = EsfType.UINT32, Codec = this });
            using (MemoryStream propertyStream = new MemoryStream()) {
                encoder.WriteCoderProperties(propertyStream);
                infoItems.Add(new ByteArrayNode { Value = propertyStream.ToArray(), TypeCode = EsfType.UINT8_ARRAY, Codec = this });
            }
            // put together the items expected by the unzipper
            List<EsfNode> dataItems = new List<EsfNode>();
            dataItems.Add(new ByteArrayNode { Value = data, TypeCode = EsfType.UINT8_ARRAY, Codec = this });
            dataItems.Add(new NamedNode { Name = CompressedNode.INFO_TAG, Value = infoItems, TypeCode = EsfType.RECORD, Codec = this });
            NamedNode compressedNode = new NamedNode { Name = CompressedNode.TAG_NAME, Value = dataItems, TypeCode = EsfType.RECORD, Codec = this };
            
            // and finally encode
            Encode(writer, compressedNode);
        }

        // unzip contained 7zip node
        EsfNode Decompress(NamedNode compressedNode) {
            byte[] data = (compressedNode.Values[0] as EsfValueNode<byte[]>).Value;
            NamedNode infoNode = compressedNode.Children[0];
            uint size = (infoNode.Values[0] as EsfValueNode<uint>).Value;
            byte[] decodeProperties = (infoNode.Values[1] as EsfValueNode<byte[]>).Value;
            LzmaDecoder decoder = new LzmaDecoder();
            decoder.SetDecoderProperties(decodeProperties);

            using (Stream inStream = new MemoryStream(data, false), file = File.OpenWrite("decompressed_section.esf")) {
                decoder.Code(inStream, file, data.Length, size, null);
                file.Write(data, 0, data.Length);
            }

            byte[] outData = new byte[size];
            using (MemoryStream inStream = new MemoryStream(data, false), outStream = new MemoryStream(outData)) {
                decoder.Code(inStream, outStream, data.Length, size, null);
                outData = outStream.ToArray();
            }
            EsfNode result;
            AbcaFileCodec codec = new AbcaFileCodec();
            NodeRead eventDelegator = CreateEventDelegate();
            if (eventDelegator != null) {
                codec.NodeReadFinished += eventDelegator;
            }
            using (BinaryReader reader = new BinaryReader(new MemoryStream(outData))) {
                result = codec.Parse(reader);
            }
            if (eventDelegator != null) {
                codec.NodeReadFinished -= eventDelegator;
            }
            return new CompressedNode { 
                Name = CompressedNode.TAG_NAME, 
                RootNode = result, 
                TypeCode = EsfType.RECORD, 
                Version = compressedNode.Version,
                Codec = codec
            };
        }
        #endregion
  
        #region Version-dependent overridables ABCA
        protected override long ReadSize(BinaryReader reader) {
            byte read = reader.ReadByte();
            long result = 0;
            while ((read & 0x80) != 0) {
                result = (result << 7) + (read & (byte)0x7f);
                read = reader.ReadByte();
            }
            result = (result << 7) + (read & (byte)0x7f);
            // Debug.WriteLine(string.Format("size is {0}, end of size at {1:x}", result, reader.BaseStream.Position));
            return result;
        }
        protected override void WriteSize(BinaryWriter writer, long size) {
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
        protected override void WriteOffset(BinaryWriter writer, long offset) {
            WriteSize(writer, offset);
        }

        // allow de/encoding of short info (2 byte)
        protected override void ReadRecordInfo(BinaryReader reader, byte encoded, out string name, out byte version) {
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
        protected override void WriteRecordInfo(BinaryWriter writer, byte typeCode, ushort nameIndex, byte version) {
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
                base.WriteRecordInfo (writer, typeCode, nameIndex, version);
            }
        }
        public static ushort encodeShortRecordInfo(byte typeCode, ushort nameIndex, byte version) {
            ushort shortInfo = (ushort) (version << 9); // shift left to leave place for the type
            shortInfo |= nameIndex; // type uses rightmost 9 bits
            shortInfo |= (((EsfType)typeCode == EsfType.RECORD_BLOCK) ? (ushort) (BLOCK_BIT << 8) : (ushort) 0);  // set block bit for record arrays
            shortInfo |= (ushort) (RECORD_BIT << 8);
            return shortInfo;
        }

        // all offsets are now relative sizes
        protected override List<EsfNode> ReadToOffset(BinaryReader reader, long targetOffset) {
            return base.ReadToOffset(reader, reader.BaseStream.Position + targetOffset);
        }
        protected override void EncodeSized(BinaryWriter writer, List<EsfNode> nodes) {
            byte[] encoded;
            MemoryStream bufferStream = new MemoryStream();
            using (BinaryWriter w = new BinaryWriter(bufferStream)) {
            foreach (EsfNode node in nodes) {
                Encode(w, node);
            }
                encoded = bufferStream.ToArray();
            }
            WriteSize(writer, encoded.LongLength);
            writer.Write(encoded);
        }
        #endregion
    }
}
