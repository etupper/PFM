using System;
using System.IO;
using System.Collections.Generic;

namespace EsfLibrary {
    public class CodecTest {
        TestableAbcaCodec codec = new TestableAbcaCodec();
        
        public CodecTest() {
            codec.AddNodeName(0, "root");
            codec.AddNodeName(1, "test");
        }

        public void run() {
            testEquals();
            testIntCodec();
            testUIntCodec();
            testUIntArrayCodec();
            testRecordNode();
            testRecordArrayNode();
            Console.WriteLine("All tests successful");
            Console.ReadKey();
        }
        
        public void testRecordArrayNode() {
            List<EsfNode> records = new List<EsfNode>();
            for (int i = 0; i < 5; i++) {
                RecordEntryNode entry = new RecordEntryNode(codec) {
                    Name = "test - " + i,
                    Value = createSomeNodes()
                };
                records.Add(entry);
            }
            RecordArrayNode array = new RecordArrayNode(codec, (byte) EsfType.RECORD_BLOCK) {
                Name = "test",
                Value = records
            };
            verifyEncodeDecode(array);
        }
        
        public void testRecordNode() {
            RecordNode node = new RecordNode(codec, (byte)EsfType.RECORD) {
                Name = "test",
                Value = createSomeNodes()
            };
            verifyEncodeDecode(node);
        }

        private void verifyEncodeDecode(EsfNode node) {
            List<EsfNode> singleNode = new List<EsfNode>();
            singleNode.Add(node);
            verifyEncodeDecode(singleNode);
        }
        private void verifyEncodeDecode(List<EsfNode> nodes) {
            RecordNode rootNode = createRootNode();
            rootNode.Value = nodes;
            EsfFile encodedFile = new EsfFile(rootNode, codec);
            MemoryStream stream = new MemoryStream();
            byte[] encoded;
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                codec.EncodeRootNode(writer, rootNode);
                encoded = stream.ToArray();
            }
            EsfNode decoded;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(encoded))) {
                decoded = codec.Parse (reader);
            }
            assertEqual(rootNode, decoded);
            // EsfNode decodedResult = (decoded as ParentNode).AllNodes[0];
            for(int i = 0; i < rootNode.AllNodes.Count; i++) {
                assertEqual(rootNode.AllNodes[i], (decoded as ParentNode).AllNodes[i]);
            }
