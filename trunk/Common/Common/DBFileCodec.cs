using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common {
    public class DBFileHeaderCodec {
        static UInt32 GUID_MARKER = BitConverter.ToUInt32 (new byte[] { 0xFD, 0xFE, 0xFC, 0xFF}, 0);
        static UInt32 VERSION_MARKER = BitConverter.ToUInt32 (new byte[] { 0xFC, 0xFD, 0xFE, 0xFF}, 0);

        public DBFileHeader readHeader(BinaryReader reader) {
            byte index = reader.ReadByte ();
            int version = 0;
            string guid = "";

            if (index != 1) {
                // I don't think those can actually occur more than once per file
                while (index == 0xFC || index == 0xFD) {
                    var bytes = new List<byte> (4);
                    bytes.Add (index);
                    bytes.AddRange (reader.ReadBytes (3));
                    UInt32 marker = BitConverter.ToUInt32 (bytes.ToArray (), 0);
                    if (marker == GUID_MARKER) {
                        guid = IOFunctions.readCAString (reader);
                    } else if (marker == VERSION_MARKER) {
                        version = (byte)reader.ReadInt32 ();
                    } else {
                        throw new DBFileNotSupportedException (string.Format ("could not interpret {0}", marker));
                    }
                    index = reader.ReadByte ();
                }
            }
            uint entryCount = reader.ReadUInt32 ();
            return new DBFileHeader (guid, version, entryCount);
        }
        public void writeHeader(BinaryWriter writer, DBFileHeader header) {
            if (header.GUID != "") {
                writer.Write (GUID_MARKER);
                writer.Write (VERSION_MARKER);
                writer.Write (header.Version);
            }
            writer.Write ((byte)1);
            writer.Write (header.EntryCount);
        }
    }

    public class DBFileCodec {
        public DBFile readDbFile(BinaryReader reader, List<TypeInfo> infos) {
            DBFileHeaderCodec headerReader = new DBFileHeaderCodec ();
            DBFileHeader header = headerReader.readHeader (reader);
            DBFile file = new DBFile (header);

            for (int i = 0; i < header.EntryCount; i++) {
                file.Entries.Add (readFields (reader, file.CurrentType));
            }
            return file;
        }

        // creates a list of field values from the given type.
        // stream needs to be positioned at the beginning of the entry.
        public List<FieldInstance> readFields(BinaryReader reader, TypeInfo ttype) {
            List<FieldInstance> entry = new List<FieldInstance> ();
            for (int i = 0; i < ttype.fields.Count; ++i) {
                FieldInfo field = ttype.fields [i];

                try {
                    string value = getFieldValue (reader, field);
                    entry.Add (new FieldInstance (field, value));

                    switch (field.modifier) {
                    case FieldInfo.Modifier.NextFieldIsConditional:
                        if (!field.TestConditionalValue (value)) {
                            for (int j = 1; j <= field.length; ++j)
                                entry.Add (new FieldInstance (ttype.fields [i + j], ""));
                            i += field.length;
                        }
                        break;

                    case FieldInfo.Modifier.NextFieldRepeats:
                        int repeatCount = Convert.ToInt32 (value);
                        string[] repeatValues = new string[repeatCount];
                        for (int j = 0; j < repeatCount; ++j)
                            repeatValues [j] = getFieldValue (reader, ttype.fields [i + 1]);
                        entry.Add (new FieldInstance (ttype.fields [i + 1], String.Join (", ", repeatValues)));
                        ++i;
                        break;
                    }
                } catch (Exception x) {
                    throw new InvalidDataException (string.Format ("Failed to read field {0}/{1} ({2})", i, ttype.fields.Count, x.Message));
                }
            }
            return entry;
        }

        public void writeFields(BinaryWriter writer, DBFile file) {
            foreach (List<FieldInstance> entry in file.Entries) {
                writeEntry (writer, entry);
            }
        }

        private void writeEntry(BinaryWriter writer, List<FieldInstance> fields) {
            for (int i = 0; i < fields.Count; i++) {
                FieldInstance field = fields [i];
                writeFieldValuePair (writer, field.Info, field.Value);
                switch (field.Info.Mod) {
                case FieldInfo.Modifier.NextFieldIsConditional:
                    if (!field.Info.TestConditionalValue (field.Value)) {
                        // if the condition is false, all the 'n/a' fields are skipped
                        i += field.Info.length;
                    }
                    break;

                case FieldInfo.Modifier.NextFieldRepeats:
                    int repeatCount = Convert.ToInt32 (field.Value);
                    string[] repeatValues = fields [i + 1].Value.Split (",".ToCharArray ());
                    for (int j = 0; j < repeatCount; ++j) {
                        writeFieldValuePair (writer, fields [i + 1].Info, repeatValues [j]);
                    }

                    ++i;
                    break;
                }
            }
        }

        private void writeFieldValuePair(BinaryWriter writer, FieldInfo field, string value) {
            switch (field.type) {
            case TypeCode.Boolean:
                writer.Write (Convert.ToBoolean (value));
                break;
            case TypeCode.UInt16:
                writer.Write (Convert.ToUInt16 (value));
                break;
            case TypeCode.UInt32:
                writer.Write (Convert.ToUInt32 (value));
                break;
            case TypeCode.UInt64:
                writer.Write (Convert.ToUInt64 (value));
                break;
            case TypeCode.Int16:
                writer.Write (Convert.ToInt16 (value));
                break;
            case TypeCode.Int32:
                writer.Write (Convert.ToInt32 (value));
                break;
            case TypeCode.Int64:
                writer.Write (Convert.ToInt64 (value));
                break;
            case TypeCode.Single:
                writer.Write (Convert.ToSingle (value));
                break;
            case TypeCode.Double:
                writer.Write (Convert.ToDouble (value));
                break;

            case TypeCode.String:
                IOFunctions.writeCAString (writer, value);
                break;

            case TypeCode.Empty:
                    // convert hex string back to individual bytes
                string[] hexBytes = value.Split (" ".ToCharArray (), StringSplitOptions.RemoveEmptyEntries);
                foreach (string hexByte in hexBytes)
                    writer.Write (Convert.ToByte (hexByte, 16));
                break;

            default:
                throw new InvalidDataException ("unknown field type");
            }
        }

        // retrieve a value from the reader at the current position.
        public static string getFieldValue(BinaryReader reader, FieldInfo field) {
            switch (field.type) {
            case TypeCode.Boolean:
                return Convert.ToBoolean (reader.ReadByte()).ToString();
            case TypeCode.UInt16:
                return reader.ReadUInt16().ToString();
            case TypeCode.UInt32:
                return reader.ReadUInt32().ToString();
            case TypeCode.UInt64:
                return reader.ReadUInt64().ToString();
            case TypeCode.Int16:
                return reader.ReadInt16().ToString();
            case TypeCode.Int32:
                return reader.ReadInt32().ToString();
            case TypeCode.Int64:
                return reader.ReadInt64().ToString();
            case TypeCode.Single:
                return reader.ReadSingle().ToString();
            case TypeCode.Double:
                return reader.ReadDouble().ToString();
            case TypeCode.String:
                return IOFunctions.readCAString (reader);

            case TypeCode.Empty:
                byte[] data = reader.ReadBytes (field.length);
                StringBuilder dataString = new StringBuilder();
                for (int k = 0; k < data.Length; ++k)
                    dataString.AppendFormat ("{0:X2} ", data [k]);
                return dataString.ToString();

            default:
                throw new InvalidDataException ("unknown field type");
            }
        }
    }
}
