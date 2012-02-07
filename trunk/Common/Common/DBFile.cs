using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Common.Properties;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics;

namespace Common {
    public class DBFileHeader {
        public string GUID { get; set; }
        public int Version { get; set; }
        public uint EntryCount { get; set; }
    }
    public class DBFile {
        static UInt32 GUID_MARKER = BitConverter.ToUInt32(new byte[] { 0xFD, 0xFE, 0xFC, 0xFF }, 0);
        static UInt32 VERSION_MARKER = BitConverter.ToUInt32(new byte[] { 0xFC, 0xFD, 0xFE, 0xFF }, 0);

        public PackedFile PackedFile {
            get { return packedFile; }
        }

        public string GUID;
        private int headerVersion;
        uint entryCount;

        private List<List<FieldInstance>> entries;
        private TypeInfo typeInfo;
        private PackedFile packedFile;
        public int TotalwarHeaderVersion {
            get { return headerVersion; }
            set {
                headerVersion = value;
//                typeInfo = type[value];
            }
        }
        // private readonly TypeInfo[] type = new TypeInfo[Settings.Default.totalwarHeaderVersions];

        public DBFile (PackedFile packedFile, string type, bool readData = true) {
			this.packedFile = packedFile;
			BinaryReader reader = readHeader (packedFile);
			int i = 0;
			if (readData) {
                // this.type = type;
				try {
					this.typeInfo = DBTypeMap.Instance [type, TotalwarHeaderVersion];
                    if (typeInfo == null) {
                        // find the next-lower version...
                        //						for (i = TotalwarHeaderVersion-1; i >= 0; i--) {
                        //							typeInfo = type[i];
                        //							if (typeInfo!= null) {
                        //								break;
                        //							}
                        //						}
                    }
                    this.entries = new List<List<FieldInstance>>();
                    for (i = 0; i < entryCount; i++) {
                        List<FieldInstance> entry = readEntry(reader, 0, this.typeInfo.fields.Count);
                        if (entry.Count != typeInfo.fields.Count) {
                            throw new DBFileNotSupportedException(
                                string.Format("Only read {0}/{1} fields for {3} (file offset {4}), previously read {2} entries",
                                entry.Count, typeInfo.fields.Count, entries.Count, packedFile.Filepath, packedFile.offset), this);
                        }
                        this.entries.Add(entry);
                    }
                } catch (DBFileNotSupportedException) {
                    throw;
                } catch (Exception x) {
                    throw new DBFileNotSupportedException(
                        string.Format("Table {4} at {5} has an unexpected format: " +
                            "failure to read field {2}. " +
                            "DB file version is {0}, we can handle up to version {1}.\n{3}",
                        TotalwarHeaderVersion, type.Length - 1, i, x, packedFile.Filepath, packedFile.offset), this);
                }
            }
        }
        int countNoModifier(List<FieldInstance> fields) {
            int i = 0;
            foreach (FieldInstance field in fields) {
                i++;
                if (field.Info.modifier == FieldInfo.Modifier.NextFieldIsConditional && field.Value == Boolean.FalseString) {
                    i++;
                }
            }
            return i;
        }
        public DBFile(DBFile toCopy) {
            typeInfo = toCopy.typeInfo;
            TotalwarHeaderVersion = toCopy.TotalwarHeaderVersion;
            //type = toCopy.type;
            GUID = toCopy.GUID;
            toCopy.entries.ForEach(entry => entries.Add(new List<FieldInstance>(entry)));
        }

        private BinaryReader readHeader(PackedFile packedFile) {
            var reader = new BinaryReader(new MemoryStream(packedFile.Data, false));
            int justForFun = reader.PeekChar();
            byte index = reader.ReadByte();
            int version = 0;
            if (index != 1) {
                // I don't think those can actually occur more than once per file
                while (index == 0xFC || index == 0xFD) {
                    var bytes = new List<byte>(4);
                    bytes.Add(index);
                    bytes.AddRange(reader.ReadBytes(3));
                    UInt32 header = BitConverter.ToUInt32(bytes.ToArray(), 0);
                    if (header == GUID_MARKER) {
                        string guid = IOFunctions.readCAString(reader);
                        GUID = guid;
                        index = reader.ReadByte();
                    } else if (header == VERSION_MARKER) {
                        version = (byte)reader.ReadInt32();
                        index = reader.ReadByte();
                    } else {
                        throw new DBFileNotSupportedException(this);
                    }
                }
            }
            headerVersion = version;
            entryCount = reader.ReadUInt32();
            return reader;
        }

