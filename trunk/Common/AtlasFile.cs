namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class AtlasFile
    {
        private List<AtlasObject> atlasObjects = new List<AtlasObject>();
        public int numEntries;

        public void add(AtlasObject newEntry)
        {
            this.atlasObjects.Add(newEntry);
            this.numEntries++;
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

        public void ReadAtlasFile(BinaryReader reader)
        {
            if (reader.ReadInt32() != 1)
            {
                throw new FileLoadException("Illegal atlas file: Does not start with '1'");
            }
            reader.ReadBytes(4);
            this.numEntries = reader.ReadInt32();
            this.atlasObjects = new List<AtlasObject>();
            for (int i = 0; i < this.numEntries; i++)
            {
                AtlasObject item = new AtlasObject {
                    Container1 = IOFunctions.readStringContainer(reader),
                    Container2 = IOFunctions.readStringContainer(reader),
                    X1 = reader.ReadSingle(),
                    Y1 = reader.ReadSingle(),
                    X2 = reader.ReadSingle(),
                    Y2 = reader.ReadSingle(),
                    X3 = reader.ReadSingle(),
                    Y3 = reader.ReadSingle()
                };
                this.atlasObjects.Add(item);
            }
            reader.Close();
        }

        public void removeAt(int index)
        {
            this.atlasObjects.RemoveAt(index);
            this.numEntries--;
        }

        public void replace(int index, AtlasObject newEntry)
        {
            this.atlasObjects[index] = newEntry;
        }

        public void resetEntries()
        {
            this.atlasObjects = new List<AtlasObject>();
            this.numEntries = 0;
        }

        public void setPackedFile(PackedFile packedFile)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(packedFile.Data, false));
            this.ReadAtlasFile(reader);
            reader.Close();
        }

        public void setPixelUnits(float imageHeight)
        {
            foreach (AtlasObject obj2 in this.atlasObjects)
            {
                obj2.PX1 = obj2.X1 * 4096f;
                obj2.PY1 = obj2.Y1 * imageHeight;
                obj2.PX2 = obj2.X2 * 4096f;
                obj2.PY2 = obj2.Y2 * imageHeight;
            }
        }

        private void writeToStream(BinaryWriter writer)
        {
            writer.Write((uint) 1);
            writer.Write((uint) 0);
            writer.Write(this.numEntries);
            for (int i = 0; i < this.numEntries; i++)
            {
                IOFunctions.writeStringContainer(writer, this.atlasObjects[i].Container1);
                IOFunctions.writeStringContainer(writer, this.atlasObjects[i].Container2);
                writer.Write(this.atlasObjects[i].X1);
                writer.Write(this.atlasObjects[i].Y1);
                writer.Write(this.atlasObjects[i].X2);
                writer.Write(this.atlasObjects[i].Y2);
                writer.Write(this.atlasObjects[i].X3);
                writer.Write(this.atlasObjects[i].Y3);
            }
        }

        public List<AtlasObject> Entries
        {
            get
            {
                return this.atlasObjects;
            }
        }
    }
}

