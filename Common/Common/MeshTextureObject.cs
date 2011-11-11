namespace Common
{
    using System;

    public class MeshTextureObject
    {
        private bool bool1;
        private bool bool2;
        private string mesh;
        private string texture;

        public MeshTextureObject()
        {
            this.mesh = string.Empty;
            this.texture = string.Empty;
            this.bool1 = false;
            this.bool2 = false;
        }

        public MeshTextureObject(string mMesh, string mTexture, bool mBool1, bool mBool2)
        {
            this.mesh = mMesh;
            this.texture = mTexture;
            this.bool1 = mBool1;
            this.bool2 = mBool2;
        }

        public bool Bool1
        {
            get
            {
                return this.bool1;
            }
            set
            {
                this.bool1 = value;
            }
        }

        public bool Bool2
        {
            get
            {
                return this.bool2;
            }
            set
            {
                this.bool2 = value;
            }
        }

        public string Mesh
        {
            get
            {
                return this.mesh;
            }
            set
            {
                this.mesh = value;
            }
        }

        public string Texture
        {
            get
            {
                return this.texture;
            }
            set
            {
                this.texture = value;
            }
        }
    }
}