        private TypeInfo GetTypeInfo(string dbName) {
            string delimitedFields = string.Empty;
            const string dbSchemaPath = @"DB.xsd";
            using (var stream = new FileStream(dbSchemaPath, FileMode.Open, FileAccess.Read)) {
                var reader = XmlReader.Create(stream);
                var root = XElement.Load(reader);
                var nameTable = reader.NameTable;
                var namespaceManager = new XmlNamespaceManager(nameTable);
                namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
                var xpathQuery = string.Format("./xs:complexType[@name=\"{0}\"]", dbName);
                var tableNode = root.XPathSelectElement(xpathQuery, namespaceManager);

                if (tableNode == null) {
                    throw new DBFileNotSupportedException("Unknown table.", this);
                }

                var fields = tableNode.XPathSelectElements("./xs:attribute", namespaceManager);
                foreach (var field in fields) {
                    var nameAttribute = field.Attribute("name");
                    var xsTypeAttribute = field.Attribute("type");

                    if (nameAttribute != null) delimitedFields += nameAttribute.Value;
                    delimitedFields += ",";
                    if (xsTypeAttribute != null) delimitedFields += xsTypeAttribute.Value.Replace("xs:", "");
                    delimitedFields += ";";
                }
            }
            var typeInfo = new TypeInfo(dbName, delimitedFields);

            return typeInfo;
        }

        private List<FieldInstance> readEntry(BinaryReader reader, int startIndex, int endIndex) {
            List<FieldInstance> entry = new List<FieldInstance>();
            for (int i = startIndex; i < endIndex; i++) {
                int counter = -1;
                FieldInfo field = this.typeInfo.fields[i];
                string str = this.getFieldValue(reader, field);
                entry.Add(new FieldInstance(field, str));
                switch (field.modifier) {
                    case FieldInfo.Modifier.NextFieldIsConditional:
                        if (field.TestConditionalValue(str)) {
                            // if condition is true, we can read the next field as is
                            break;
                        }
                        // otherwise, we have to include empty fields to have the correct number of entries
                        for (counter = 0; counter < field.length; counter++) {
                            entry.Add(new FieldInstance(typeInfo.fields[i + 1], ""));
                        }
                        i+= field.length;
                        break;

                    case FieldInfo.Modifier.NextFieldRepeats:
                        int repeatCount = Convert.ToInt32(str);
                        List<string> repeatedValues = new List<string>(repeatCount);
                        for (counter = 0; counter < repeatCount; counter++) {
                            repeatedValues.Add(getFieldValue(reader, typeInfo.fields[i + 1]));
                        }
                        entry.Add(new FieldInstance(typeInfo.fields[i + 1], string.Join(", ", repeatedValues)));
                        i++;
                        continue;
                    default:
                        continue;
                }
            }
            return entry;
        }

        public void Export(StreamWriter writer) {
            writer.WriteLine(typeInfo.name);
            writer.WriteLine(Convert.ToString(this.TotalwarHeaderVersion));
            foreach (FieldInfo info2 in typeInfo.fields) {
                writer.Write(info2.name + "\t");
            }
            writer.WriteLine();
            foreach (List<FieldInstance> list in this.entries) {
                string str = "";
                foreach (FieldInstance instance in list) {
                    string str2 = instance.Value;
                    str = str + str2.Replace("\t", @"\t").Replace("\n", @"\n") + "\t";
                }
                writer.WriteLine(str.TrimEnd(new char[] { '\t' }));
            }
        }

