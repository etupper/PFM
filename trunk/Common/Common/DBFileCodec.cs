using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
	interface DBFileCodec
	{
        DBFileHeader readHeader();
        // DBFile readDbFile();
        // void writeDbFile(DBFile file);
	}

    public class PackedDbCodec : DBFileCodec {
        static UInt32 GUID_MARKER = BitConverter.ToUInt32(new byte[] { 0xFD, 0xFE, 0xFC, 0xFF }, 0);
        static UInt32 VERSION_MARKER = BitConverter.ToUInt32(new byte[] { 0xFC, 0xFD, 0xFE, 0xFF }, 0);

        PackedFile packedFile;
        public PackedDbCodec(PackedFile file) {
            packedFile = file;
        }
        public DBFileHeader readHeader() {
            DBFileHeader result = new DBFileHeader();
            var reader = new BinaryReader(new MemoryStream(packedFile.Data, false));
            int justForFun = reader.PeekChar();
            byte index = reader.ReadByte();
            result.Version = 0;
            if (index != 1) {
                // I don't think those can actually occur more than once per file
                while (index == 0xFC || index == 0xFD) {
                    var bytes = new List<byte>(4);
                    bytes.Add(index);
                    bytes.AddRange(reader.ReadBytes(3));
                    UInt32 header = BitConverter.ToUInt32(bytes.ToArray(), 0);
                    if (header == GUID_MARKER) {
                        string guid = IOFunctions.readCAString(reader);
                        result.GUID = guid;
                        index = reader.ReadByte();
                    } else if (header == VERSION_MARKER) {
                        result.Version = (byte)reader.ReadInt32();
                        index = reader.ReadByte();
                    } else {
                        throw new InvalidDataException("could not read db file header of " + packedFile.Filepath);
                        // throw new DBFileNotSupportedException(this);
                    }
                }
            }
            result.EntryCount = reader.ReadUInt32();
            return result;
        }
    }
}
