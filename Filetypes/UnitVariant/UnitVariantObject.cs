﻿namespace Common
{
    using System;
    using System.Collections.Generic;

    public class UnitVariantObject
    {
		// header info
		public string ModelPart { set; get; }
        public uint Index { get; set; }
        public uint Num2 { get; set; }
        public uint Num3 { get; set; }
		public uint MeshStartIndex { get; set; }
		
		// contained meshes
		public List<MeshTextureObject> MeshTextureList { get; private set; }

        public UnitVariantObject () {
			this.ModelPart = string.Empty;
			this.Index = 0;
			this.Num2 = 0;
			this.Num3 = 0;
			this.MeshStartIndex = 0;
			MeshTextureList = new List<MeshTextureObject> ();
		}

        public uint EntryCount {
			get {
				return (uint)MeshTextureList.Count;
			}
		}
		
		// only used when in the process of being loaded
		public volatile uint StoredEntryCount = 0;
    }
	
    public class MeshTextureObject {
		public string Mesh { get; set; }
		public string Texture { get; set; }
		public bool Bool1 { get; set; }
		public bool Bool2 { get; set; }

		public MeshTextureObject (string mMesh = "", string mTexture = "", bool mBool1 = false, bool mBool2 = false) {
			this.Mesh = mMesh;
			this.Texture = mTexture;
			this.Bool1 = mBool1;
			this.Bool2 = mBool2;
		}
	}
}