        public byte[] GetBytes() {
            byte[] buffer;
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    this.writeEntries(writer);
                    buffer = stream.ToArray();
                }
            }
            return buffer;
        }

        private string getFieldValue(BinaryReader reader, FieldInfo field) {
            switch (field.type) {
                case PackTypeCode.Empty: {
                        byte[] buffer = reader.ReadBytes(field.length);
                        StringBuilder builder = new StringBuilder();
                        for (int i = 0; i < buffer.Length; i++) {
                            builder.AppendFormat("{0:X2} ", buffer[i]);
                        }
                        return builder.ToString();
                    }
                case PackTypeCode.Boolean:
                    return Convert.ToBoolean(reader.ReadByte()).ToString();

                case PackTypeCode.Int16:
                    return reader.ReadInt16().ToString();

                case PackTypeCode.UInt16:
                    return reader.ReadUInt16().ToString();

                case PackTypeCode.Int32:
                    return reader.ReadInt32().ToString();

                case PackTypeCode.UInt32:
                    return reader.ReadUInt32().ToString();

                case PackTypeCode.Int64:
                    return reader.ReadInt64().ToString();

                case PackTypeCode.UInt64:
                    return reader.ReadUInt64().ToString();

                case PackTypeCode.Single:
                    return reader.ReadSingle().ToString();

                case PackTypeCode.Double:
                    return reader.ReadDouble().ToString();

                case PackTypeCode.String:
                    return IOFunctions.readCAString(reader);

                case PackTypeCode.StringContainer:
                    return IOFunctions.readStringContainer(reader);
            }
            throw new InvalidDataException("unknown field type");
        }

        public List<FieldInstance> GetNewEntry() {
            List<FieldInstance> list = new List<FieldInstance>();
            foreach (FieldInfo info in typeInfo.fields) {
                List<string> list2;
                int num;
                switch (info.type) {
                    case PackTypeCode.Empty:
                        list2 = new List<string>();
                        num = 0;
                        goto Label_01BC;

                    case PackTypeCode.Object:
                    case PackTypeCode.DBNull:
                    case PackTypeCode.Char:
                    case PackTypeCode.SByte:
                    case PackTypeCode.Byte:
                    case PackTypeCode.Decimal:
                    case PackTypeCode.DateTime:
                    case (PackTypeCode.DateTime | PackTypeCode.Object): {
                            continue;
                        }
                    case PackTypeCode.Boolean: {
                            list.Add(new FieldInstance(info, "False"));
                            continue;
                        }
                    case PackTypeCode.Int16: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.UInt16: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.Int32: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.UInt32: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.Int64: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.UInt64: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.Single: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.Double: {
                            list.Add(new FieldInstance(info, "0"));
                            continue;
                        }
                    case PackTypeCode.String: {
                            list.Add(new FieldInstance(info, string.Empty));
                            continue;
                        }
                    case PackTypeCode.StringContainer: {
                            list.Add(new FieldInstance(info, string.Empty));
                            continue;
                        }
                    default: {
                            continue;
                        }
                }
            Label_01AC:
                list2.Add("00");
                num++;
            Label_01BC:
                if (num < info.length) {
                    goto Label_01AC;
                }
                list.Add(new FieldInstance(info, string.Join(" ", list2.ToArray())));
            }
            return list;
        }

        public void Import(StreamReader reader, string type) {
			TypeInfo info;
			if (this.CurrentType.name != reader.ReadLine ().TrimEnd (new char[0]).Trim (new char[] { '"' })) {
				throw new DBFileNotSupportedException ("File type of imported DB doesn't match that of the currently opened one", this);
			}
			string str = reader.ReadLine ().TrimEnd (new char[0]).Trim (new char[] { '"' });
			if (str == "1.0") {
				this.TotalwarHeaderVersion = 0;
			} else if (str == "1.2") {
				this.TotalwarHeaderVersion = 1;
			} else {
				this.TotalwarHeaderVersion = Convert.ToInt32 (str);
			}
			info = DBTypeMap.Instance [type, TotalwarHeaderVersion];
			reader.ReadLine ();
			this.entries = new List<List<FieldInstance>> ();
			while (!reader.EndOfStream) {
				string str2 = reader.ReadLine ();
				if (str2.Trim () != "") {
					string[] strArray = str2.Split (new char[] { '\t' });
					List<FieldInstance> item = new List<FieldInstance> ();
					for (int i = 0; i < strArray.Length; i++) {
						FieldInfo fieldInfo = info.fields [i];
						string str3 = strArray [i].Replace (@"\t", "\t").Replace (@"\n", "\n").Trim (new char[] { '"' });
						item.Add (new FieldInstance (fieldInfo, str3));
					}
					this.entries.Add (item);
				}
			}
		}

        private string readString(BinaryReader reader) {
            ushort count = reader.ReadUInt16();
            return new string(reader.ReadChars(count));
        }

        public void Save() {
            using (BinaryWriter writer = new BinaryWriter(new FileStream("foo.txt", FileMode.Create))) {
                this.writeEntries(writer);
            }
        }

        private void writeEntries(BinaryWriter writer) {
            // TypeInfo info;
            if (this.TotalwarHeaderVersion == 0) {
                writer.Write((byte)1);
                // info = this.type[0];
            } else {
                writer.Write(new byte[] { 0xfd, 0xfe, 0xfc, 0xff });
                IOFunctions.writeCAString(writer, GUID);

                if (TotalwarHeaderVersion != 0) {
                    writer.Write(new byte[] { 0xfc, 0xfd, 0xfe, 0xff });
                    writer.Write(this.TotalwarHeaderVersion);
                }

                writer.Write((byte)1);
                // info = this.type[this.TotalwarHeaderVersion];
            }
            writer.Write(this.Entries.Count);
            for (int i = 0; i < this.Entries.Count; i++) {
                this.writeFields(writer, this.entries[i], 0, this.typeInfo.fields.Count);
            }
        }

        private void writeFields(BinaryWriter writer, List<FieldInstance> entry, int startIndex, int endIndex) {
            for (int i = startIndex; i < endIndex; i++) {
                int num2;
                string[] strArray;
                int num3;
                FieldInstance instance = entry[i];
                FieldInfo field = instance.Info;
                string str = instance.Value;
                this.writeFieldValuePair(writer, field, str);
                switch (field.modifier) {
                    case FieldInfo.Modifier.NextFieldIsConditional: {
                            if (field.TestConditionalValue(str)) {
                                this.writeFields(writer, entry, i + 1, (i + 1) + field.length);
                            }
                            i += field.length;
                            continue;
                        }
                    case FieldInfo.Modifier.NextFieldRepeats:
                        num2 = Convert.ToInt32(str);
                        strArray = entry[i + 1].Value.Split(",".ToCharArray());
                        num3 = 0;
                        goto Label_00CE;

                    default: {
                            continue;
                        }
                }
            Label_00B1:
                this.writeFieldValuePair(writer, entry[i + 1].Info, strArray[num3]);
                num3++;
            Label_00CE:
                if (num3 < num2) {
                    goto Label_00B1;
                }
                i++;
            }
        }

        private void writeFieldValuePair(BinaryWriter writer, FieldInfo field, string value) {
            switch (field.type) {
                case PackTypeCode.Empty: {
                        string[] strArray = value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        foreach (string str in strArray) {
                            writer.Write(Convert.ToByte(str, 0x10));
                        }
                        return;
                    }
                case PackTypeCode.Boolean:
                    writer.Write(Convert.ToBoolean(value));
                    return;

                case PackTypeCode.Int16:
                    writer.Write(Convert.ToInt16(value));
                    return;

                case PackTypeCode.UInt16:
                    writer.Write(Convert.ToUInt16(value));
                    return;

                case PackTypeCode.Int32:
                    writer.Write(Convert.ToInt32(value));
                    return;

                case PackTypeCode.UInt32:
                    writer.Write(Convert.ToUInt32(value));
                    return;

                case PackTypeCode.Int64:
                    writer.Write(Convert.ToInt64(value));
                    return;

                case PackTypeCode.UInt64:
                    writer.Write(Convert.ToUInt64(value));
                    return;

                case PackTypeCode.Single:
                    writer.Write(Convert.ToSingle(value));
                    return;

                case PackTypeCode.Double:
                    writer.Write(Convert.ToDouble(value));
                    return;

                case PackTypeCode.String:
                    IOFunctions.writeCAString(writer, value);
                    return;

                case PackTypeCode.StringContainer:
                    IOFunctions.writeStringContainer(writer, value);
                    return;
            }
            throw new InvalidDataException("unknown field type");
        }

        public TypeInfo CurrentType {
            get {
                return typeInfo;
            }
        }

        public List<List<FieldInstance>> Entries {
			get {
				return this.entries;
			}
		}
		
		public static string typename(string fullPath) {
            return fullPath.Split ('\\') [1].Split ('/') [0].Replace ("_tables", "");
		}
    }
}