//            nodes.ForEach(node => {assertEqual(node, decodedResult); });
        }
        
        private RecordNode createRootNode() {
            return new RecordNode(codec, (byte) EsfType.RECORD) { Name = "root" };
        }
        private List<EsfNode> createSomeNodes() {
            List<EsfNode> list = new List<EsfNode>();
            list.Add(new UIntNode { Value = 1, Codec = codec, TypeCode = EsfType.UINT32 });
            list.Add(new UIntNode { Value = 2, Codec = codec, TypeCode = EsfType.UINT32 });
            list.Add(new UIntNode { Value = 3, Codec = codec, TypeCode = EsfType.UINT32 });
            list.Add(new UIntNode { Value = 4, Codec = codec, TypeCode = EsfType.UINT32 });
            list.Add(new UIntNode { Value = 5, Codec = codec, TypeCode = EsfType.UINT32 });
            return list;
        }
        
        public void testUIntArrayCodec() {
            byte[] array = { 0, 1, 2, 3, 4, 5 };
            //List<EsfNode> nodes = new List<EsfNode>();
            UIntArrayNode node = new UIntArrayNode(codec) { Value = array, TypeCode = EsfType.UINT32_ARRAY };
            verifyEncodeDecode(node);
        }
        
        public void testEquals() {
            EsfNode valueNode = new IntNode { Value = 1 };
            EsfNode valueNode2 = new IntNode { Value = 1 };
            assertEqual(valueNode, valueNode2);
            
            List<EsfNode> nodeList1 = new List<EsfNode>();
            nodeList1.Add(valueNode);
            List<EsfNode> nodeList2 = new List<EsfNode>();
            nodeList2.Add(valueNode);
            RecordNode node = new RecordNode(null) { Name = "name", Value = nodeList1 };
            EsfNode node2 = new RecordNode(null) { Name = "nodename", Value = nodeList2 };
            assertNotEqual(node, node2);
            node = new RecordNode(null) { Name = "nodename", Value = nodeList1 };
            assertEqual(node, node2);
            
            AbcaFileCodec codec = new AbcaFileCodec();
            EsfFile file = new EsfFile(node, codec);
            AbceCodec codec2 = new AbceCodec();
            EsfFile file2 = new EsfFile(node2, codec2);
            assertNotEqual(file, file2);
            file2.Codec = codec;
            assertEqual(file, file2);
        }
        
        private EsfValueNode<int> testIntNode(int val, EsfType expectedTypeCode = EsfType.INVALID) {
            EsfValueNode<int> node = new OptimizedIntNode { Value = val };
            return testNode(node, expectedTypeCode);
        }
        private void testUIntNode(uint val, EsfType typeCode = EsfType.UINT32) {
            EsfValueNode<uint> node = new OptimizedUIntNode { Value = val, TypeCode = typeCode };
            testNode(node);
        }

        private EsfValueNode<T> testNode<T>(EsfValueNode<T> node, EsfType expectedTypeCode = EsfType.INVALID) {
            byte[] data = encodeNode(node);
            EsfNode decoded = decodeNode(data);
            EsfValueNode<T> node2 = decoded as EsfValueNode<T>;
            assertEqual(node, node2);
            if (expectedTypeCode != EsfType.INVALID) {
                assertEqual(node2.TypeCode, expectedTypeCode);
            }
            encodeNode(node2);
            return node2;
        }
        byte[] encodeNode(EsfNode node) {
            MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                codec.Encode(writer, node);
                return stream.ToArray();
            }
        }
        EsfNode decodeNode(byte[] data) {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data))) {
                return codec.Decode(reader);
            }
        }
        
        #region Asserts
        public void assert(bool toValidate, bool expected) {
            if (toValidate != expected) {
                Console.WriteLine("Validation failed");
                throw new InvalidOperationException("Validation failed");
            }
        }
        public void assertFalse(bool toValidate) {
            assert(toValidate, false);
        }
        public void assertTrue(bool toValidate) {
            assert (toValidate, true);
        }
        public void assertEqual(object o1, object o2) {
            if (!o1.Equals(o2)) {
                Console.WriteLine("Validation failed");
                throw new InvalidOperationException("Validation failed");
            }
        }
        public void assertNotEqual(object o1, object o2) {
            if (o1.Equals(o2)) {
                Console.WriteLine("Validation failed");
                throw new InvalidOperationException("Validation failed");
            }
        }
        #endregion
        
        #region Old Tests
        public void testIntCodec() {
            testIntNode(0);
            testIntNode(1);
            testIntNode(0x100);
            testIntNode(181);
            testIntNode(0x10000);
            testIntNode(1573280);
            testIntNode(0x1000000);
            testIntNode(int.MaxValue);
            testIntNode(-1, EsfType.INT32_BYTE);
            testIntNode(-0xff);
            testIntNode(-0xffff);
            testIntNode(-0xffffff);
            testIntNode(-11831522);
            testIntNode(int.MinValue);
        }

        public void testUIntCodec() {
            testUIntNode(0);
            testUIntNode(1);
            testUIntNode(480, EsfType.UINT32_SHORT);
            testUIntNode(0x100);
            testUIntNode(0x10000);
            testUIntNode(0x1000000);
            testUIntNode(uint.MaxValue);
        }
        #endregion
    }
    
    
    public class TestableAbcaCodec : AbcaFileCodec {
        public void AddNodeName(int index, string name) {
            nodeNames.Add(index, name);
        }
    }
}

