using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Common.Properties;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics;

namespace Common
{

    public class DBFile
    {
        private List<List<FieldInstance>> entries;
        private TypeInfo typeInfo;
        private PackedFile packedFile;
        public int TotalwarHeaderVersion { get; set; }
        private readonly TypeInfo[] type = new TypeInfo[Settings.Default.totalwarHeaderVersions];
        private Int16 GUIDLength;
        private byte[] GUID;

        public DBFile(PackedFile packedFile, TypeInfo[] type, bool readData = true)
        {
            BinaryReader reader = readHeader(packedFile);

            if (readData)
            {
                this.type = type;
                try
                {
                    this.typeInfo = type[TotalwarHeaderVersion];
                    //this.typeInfo = type[0]; // temporarily overwrite with 0 for db testing
                    int num3 = reader.ReadInt32();
                    this.entries = new List<List<FieldInstance>>();
                    for (int i = 0; i < num3; i++)
                    {
                        List<FieldInstance> entry = new List<FieldInstance>();
                        this.addFields(reader, entry, 0, this.typeInfo.fields.Count);
                        this.entries.Add(entry);
                    }
                }
                catch (Exception)
                {
                    throw new DBFileNotSupportedException(
                        string.Format("This table has an unexpected format. DB file version is {0}, we can handle up to version {1}.",
                        TotalwarHeaderVersion, type.Length - 1));
                }
            }
        }
        private BinaryReader readHeader(PackedFile packedFile)
        {
            this.packedFile = packedFile;
            var reader = new BinaryReader(new MemoryStream(packedFile.Data, false));
            byte num = reader.ReadByte();
            int index = 0;
            int version = 0;
            if (num != 1)
            {
                var bytes = new List<byte>();
                bytes.AddRange(reader.ReadBytes(3));
                bytes.Add(0x00);
                UInt32 header = BitConverter.ToUInt32(bytes.ToArray(), 0);
                if (header != 0x00FFFCFE && header != 0x00FFFEFD)
                {
                    throw new DBFileNotSupportedException("DB Header type unknown");
                }
                if (header == 0x00FFFCFE)
                {
                    this.GUIDLength = reader.ReadInt16();
                    this.GUID = reader.ReadBytes(this.GUIDLength * 2);
                    //Int16 guidLength = reader.ReadInt16();
                    //var guid = reader.ReadBytes(guidLength * 2);
                    version = reader.ReadByte();
                    if (version == 0xFC)
                    {
                        bytes.Clear();
                        bytes.AddRange(reader.ReadBytes(3));
                        bytes.Add(0x00);
                        header = BitConverter.ToUInt32(bytes.ToArray(), 0);
                        if (header != 0x00FFFEFD)
                        {
                            throw new DBFileNotSupportedException("DB Header type unknown");
                        }
                    }
                }
                if (version == 0xFC)
                {
                    index = reader.ReadInt32();
                    if (reader.ReadByte() != 1)
                    {
                        throw new DBFileNotSupportedException("DB Header type unknown");
                    }
                }
            }
            this.TotalwarHeaderVersion = index;
            return reader;
        }

        private TypeInfo GetTypeInfo(string dbName)
        {
            string delimitedFields = string.Empty;
            const string dbSchemaPath = @"DB.xsd";
            using (var stream = new FileStream(dbSchemaPath, FileMode.Open, FileAccess.Read))
            {
                var reader = XmlReader.Create(stream);
                var root = XElement.Load(reader);
                var nameTable = reader.NameTable;
                var namespaceManager = new XmlNamespaceManager(nameTable);
                namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
                var xpathQuery = string.Format("./xs:complexType[@name=\"{0}\"]", dbName);
                var tableNode = root.XPathSelectElement(xpathQuery, namespaceManager);

                if (tableNode == null)
                {
                    throw new DBFileNotSupportedException("Unknown table.");
                }

                var fields = tableNode.XPathSelectElements("./xs:attribute", namespaceManager);
                foreach (var field in fields)
                {
                    var nameAttribute = field.Attribute("name");
                    var xsTypeAttribute = field.Attribute("type");
                    var useAttribute = field.Attribute("use");

                    if (nameAttribute != null) delimitedFields += nameAttribute.Value;
                    delimitedFields += ",";
                    if (xsTypeAttribute != null) delimitedFields += xsTypeAttribute.Value.Replace("xs:", "");
                    delimitedFields += ";";
                }
            }
            var typeInfo = new TypeInfo(dbName, delimitedFields);

            return typeInfo;
        }

