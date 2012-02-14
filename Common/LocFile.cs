namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class LocFile
    {
        private List<LocEntry> entries = new List<LocEntry>();
        public string name;
        public int numEntries;

        public void add(LocEntry newEntry)
        {
            this.entries.Add(newEntry);
            this.numEntries++;
        }

        public void Export(StreamWriter writer)
        {
            for (int i = 0; i < this.numEntries; i++)
            {
                writer.WriteLine(this.entries[i].Tag.Replace("\t", @"\t").Replace("\n", @"\n") + "\t" + this.entries[i].Localised.Replace("\t", @"\t").Replace("\n", @"\n") + "\t" + (this.entries[i].Tooltip ? "True" : "False"));
            }
        }

        public byte[] GetBytes()
        {
            byte[] buffer;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    this.writeToStream(writer);
                    buffer = stream.ToArray();
                }
            }
            return buffer;
        }

        public void Import(StreamReader reader)
        {
            this.entries = new List<LocEntry>();
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                if (str.Trim() != "")
                {
                    string[] strArray = str.Split(new char[] { '\t' });
                    string tag = strArray[0].Replace(@"\t", "\t").Replace(@"\n", "\n").Trim(new char[] { '"' });
                    string localised = strArray[1].Replace(@"\t", "\t").Replace(@"\n", "\n").Trim(new char[] { '"' });
                    bool tooltip = strArray[2].ToLower() == "true";
                    this.entries.Add(new LocEntry(tag, localised, tooltip));
                }
            }
            this.numEntries = this.entries.Count;
        }

        public void removeAt(int index)
        {
            this.entries.RemoveAt(index);
        }

        public void replace(int index, LocEntry newEntry)
        {
            this.entries[index] = newEntry;
        }

        public void resetEntries()
        {
            this.entries = new List<LocEntry>();
            this.numEntries = 0;
        }

        public void setPackedFile(PackedFile packedFile)
        {
            this.name = Path.GetFileName(packedFile.FullPath);
            BinaryReader reader = new BinaryReader(new MemoryStream(packedFile.Data, false));
            if (reader.ReadInt16() != -257)
            {
                throw new FileLoadException("Illegal loc file: doesn't have a byte order mark");
            }
            byte[] buffer = reader.ReadBytes(3);
            if (((buffer[0] != 0x4c) || (buffer[1] != 0x4f)) || (buffer[2] != 0x43))
            {
                throw new FileLoadException("Illegal loc file: doesn't have LOC string");
            }
            reader.ReadByte();
            if (reader.ReadInt32() != 1)
            {
                throw new FileLoadException("Illegal loc file: File version isn't '1'");
            }
            this.numEntries = reader.ReadInt32();
            this.entries = new List<LocEntry>();
            for (int i = 0; i < this.numEntries; i++)
            {
                string tag = IOFunctions.readCAString(reader);
                string localised = IOFunctions.readCAString(reader);
                bool tooltip = reader.ReadBoolean();
                this.entries.Add(new LocEntry(tag, localised, tooltip));
            }
            reader.Close();
        }

        private void writeToStream(BinaryWriter writer)
        {
            writer.Write((short) (-257));
            writer.Write("LOC".ToCharArray());
            writer.Write((byte) 0);
            writer.Write(1);
            writer.Write(this.numEntries);
            for (int i = 0; i < this.numEntries; i++)
            {
                IOFunctions.writeCAString(writer, this.entries[i].Tag);
                IOFunctions.writeCAString(writer, this.entries[i].Localised);
                writer.Write(this.entries[i].Tooltip);
            }
        }

        public List<LocEntry> Entries
        {
            get
            {
                return this.entries;
            }
        }
    }
}

