namespace Common
{
    using System;
    using System.Collections.Generic;

    public class UnitVariantObject
    {
        private List<MeshTextureObject> meshTextureList;
        private string modelPart;
        private ushort num;
        private uint num1;
        private uint num2;
        private uint num3;
        private uint num4;

        public UnitVariantObject()
        {
            this.modelPart = string.Empty;
            this.num1 = 0;
            this.num2 = 0;
            this.num3 = 0;
            this.num4 = 0;
            this.meshTextureList = new List<MeshTextureObject>();
        }

        public UnitVariantObject(string mModelPart, uint[] nums, List<MeshTextureObject> mMeshTexture, ushort mNum)
        {
            this.modelPart = mModelPart;
            this.num1 = nums[0];
            this.num2 = nums[1];
            this.num3 = nums[2];
            this.num4 = nums[3];
            this.meshTextureList = mMeshTexture;
        }

        public List<MeshTextureObject> MeshTextureList
        {
            get
            {
                return this.meshTextureList;
            }
            set
            {
                this.meshTextureList = value;
            }
        }

        public string ModelPart
        {
            get
            {
                return this.modelPart;
            }
            set
            {
                this.modelPart = value;
            }
        }

        public uint Num1
        {
            get
            {
                return this.num1;
            }
            set
            {
                this.num1 = value;
            }
        }

        public uint Num2
        {
            get
            {
                return this.num2;
            }
            set
            {
                this.num2 = value;
            }
        }

        public uint Num3
        {
            get
            {
                return this.num3;
            }
            set
            {
                this.num3 = value;
            }
        }

        public uint Num4
        {
            get
            {
                return this.num4;
            }
            set
            {
                this.num4 = value;
            }
        }
    }
}

