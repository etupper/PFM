using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Common
{
    [DebuggerDisplay("Filepath = {Filepath}")]
    public class PackFile
    {
        public static readonly List<string> CAPackList = new List<string> { 
            "boot.pack", "data.pack", "local_en.pack", "local_en_patch.pack", "models.pack", "models2.pack", "movies.pack", "movies2.pack", "patch_movies.pack", "patch.pack", "patch2.pack", "sound.pack", "terrain.pack"
         };
        private SortedList<string, PackedFile> fileList;
        private string filepath;
        private bool isModified;
        private string relativePathAnchor;
        private ulong size;
        private string PackIdentifier = "PFH2";
        private uint FilesOverwritten = 0;
        private uint FileNameOffset = 0;
        private byte[] FileNameData;

        private PackType type;

        public event EventHandler FinishedLoading;

        public event EventHandler HeaderLoaded;

        public event EventHandler Modified;

        public event EventHandler PackedFileLoaded;

        public PackFile(string filepath)
        {
            if (File.Exists(filepath))
            {
                Open(filepath);
            }
            else
            {
                this.filepath = filepath;
                relativePathAnchor = Path.GetDirectoryName(filepath);
                type = PackType.Release;
                size = 0L;
                fileList = new SortedList<string, PackedFile>();
            }
        }

        public PackedFile Add(string filepath)
        {
            if (this.fileList.ContainsKey(filepath))
            {
                throw new ArgumentException("filepath already exists in pack");
            }
            PackedFile file = new PackedFile(this, filepath);
            this.fileList.Capacity++;
            this.fileList.Add(filepath, file);
            return file;
        }

        public PackedFile AddData(string filepath, byte[] data)
        {
            if (this.fileList.ContainsKey(filepath))
            {
                throw new ArgumentException("filepath already exists in pack");
            }
            PackedFile file = new PackedFile(this, Convert.ToUInt32(data.Length), filepath, 0L);
            file.ReplaceData(data);
            this.fileList.Capacity++;
            this.fileList.Add(filepath, file);
            return file;
        }

        public PackedFile AddEmptyFile(string filepath)
        {
            return this.AddData(filepath, new byte[0]);
        }

        public void AddRange(string[] filepaths)
        {
            foreach (string str in filepaths)
            {
                this.Add(str);
            }
        }

        public bool Contains(string filepath)
        {
            return this.fileList.ContainsKey(filepath);
        }

        public void Delete(PackedFile packedFile)
        {
            this.fileList[packedFile.Filepath].Delete();
        }

        public static PackType GetPackType(string filepath)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open), Encoding.ASCII))
            {
                if (new string(reader.ReadChars(4)) != "PFH2")
                {
                    throw new ArgumentException("not a pack file", "filepath");
                }
                byte num = reader.ReadByte();
                if (num > 4)
                {
                    throw new InvalidDataException("unknown pack type");
                }
                return (PackType)num;
            }
        }

        private void OnFinishedLoading()
        {
            if (this.FinishedLoading != null)
            {
                this.FinishedLoading(this, EventArgs.Empty);
            }
        }

        private void OnHeaderLoaded()
        {
            if (this.HeaderLoaded != null)
            {
                this.HeaderLoaded(this, EventArgs.Empty);
            }
        }

        internal void OnModified()
        {
            this.isModified = true;
            if (this.Modified != null)
            {
                this.Modified(this, EventArgs.Empty);
            }
        }

        private void OnPackedFileLoaded()
        {
            if (this.PackedFileLoaded != null)
            {
                this.PackedFileLoaded(this, EventArgs.Empty);
            }
        }

        public void Open(string packFilepath)
        {
            filepath = packFilepath;
            relativePathAnchor = Path.GetDirectoryName(packFilepath);
            fileList = new SortedList<string, PackedFile>();
            this.isModified = false;
            using (var reader = new BinaryReader(new FileStream(packFilepath, FileMode.Open), Encoding.ASCII))
            {
                ulong num8;
                this.PackIdentifier = new string(reader.ReadChars(4));
                if (this.PackIdentifier != "PFH0" && this.PackIdentifier != "PFH2" && this.PackIdentifier != "PFH3")
                {
                    throw new ArgumentException("not a pack file", "packFilepath");
                }
                int PackType = reader.ReadInt32();
                if (PackType > 4)
                {
                    throw new InvalidDataException("unknown pack type");
                }
                this.type = (PackType) PackType;
                int num2 = reader.ReadInt32(); // previous files? or files overwritten
                this.FilesOverwritten = (uint) num2;
                this.FileNameOffset = reader.ReadUInt32();

                uint num4 = 0;
                uint num5 = 0;
                ulong offset = 0L;
                reader.BaseStream.Seek(0x10L, SeekOrigin.Begin);
                num4 = reader.ReadUInt32();
                this.fileList.Capacity = (int) num4;
                num5 = reader.ReadUInt32();
                if (this.PackIdentifier == "PFH2" || this.PackIdentifier == "PFH3")
                {
                    var PackFileTime = reader.ReadInt64();
                    var date = DateTime.FromFileTimeUtc(PackFileTime);
                    if (this.FileNameOffset > 0)
                    {
                        this.FileNameData = reader.ReadBytes((int) this.FileNameOffset); // Read File Names as bytes
                    }
                    offset = 0x20 + this.FileNameOffset + num5;
                }
                else
                {
                    offset = 0x18 + num5;
                }
                this.OnHeaderLoaded();
                num8 = 0L;
                for (uint i = 0; i < num4; i++)
                {
                    /*
                    if (PackType == 0x40) {
                        // reader.ReadInt32();
                        reader.ReadChars(8);
                    }
                     * */
                    uint num10 = 5;
                    uint size = reader.ReadUInt32();
                    StringBuilder builder2 = new StringBuilder();
                    char ch2 = reader.ReadChar();
                    while (ch2 != '\0')
                    {
                        builder2.Append(ch2);
                        ch2 = reader.ReadChar();
                        num10++;
                    }
                    string filename = builder2.ToString();
                    int j = 1;
                    while (this.fileList.ContainsKey(filename))
                    {
                        filename = string.Format("{0}_{1}", builder2, j++);
                    }
                    this.fileList.Add(builder2.ToString(), new PackedFile(this, size, filename, offset));
                    offset += size;
                    num8 += num10;
                    this.OnPackedFileLoaded();
                }
            }
            this.OnFinishedLoading();
        }

        public void Replace(PackedFile packedFile, string filepath)
        {
            this.fileList[packedFile.Filepath].Replace(filepath);
        }

        public void Save()
        {
            string tempFileName = Path.GetTempFileName();
            this.writeToFile(tempFileName);
            File.Delete(this.filepath);
            File.Move(tempFileName, this.filepath);
            this.Open(this.filepath);
        }

        public void SaveAs(string filepath)
        {
            this.writeToFile(filepath);
            this.Open(filepath);
        }

        public bool TryGetValue(string filepath, out PackedFile packedFile)
        {
            string relativePathAnchor = this.RelativePathAnchor;
            if (!relativePathAnchor.EndsWith(@"\"))
            {
                relativePathAnchor = relativePathAnchor + @"\";
            }
            filepath = filepath.Replace(relativePathAnchor, "");
            return this.fileList.TryGetValue(filepath, out packedFile);
        }

        private void writeToFile(string filepath)
        {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(filepath, FileMode.Create), Encoding.ASCII))
            {
                writer.Write(this.PackIdentifier.ToCharArray());
                writer.Write((int)this.type);
                writer.Write((int)this.FilesOverwritten);
                writer.Write((int)this.FileNameOffset);
                int num = 0;
                int num2 = 0;
                foreach (PackedFile file in this.fileList.Values)
                {
                    if (file.Action is PackedFile.RenamePackAction)
                    {
                        num++;
                        num2 += (file.Action as PackedFile.RenamePackAction).filepath.Length + 5;
                    }
                    else if (!(file.Action is PackedFile.DeleteFilePackAction))
                    {
                        num++;
                        num2 += file.Filepath.Length + 5;
                    }
                }
                writer.Write(num);
                writer.Write(num2);

                // File Time
                Int64 fileTime = DateTime.Now.ToFileTimeUtc();
                if (this.PackIdentifier == "PFH2")
                {
                    writer.Write(fileTime);
                }

                // Write File Names stored from opening the file
                if (this.FileNameOffset > 0)
                {
                    writer.Write(this.FileNameData);
                }

                foreach (PackedFile file in this.fileList.Values)
                {
                    if (file.Action is PackedFile.RenamePackAction)
                    {
                        writer.Write(file.Size);
                        writer.Write((file.Action as PackedFile.RenamePackAction).filepath.ToCharArray());
                        writer.Write('\0');
                    }
                    else if (file.Action is PackedFile.ReplaceFilePackAction)
                    {
                        writer.Write((int)new FileInfo((file.Action as PackedFile.ReplaceFilePackAction).filepath).Length);
                        writer.Write(file.Filepath.ToCharArray());
                        writer.Write('\0');
                    }
                    else if (!(file.Action is PackedFile.DeleteFilePackAction))
                    {
                        writer.Write(file.Size);
                        writer.Write(file.Filepath.ToCharArray());
                        writer.Write('\0');
                    }
                }
                foreach (PackedFile file in this.fileList.Values)
                {
                    if (file.Size > 0)
                    {
                        writer.Write(file.Data);
                    }
                }
            }
        }

        public int FileCount
        {
            get
            {
                return this.fileList.Capacity;
            }
        }

        public ReadOnlyCollection<PackedFile> FileList
        {
            get
            {
                return new ReadOnlyCollection<PackedFile>(this.fileList.Values);
            }
        }

        public string Filepath
        {
            get
            {
                return this.filepath;
            }
        }

        public bool IsModified
        {
            get
            {
                return this.isModified;
            }
        }

        public string RelativePathAnchor
        {
            get
            {
                return this.relativePathAnchor;
            }
            set
            {
                this.relativePathAnchor = value;
            }
        }

        public ulong Size
        {
            get
            {
                return this.size;
            }
        }

        public PackType Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
            }
        }
    }
}

