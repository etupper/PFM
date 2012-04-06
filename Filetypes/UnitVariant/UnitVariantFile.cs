namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
	
    public class UnitVariantFile
    {
		// "header" information
        public uint Version { get; set; }
        public byte B1 { get; set; }
        public byte B2 { get; set; }
        public byte B3 { get; set; }
        public byte B4 { get; set; }
		public uint Unknown1 { get; set; }
		public uint Unknown2 { get; set; }
		public int Unknown3 { get; set; }

		private List<UnitVariantObject> unitVariantObjects = new List<UnitVariantObject> ();
		public List<UnitVariantObject> UnitVariantObjects {
			get {
				return this.unitVariantObjects;
			}
			set {
				this.unitVariantObjects = value;
			}
		}

        public uint NumEntries { 
			get {
				return (uint) unitVariantObjects.Count;
			}
		}

        public void add(UnitVariantObject newEntry)
        {
            this.unitVariantObjects.Add(newEntry);
        }

        public void addMTO(UnitVariantObject entry, MeshTextureObject mTO)
        {
            UnitVariantObject obj2 = this.unitVariantObjects[(int) entry.Index];
            // obj2.EntryCount++;
            obj2.MeshTextureList.Add(mTO);
            int count = this.unitVariantObjects.Count;
            for (int i = ((int) obj2.Index) + 1; i < count; i++)
            {
                this.unitVariantObjects[i].MeshStartIndex = this.unitVariantObjects[i - 1].EntryCount + this.unitVariantObjects[i - 1].MeshStartIndex;
            }
        }

        public void insertUVO(UnitVariantObject entry, int index)
        {
            this.unitVariantObjects.Insert(index, entry);
            if (index < (this.unitVariantObjects.Count - 1))
            {
                for (int i = index + 1; i < this.unitVariantObjects.Count; i++)
                {
                    this.unitVariantObjects[i].Index = this.unitVariantObjects[i - 1].Index + 1;
                    this.unitVariantObjects[i].MeshStartIndex = this.unitVariantObjects[i - 1].EntryCount + this.unitVariantObjects[i - 1].MeshStartIndex;
                }
            }
        }

        public void removeAt(int index)
        {
            this.unitVariantObjects.RemoveAt(index);
        }

        public void removeMTO(UnitVariantObject entry, MeshTextureObject mTO, int index) {
			UnitVariantObject obj2 = this.unitVariantObjects [(int)entry.Index];
			// obj2.EntryCount--;
			obj2.MeshTextureList.RemoveAt (index - 1);
			int count = this.unitVariantObjects.Count;
			for (int i = (int) entry.Index; i < count; i++) {
				// adjust mesh indices of the following UVOs
				// isn't really neccessary because the codec corrects these when writing
				this.unitVariantObjects [i].MeshStartIndex = this.unitVariantObjects [i - 1].EntryCount + this.unitVariantObjects [i - 1].MeshStartIndex;
			}
		}

        public void removeUVO(int index) {
			this.unitVariantObjects.RemoveAt (index);
			if (index < (this.unitVariantObjects.Count - 1)) {
				for (int i = index; i < this.unitVariantObjects.Count; i++) {
					// adjust mesh indices in all previous and following UVOs
					// again, not really neccessary
					this.unitVariantObjects [i].Index = this.unitVariantObjects [i - 1].Index + 1;
					this.unitVariantObjects [i].MeshStartIndex = 
					this.unitVariantObjects [i - 1].EntryCount + this.unitVariantObjects [i - 1].MeshStartIndex;
				}
			}
		}
    }
}

