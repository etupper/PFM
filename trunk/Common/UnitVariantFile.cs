namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class UnitVariantFile
    {
        private uint version;
        private byte b1 = 0;
        private byte b2 = 0;
        private byte b3 = 0;
        private byte b4 = 0;
        private uint numEntries = 0;
        private List<UnitVariantObject> unitVariantObjects = new List<UnitVariantObject>();
        private uint unknown1 = 0;
        private uint unknown2 = 0;
        private int unknown3 = 0;

        public void add(UnitVariantObject newEntry)
        {
            this.unitVariantObjects.Add(newEntry);
            this.numEntries++;
        }

        public void addMTO(UnitVariantObject entry, MeshTextureObject mTO)
        {
            UnitVariantObject obj2 = this.unitVariantObjects[(int) entry.Num1];
            obj2.Num3++;
            obj2.MeshTextureList.Add(mTO);
            int count = this.unitVariantObjects.Count;
            for (int i = ((int) obj2.Num1) + 1; i < count; i++)
            {
                this.unitVariantObjects[i].Num4 = this.unitVariantObjects[i - 1].Num3 + this.unitVariantObjects[i - 1].Num4;
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

        public void insertUVO(UnitVariantObject entry, int index)
        {
            this.unitVariantObjects.Insert(index, entry);
            if (index < (this.unitVariantObjects.Count - 1))
            {
                for (int i = index + 1; i < this.unitVariantObjects.Count; i++)
                {
                    this.unitVariantObjects[i].Num1 = this.unitVariantObjects[i - 1].Num1 + 1;
                    this.unitVariantObjects[i].Num4 = this.unitVariantObjects[i - 1].Num3 + this.unitVariantObjects[i - 1].Num4;
                }
            }
            this.numEntries++;
        }

        public void removeAt(int index)
        {
            this.unitVariantObjects.RemoveAt(index);
        }

        public void removeMTO(UnitVariantObject entry, MeshTextureObject mTO, int index)
        {
            UnitVariantObject obj2 = this.unitVariantObjects[(int) entry.Num1];
            obj2.Num3--;
            obj2.MeshTextureList.RemoveAt(index - 1);
            int count = this.unitVariantObjects.Count;
            for (int i = (int) entry.Num1; i < count; i++)
            {
                this.unitVariantObjects[i].Num4 = this.unitVariantObjects[i - 1].Num3 + this.unitVariantObjects[i - 1].Num4;
            }
        }

        public void removeUVO(int index)
        {
            this.unitVariantObjects.RemoveAt(index);
            if (index < (this.unitVariantObjects.Count - 1))
            {
                for (int i = index; i < this.unitVariantObjects.Count; i++)
                {
                    this.unitVariantObjects[i].Num1 = this.unitVariantObjects[i - 1].Num1 + 1;
                    this.unitVariantObjects[i].Num4 = this.unitVariantObjects[i - 1].Num3 + this.unitVariantObjects[i - 1].Num4;
                }
            }
            this.numEntries--;
        }

        public void replace(int index, UnitVariantObject newEntry)
        {
            this.unitVariantObjects[index] = newEntry;
        }

        public void resetEntries()
        {
            this.unitVariantObjects = new List<UnitVariantObject>();
            this.numEntries = 0;
        }

        public void setPackedFile(PackedFile packedFile)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(packedFile.Data, false));
            byte[] buffer = reader.ReadBytes(4);
            if ((((buffer[0] != 0x56) || (buffer[1] != 0x52)) || (buffer[2] != 0x4e)) || (buffer[3] != 0x54))
            {
                throw new FileLoadException("Illegal unit_variant file: Does not start with 'VRNT'");
            }
            this.version = reader.ReadUInt32();
            this.numEntries = reader.ReadUInt32();
            this.unknown1 = reader.ReadUInt32();
            byte[] buffer3 = reader.ReadBytes(4);
            this.b1 = buffer3[0];
            this.b2 = buffer3[1];
            this.b3 = buffer3[2];
            this.b4 = buffer3[3];
            this.unknown2 = BitConverter.ToUInt32(buffer3, 0);
            if (version == 2) {
                this.unknown3 = reader.ReadInt32();
            }
            this.unitVariantObjects = new List<UnitVariantObject>();
            for (int i = 0; i < this.numEntries; i++)
            {
                UnitVariantObject item = new UnitVariantObject {
                    ModelPart = IOFunctions.readStringContainer(reader),
                    Num1 = reader.ReadUInt32(),
                    Num2 = reader.ReadUInt32(),
                    Num3 = reader.ReadUInt32(),
                    Num4 = reader.ReadUInt32()
                };
                this.unitVariantObjects.Add(item);
            }
            for (int j = 0; j < this.numEntries; j++)
            {
                for (int k = 0; k < this.unitVariantObjects[j].Num3; k++)
                {
                    MeshTextureObject obj3 = new MeshTextureObject {
                        Mesh = IOFunctions.readStringContainer(reader),
                        Texture = IOFunctions.readStringContainer(reader),
                        Bool1 = reader.ReadBoolean(),
                        Bool2 = reader.ReadBoolean()
                    };
                    this.unitVariantObjects[j].MeshTextureList.Add(obj3);
                }
            }
            reader.Close();
        }

        private void writeToStream(BinaryWriter writer)
        {
            writer.Write("VRNT".ToCharArray(0, 4));
            writer.Write(this.version);
            writer.Write(this.numEntries);
            writer.Write(this.unknown1);
            writer.Write(this.unknown2);
            if (version == 2) {
                writer.Write(this.unknown3);
            }
            for (int i = 0; i < this.numEntries; i++)
            {
                IOFunctions.writeStringContainer(writer, this.unitVariantObjects[i].ModelPart);
                writer.Write(this.unitVariantObjects[i].Num1);
                writer.Write(this.unitVariantObjects[i].Num2);
                writer.Write(this.unitVariantObjects[i].Num3);
                writer.Write(this.unitVariantObjects[i].Num4);
            }
            for (int j = 0; j < this.numEntries; j++)
            {
                if (this.unitVariantObjects[j].Num3 != 0)
                {
                    for (int k = 0; k < this.unitVariantObjects[j].Num3; k++)
                    {
                        IOFunctions.writeStringContainer(writer, this.unitVariantObjects[j].MeshTextureList[k].Mesh);
                        IOFunctions.writeStringContainer(writer, this.unitVariantObjects[j].MeshTextureList[k].Texture);
                        writer.Write(this.unitVariantObjects[j].MeshTextureList[k].Bool1);
                        writer.Write(this.unitVariantObjects[j].MeshTextureList[k].Bool2);
                    }
                }
            }
        }

        public byte B1
        {
            get
            {
                return this.b1;
            }
            set
            {
                this.b1 = value;
            }
        }

        public byte B2
        {
            get
            {
                return this.b2;
            }
            set
            {
                this.b2 = value;
            }
        }

        public byte B3
        {
            get
            {
                return this.b3;
            }
            set
            {
                this.b3 = value;
            }
        }

        public byte B4
        {
            get
            {
                return this.b4;
            }
            set
            {
                this.b4 = value;
            }
        }

        public uint NumEntries
        {
            get
            {
                return this.numEntries;
            }
            set
            {
                this.numEntries = value;
            }
        }

        public List<UnitVariantObject> UnitVariantObjects
        {
            get
            {
                return this.unitVariantObjects;
            }
            set
            {
                this.unitVariantObjects = value;
            }
        }

        public uint Unknown1
        {
            get
            {
                return this.unknown1;
            }
            set
            {
                this.unknown1 = value;
            }
        }

        public uint Unknown2
        {
            get
            {
                return this.unknown2;
            }
            set
            {
                this.unknown2 = value;
            }
        }
    }
}