        private void addFields(BinaryReader reader, List<FieldInstance> entry, int startIndex, int endIndex)
        {
            try
            {
            for (int i = startIndex; i < endIndex; i++)
                {
                    int num2;
                    int num3;
                    string[] strArray;
                    FieldInfo field = this.typeInfo.fields[i];
                        string str = this.getFieldValue(reader, field);
                        entry.Add(new FieldInstance(field, str));
                        switch (field.modifier)
                        {
                            case FieldInfo.Modifier.NextFieldIsConditional:
                                if (field.TestConditionalValue(str))
                                {
                                    break;
                                }
                                goto Label_00AB;

                            case FieldInfo.Modifier.NextFieldRepeats:
                                num3 = Convert.ToInt32(str);
                                strArray = new string[num3];
                                num2 = 0;
                                goto Label_0111;

                            default:
                                {
                                    continue;
                                }
                        }
                        this.addFields(reader, entry, i + 1, (i + 1) + field.length);
                        goto Label_00E8;
                    Label_00AB:
                        num2 = 1;
                        while (num2 <= field.length)
                        {
                            entry.Add(new FieldInstance(typeInfo.fields[i + num2], ""));
                            num2++;
                        }
                    Label_00E8:
                        i += field.length;
                        continue;
                    Label_00F4:
                        strArray[num2] = this.getFieldValue(reader, typeInfo.fields[i + 1]);
                        num2++;
                    Label_0111:
                        if (num2 < num3)
                        {
                            goto Label_00F4;
                        }
                        entry.Add(new FieldInstance(typeInfo.fields[i + 1], string.Join(", ", strArray)));
                        i++;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void Export(StreamWriter writer)
        {
            writer.WriteLine(typeInfo.name);
            writer.WriteLine(Convert.ToString(this.TotalwarHeaderVersion));
            foreach (FieldInfo info2 in typeInfo.fields)
            {
                writer.Write(info2.name + "\t");
            }
            writer.WriteLine();
            foreach (List<FieldInstance> list in this.entries)
            {
                string str = "";
                foreach (FieldInstance instance in list)
                {
                    string str2 = instance.Value;
                    str = str + str2.Replace("\t", @"\t").Replace("\n", @"\n") + "\t";
                }
                writer.WriteLine(str.TrimEnd(new char[] { '\t' }));
            }
        }

        public byte[] GetBytes()
        {
            byte[] buffer;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    this.writeEntries(writer);
                    buffer = stream.ToArray();
                }
            }
            return buffer;
        }

        private string getFieldValue(BinaryReader reader, FieldInfo field)
        {
            switch (field.type)
            {
                case PackTypeCode.Empty:
                {
                    byte[] buffer = reader.ReadBytes(field.length);
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < buffer.Length; i++)
                    {
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

        public List<FieldInstance> GetNewEntry()
        {
            List<FieldInstance> list = new List<FieldInstance>();
            foreach (FieldInfo info in typeInfo.fields)
            {
                List<string> list2;
                int num;
                switch (info.type)
                {
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
                    case (PackTypeCode.DateTime | PackTypeCode.Object):
                    {
                        continue;
                    }
                    case PackTypeCode.Boolean:
                    {
                        list.Add(new FieldInstance(info, "False"));
                        continue;
                    }
                    case PackTypeCode.Int16:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.UInt16:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.Int32:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.UInt32:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.Int64:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.UInt64:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.Single:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.Double:
                    {
                        list.Add(new FieldInstance(info, "0"));
                        continue;
                    }
                    case PackTypeCode.String:
                    {
                        list.Add(new FieldInstance(info, string.Empty));
                        continue;
                    }
                    case PackTypeCode.StringContainer:
                    {
                        list.Add(new FieldInstance(info, string.Empty));
                        continue;
                    }
                    default:
                    {
                        continue;
                    }
                }
            Label_01AC:
                list2.Add("00");
                num++;
            Label_01BC:
                if (num < info.length)
                {
                    goto Label_01AC;
                }
                list.Add(new FieldInstance(info, string.Join(" ", list2.ToArray())));
            }
            return list;
        }

        public void Import(StreamReader reader)
        {
            TypeInfo info;
            if (this.type[0].name != reader.ReadLine().TrimEnd(new char[0]).Trim(new char[] { '"' }))
            {
                throw new DBFileNotSupportedException("File type of imported DB doesn't match that of the currently opened one");
            }
            string str = reader.ReadLine().TrimEnd(new char[0]).Trim(new char[] { '"' });
            if (str == "1.0")
            {
                this.TotalwarHeaderVersion = 0;
                info = this.type[0];
            }
            else if (str == "1.2")
            {
                this.TotalwarHeaderVersion = 1;
                info = this.type[1];
            }
            else
            {
                this.TotalwarHeaderVersion = Convert.ToInt32(str);
                info = this.type[this.TotalwarHeaderVersion];
            }
            reader.ReadLine();
            this.entries = new List<List<FieldInstance>>();
            while (!reader.EndOfStream)
            {
                string str2 = reader.ReadLine();
                if (str2.Trim() != "")
                {
                    string[] strArray = str2.Split(new char[] { '\t' });
                    List<FieldInstance> item = new List<FieldInstance>();
                    for (int i = 0; i < strArray.Length; i++)
                    {
                        FieldInfo fieldInfo = info.fields[i];
                        string str3 = strArray[i].Replace(@"\t", "\t").Replace(@"\n", "\n").Trim(new char[] { '"' });
                        item.Add(new FieldInstance(fieldInfo, str3));
                    }
                    this.entries.Add(item);
                }
            }
        }

        private string readString(BinaryReader reader)
        {
            ushort count = reader.ReadUInt16();
            return new string(reader.ReadChars(count));
        }

        public void Save()
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream("foo.txt", FileMode.Create)))
            {
                this.writeEntries(writer);
            }
        }

        private void writeEntries(BinaryWriter writer)
        {
            TypeInfo info;
            if (this.TotalwarHeaderVersion == 0)
            {
                writer.Write((byte) 1);
                info = this.type[0];
            }
            else
            {
                writer.Write(new byte[] { 0xfd, 0xfe, 0xfc, 0xff });
                writer.Write(this.GUIDLength);
                writer.Write(this.GUID);

                writer.Write(new byte[] { 0xfc, 0xfd, 0xfe, 0xff });
                writer.Write(this.TotalwarHeaderVersion);
                writer.Write((byte) 1);
                info = this.type[this.TotalwarHeaderVersion];
            }
            writer.Write(this.Entries.Count);
            for (int i = 0; i < this.Entries.Count; i++)
            {
                this.writeFields(writer, this.entries[i], 0, info.fields.Count);
            }
        }

        private void writeFields(BinaryWriter writer, List<FieldInstance> entry, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                int num2;
                string[] strArray;
                int num3;
                FieldInstance instance = entry[i];
                FieldInfo field = instance.Info;
                string str = instance.Value;
                this.writeFieldValuePair(writer, field, str);
                switch (field.modifier)
                {
                    case FieldInfo.Modifier.NextFieldIsConditional:
                    {
                        if (field.TestConditionalValue(str))
                        {
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

                    default:
                    {
                        continue;
                    }
                }
            Label_00B1:
                this.writeFieldValuePair(writer, entry[i + 1].Info, strArray[num3]);
                num3++;
            Label_00CE:
                if (num3 < num2)
                {
                    goto Label_00B1;
                }
                i++;
            }
        }

        private void writeFieldValuePair(BinaryWriter writer, FieldInfo field, string value)
        {
            switch (field.type)
            {
                case PackTypeCode.Empty:
                {
                    string[] strArray = value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (string str in strArray)
                    {
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

        public TypeInfo CurrentType
        {
            get
            {
                return typeInfo;
            }
        }

        public List<List<FieldInstance>> Entries
        {
            get
            {
                return this.entries;
            }
        }
    }
}

