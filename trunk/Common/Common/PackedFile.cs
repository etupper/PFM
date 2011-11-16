namespace Common
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    [DebuggerDisplay("{offset}@{Filepath}")]
    public class PackedFile
    {
        private PackAction action;
        private string filepath;
        public ulong offset;
        private Common.PackFile packFile;
        private uint size;

        internal PackedFile(Common.PackFile packFile, string filepathToAdd)
        {
            this.packFile = packFile;
            this.offset = 0L;
            string relativePathAnchor = packFile.RelativePathAnchor;
            if (!relativePathAnchor.EndsWith(@"\"))
            {
                relativePathAnchor = relativePathAnchor + @"\";
            }
            this.filepath = filepathToAdd.Replace(relativePathAnchor, "");
            if (Path.IsPathRooted(this.filepath))
            {
                throw new ArgumentException("added files must be in the relative directory or a subdirectory");
            }
            this.action = new AddFilePackAction(filepathToAdd);
            this.size = (uint) new FileInfo(filepathToAdd).Length;
            packFile.OnModified();
        }

        internal PackedFile(Common.PackFile packFile, uint size, string filepath, ulong offset)
        {
            this.packFile = packFile;
            this.size = size;
            this.filepath = filepath;
            this.offset = offset;
            this.action = null;
        }

        public void Delete()
        {
            this.action = new DeleteFilePackAction();
            this.packFile.OnModified();
        }

        public int Read(out byte[] data)
        {
            if (this.action is DeleteFilePackAction)
            {
                data = null;
                return 0;
            }
            if (this.action is AddFilePackAction)
            {
                data = File.ReadAllBytes((this.action as AddFilePackAction).filepath);
                return data.Length;
            }
            if (this.action is ReplaceFilePackAction)
            {
                data = File.ReadAllBytes((this.action as ReplaceFilePackAction).filepath);
                return data.Length;
            }
            if (this.action is ReplaceDataPackAction)
            {
                data = (this.action as ReplaceDataPackAction).data;
                return data.Length;
            }
            using (BinaryReader reader = new BinaryReader(new FileStream(this.packFile.Filepath, FileMode.Open), Encoding.ASCII))
            {
                data = new byte[this.size];
                reader.BaseStream.Seek((long) this.offset, SeekOrigin.Begin);
                return reader.Read(data, 0, (int) this.size);
            }
        }

        public void Rename(string filepath)
        {
            this.action = new RenamePackAction(filepath);
            this.packFile.OnModified();
        }

        public void Replace(string filepath)
        {
            this.filepath = Path.Combine(Path.GetDirectoryName(this.filepath), Path.GetFileName(filepath));
            this.action = new ReplaceFilePackAction(filepath);
            this.packFile.OnModified();
        }

        public void ReplaceData(byte[] data)
        {
            this.action = new ReplaceDataPackAction(data);
            this.packFile.OnModified();
        }

        public PackAction Action
        {
            get
            {
                return this.action;
            }
        }

        public byte[] Data
        {
            get
            {
                byte[] buffer;
                this.Read(out buffer);
                return buffer;
            }
        }

        public string Filepath
        {
            get
            {
                return this.filepath;
            }
        }

        public Common.PackFile PackFile
        {
            get
            {
                return this.packFile;
            }
        }

        public uint Size
        {
            get
            {
                if (this.action != null)
                {
                    if (this.action is DeleteFilePackAction)
                    {
                        return 0;
                    }
                    if (this.action is ReplaceFilePackAction)
                    {
                        return (uint) new FileInfo((this.action as ReplaceFilePackAction).filepath).Length;
                    }
                    if (this.action is ReplaceDataPackAction)
                    {
                        return (uint) (this.action as ReplaceDataPackAction).data.Length;
                    }
                }
                return this.size;
            }
        }

        public class AddFilePackAction : PackedFile.PackAction
        {
            public string filepath;

            public AddFilePackAction(string filepathToAdd)
            {
                this.filepath = filepathToAdd;
            }
        }

        public class DeleteFilePackAction : PackedFile.PackAction
        {
        }

        public abstract class PackAction
        {
            protected PackAction()
            {
            }
        }

        public class RenamePackAction : PackedFile.PackAction
        {
            public string filepath;

            public RenamePackAction(string newFilepath)
            {
                this.filepath = newFilepath;
            }
        }

        public class ReplaceDataPackAction : PackedFile.PackAction
        {
            public byte[] data;

            public ReplaceDataPackAction(byte[] replacementData)
            {
                this.data = replacementData;
            }
        }

        public class ReplaceFilePackAction : PackedFile.PackAction
        {
            public string filepath;

            public ReplaceFilePackAction(string replacementFilepath)
            {
                this.filepath = replacementFilepath;
            }
        }
    }
}

