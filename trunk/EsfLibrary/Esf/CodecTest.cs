using System;
using System.IO;
using System.Collections.Generic;

namespace EsfLibrary {
    public class CodecTest {
        AbcaFileCodec codec = new AbcaFileCodec();
        public void run() {
            testIntCodec();
            testUIntCodec();
            //testUIntArrayCodec();
            testEquals();
            Console.WriteLine("All tests successful");
            Console.ReadKey();
        }
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
        public void testEquals() {
            EsfNode valueNode = new IntNode { Value = 1 };
            EsfNode valueNode2 = new IntNode { Value = 1 };
            assertEqual(valueNode, valueNode2);
            
            List<EsfNode> nodeList1 = new List<EsfNode>();
            nodeList1.Add(valueNode);
            List<EsfNode> nodeList2 = new List<EsfNode>();
            nodeList2.Add(valueNode);
            NamedNode node = new NamedNode { Name = "name", Value = nodeList1 };
            NamedNode node2 = new NamedNode { Name = "nodename", Value = nodeList2 };
            assertNotEqual(node, node2);
            node.Name = "nodename";
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
    }
}

