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

        public static string readCAString(BinaryReader reader)
        {
            int num = reader.ReadInt16();
            return new string(Encoding.Unicode.GetChars(reader.ReadBytes(num * 2)));
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

        public static void writeCAString(BinaryWriter writer, string value)
        {
            writer.Write((ushort) value.Length);
            writer.Write(Encoding.Unicode.GetBytes(value));
        }

        public static void writeStringContainer(BinaryWriter writer, string value)
        {
            byte[] array = new byte[0x200];
            Encoding.Unicode.GetBytes(value).CopyTo(array, 0);
            writer.Write(array);
        }
    }
}

