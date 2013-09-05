namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class IOFunctions
    {
        public static string TSV_FILTER = "TSV Files (*.csv,*.tsv)|*.csv;*.tsv|Text Files (*.txt)|*.txt|All Files|*.*";
        public static string PACKAGE_FILTER = "Package File (*.pack)|*.pack|Any File|*.*";

        public static string readCAString(BinaryReader reader) {
            return readCAString(reader, Encoding.Unicode);
        }
        public static string readCAString(BinaryReader reader, Encoding encoding) {
            int num = reader.ReadInt16();
            int bytes = num * (encoding.IsSingleByte ? 1 : 2);
            return new string(encoding.GetChars(reader.ReadBytes(bytes)));
        }
        
        public static string readStringContainer(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(0x200);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; bytes[i] != 0; i += 2)
            {
                builder.Append(Encoding.Unicode.GetChars(bytes, i, 2));
            }
            return builder.ToString();
        }

        public static string readZeroTerminatedAscii(BinaryReader reader) {
            StringBuilder builder2 = new StringBuilder();
            byte ch2 = reader.ReadByte();
            while (ch2 != '\0') {
                builder2.Append((char) ch2);
                ch2 = reader.ReadByte();
            }
            return builder2.ToString();
        }
        
        public static void WriteZeroTerminatedAscii(BinaryWriter writer, string toWrite) {
            writer.Write(toWrite.ToCharArray());
            writer.Write((byte) 0);
        }
  
        public static void writeCAString(BinaryWriter writer, string value) {
            writeCAString (writer, value, Encoding.Unicode);
        }
        public static void writeCAString(BinaryWriter writer, string value, Encoding encoding) {
            writer.Write((ushort) value.Length);
            writer.Write(encoding.GetBytes(value));
        }

        public static void writeStringContainer(BinaryWriter writer, string value)
        {
            byte[] array = new byte[0x200];
            Encoding.Unicode.GetBytes(value).CopyTo(array, 0);
            writer.Write(array);
        }

        public static void FillList<T>(List<T> toFill, ItemReader<T> readItem, BinaryReader reader, 
                                          bool skipIndex = true, int itemCount = -1) {
            try {

#if DEBUG
                long listStartPosition = reader.BaseStream.Position;
#endif
                if (itemCount == -1) {
                    itemCount = reader.ReadInt32();
                }
#if DEBUG
                Console.WriteLine("Reading list at {0:x}, {1} entries", listStartPosition, itemCount);
#endif
                for (int i = 0; i < itemCount; i++) {
                    try {
                        if (skipIndex) {
                            reader.ReadInt32();
                        }
                        toFill.Add(readItem(reader));
                    } catch (Exception ex) {
                        throw new ParseException(string.Format("Failed to read item {0}", i), 
                                                 reader.BaseStream.Position, ex);
                    }
                }
            } catch (Exception ex) {
                throw new ParseException(string.Format("Failed to entries for list {0}"), 
                                         reader.BaseStream.Position, ex);
            }
        }
        
        public delegate T ItemReader<T>(BinaryReader reader);
    }
}

