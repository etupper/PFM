namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    public abstract class PackEntry : IComparable<PackEntry> {
        public delegate void Renamed(PackEntry dir, String newName);
        public event Renamed RenameEvent;
        public delegate void Modification(PackEntry file);
        public event Modification ModifiedEvent;

        public PackEntry Parent { get; set; }
        string name;
        public string Name {
            get {
                return name;
            }
            set {
                if (RenameEvent != null) {
                    RenameEvent(this, value);
                }
                name = value;
            }
        }
        public string FullPath {
            get {
                string result = Name;
                PackEntry p = Parent;
                while (p != null) {
                    result = p.Name + Path.DirectorySeparatorChar + result;
                    p = p.Parent;
                }
                int index = result.IndexOf(Path.DirectorySeparatorChar);
                if (index != -1) {
                    result = result.Substring(index + 1);
                }
                return result;
            }
        }

        private bool deleted = false;
        public virtual bool Deleted {
            get {
                return deleted;
            }
            set {
                deleted = value;
                Modified = true;
            }
        }
        bool modified;
        public bool Modified {
            get { return modified; }
            set {
                if (modified != value) {
                    modified = value;
                    if (ModifiedEvent != null) {
                        ModifiedEvent(this);
                    }
                    if (Parent != null) {
                        Parent.Modified = value;
                    }
                }
            }
        }
        public int CompareTo(PackEntry entry) {
            return entry != null ? Name.CompareTo(entry.Name) : 0;
        }
    }

    [DebuggerDisplay("{Name}")]
    public class PackedFile : PackEntry {
		public DateTime EditTime {
			get;
			set;
		}

        private static readonly byte[] EMPTY = new byte[0];

        public byte[] Data {
			get {
				return Source == null ? EMPTY : Source.ReadData ();
			}
			set {
				Source = new MemorySource (value);
				Modified = true;
				EditTime = DateTime.Now;
			}
		}
        public long Size {
            get { return Source.Size; }
        }
        DataSource source;
        public DataSource Source {
			get { return source; }
			set {
				source = value;
				Modified = true;
				EditTime = DateTime.Now;
			}
		}
        public PackedFile() { }
        public PackedFile (string filename) {
			Name = Path.GetFileName (filename);
			Source = new FileSystemSource (filename);
			Modified = false;
			EditTime = File.GetLastWriteTime (filename);
        }
        public PackedFile (string packFile, string packedName, long offset, long len) {
			Name = Path.GetFileName (packedName);
			Source = new PackedFileSource (packedName, packFile, offset, len);
			Modified = false;
			EditTime = File.GetLastWriteTime (packFile);
        }
        // public abstract byte[] ReadData();

        public int CompareTo(object o) {
            PackedFile file = o as PackedFile;
            return file != null ? CompareTo(file.Name) : 0;
        }
    }


    public class VirtualDirectory : PackEntry {
        public delegate void ContentsEvent(PackEntry entry);
        public event ContentsEvent DirectoryAdded;
        public event ContentsEvent FileAdded;
        public event ContentsEvent FileRemoved;

        public SortedSet<VirtualDirectory> Subdirectories {
            get {
                return subdirectories;
            }
        }
        public SortedSet<PackedFile> Files {
            get {
                return containedFiles;
            }
        }
        private SortedSet<VirtualDirectory> subdirectories = new SortedSet<VirtualDirectory>();
        private SortedSet<PackedFile> containedFiles = new SortedSet<PackedFile>();

        public override bool Deleted {
            get {
                return base.Deleted;
            }
            set {
                foreach (PackedFile file in containedFiles) {
                    file.Deleted = value;
                }
                foreach (VirtualDirectory dir in subdirectories) {
                    dir.Deleted = value;
                }
                base.Deleted = value;
            }
        }

        public VirtualDirectory getSubdirectory(string subDir) {
            VirtualDirectory result = null;
            foreach (VirtualDirectory dir in subdirectories) {
                if (dir.Name.Equals(subDir)) {
                    result = dir;
                    break;
                }
            }
            if (result == null) {
                result = new VirtualDirectory { Parent = this, Name = subDir };
                Add(result);
            }
            return result;
        }
        public PackedFile GetFile(string name) {
            PackedFile result = null;
            foreach (PackedFile file in containedFiles) {
                if (file.Name.Equals(name)) {
                    result = file;
                    break;
                }
            }
            return result;
        }
        public void Add(VirtualDirectory dir) {
            subdirectories.Add(dir);
            if (DirectoryAdded != null) {
                DirectoryAdded(dir);
            }
        }
        public void Add(PackedFile file) {
            if (containedFiles.Contains(file)) {
                PackedFile contained = null;
                foreach (PackedFile f in containedFiles) {
                    if (f.Name.Equals(file.Name)) {
                        contained = f;
                        break;
                    }
                }
                if (contained.Deleted) {
                    containedFiles.Remove(contained);
                    if (FileRemoved != null) {
                        FileRemoved(contained);
                    }
                } else {
                    throw new Exception("File already present");
                }
            }
            containedFiles.Add(file);
            file.Parent = this;
            if (FileAdded != null) {
                FileAdded(file);
            }
        }
        public List<PackEntry> Entries {
            get {
                List<PackEntry> entries = new List<PackEntry>();
                entries.AddRange(containedFiles);
                entries.AddRange(subdirectories);
                return entries;
            }
        }

        /*
         * Adds all file from the given directory path.
         */
        public void Add(string basePath) {
			string[] files = Directory.GetFiles (basePath, "*.*", SearchOption.AllDirectories);
			foreach (string filepath in files) {
				string relativePath = filepath.Replace (Path.GetDirectoryName (basePath), "");
				Add (relativePath, new PackedFile (filepath));
			}
		}
        public void Add(string relativePath, PackedFile file) {
			char[] splitAt = { Path.DirectorySeparatorChar };
			string[] dirs = Path.GetDirectoryName (relativePath).Split (splitAt, StringSplitOptions.RemoveEmptyEntries);
			VirtualDirectory current = this;
			if (dirs.Length > 0) {
				foreach (string dir in dirs) {
					current = current.getSubdirectory (dir);
				}
			}
			file.Parent = current;
			current.Add (file);
		}
    }
    public abstract class DataSource {
        public long Size {
            get;
            protected set;
        }
        public abstract byte[] ReadData();
    }
    
    [DebuggerDisplay("From file {filepath}")]
    public class FileSystemSource : DataSource {
        protected string filepath;
        public FileSystemSource(string filepath)
            : base() {
            Size = new FileInfo(filepath).Length;
            this.filepath = filepath;
        }
        public override byte[] ReadData() {
            return File.ReadAllBytes(filepath);
        }
    }
    [DebuggerDisplay("From Memory")]
    public class MemorySource : DataSource {
        private byte[] data;
        public MemorySource(byte[] data) {
            Size = data.Length;
            this.data = data;
        }
        public override byte[] ReadData() {
            return data;
        }
    }
    [DebuggerDisplay("{Offset}@{filepath}")]
    public class PackedFileSource : DataSource {
        private string filepath;
        public long Offset {
            get;
            private set;
        }
        public PackedFileSource(string pathInPack, string packfilePath, long offset, long length) {
            Offset = offset;
            filepath = packfilePath;
            Size = length;
        }
        public override byte[] ReadData() {
            byte[] data = new byte[Size];
            using (Stream stream = File.OpenRead(filepath)) {
                stream.Seek(Offset, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);
            }
            return data;
        }
    }
}

