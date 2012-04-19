using System;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression;

using LzmaDecoder = SevenZip.Compression.LZMA.Decoder;
using LzmaEncoder = SevenZip.Compression.LZMA.Encoder;

namespace EsfLibrary {
    public class CompressedNode : RecordNode {
        public CompressedNode(EsfCodec codec, RecordNode rootNode) : base(codec) {
            Name = TAG_NAME;
            RootNode = Decompress(rootNode);
        }

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
        public void Decode(BinaryReader reader) {
            throw new NotImplementedException ();
        }

        // unzip contained 7zip node
        EsfNode Decompress(RecordNode compressedNode) {
            byte[] data = (compressedNode.Values[0] as EsfValueNode<byte[]>).Value;
            ParentNode infoNode = compressedNode.Children[0];
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
//            NodeRead eventDelegator = CreateEventDelegate();
//            if (eventDelegator != null) {
//                codec.NodeReadFinished += eventDelegator;
//            }
            using (BinaryReader reader = new BinaryReader(new MemoryStream(outData))) {
                result = codec.Parse(reader);
            }
//            if (eventDelegator != null) {
//                codec.NodeReadFinished -= eventDelegator;
//            }
            return result;
        }
        
        //re-compress node
        public override void Encode(BinaryWriter writer) {
            // encode the node into bytes
            byte[] data;
            MemoryStream uncompressedStream = new MemoryStream();
            using (BinaryWriter w = new BinaryWriter(uncompressedStream)) {
                // use the node's own codec or we'll mess up the string lists
                //Codec.EncodeRootNode(w, RootNode);
                RootNode.Codec.EncodeRootNode(w, RootNode);
                // (RootNode as ICodecNode).Encode(w);
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
            infoItems.Add(new UIntNode { Value = uncompressedSize, TypeCode = EsfType.UINT32, Codec = Codec });
            using (MemoryStream propertyStream = new MemoryStream()) {
                encoder.WriteCoderProperties(propertyStream);
                infoItems.Add(new ByteArrayNode(Codec) { Value = propertyStream.ToArray(), TypeCode = EsfType.UINT8_ARRAY });
            }
            // put together the items expected by the unzipper
            List<EsfNode> dataItems = new List<EsfNode>();
            dataItems.Add(new ByteArrayNode(Codec) { Value = data, TypeCode = EsfType.UINT8_ARRAY });
            dataItems.Add(new RecordNode(Codec)  { Name = CompressedNode.INFO_TAG, Value = infoItems });
            RecordNode compressedNode = new RecordNode(Codec) { Name = CompressedNode.TAG_NAME, Value = dataItems };
            
            // and finally encode
            compressedNode.Encode(writer);
        }
    }
}

