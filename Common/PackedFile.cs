namespace Common {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /*
     * Any entry in the Pack file.
     * Has a name and a path designating its full position.
     */
    public abstract class PackEntry : IComparable<PackEntry> {
        // Event triggered when name is about to be changed (called before actual change)
        public delegate void Renamed(PackEntry dir, String newName);
        public event Renamed RenameEvent;

        // Event triggered if any modification occurred on this entry
        // (called before modification is committed)
        public delegate void Modification(PackEntry file);
        public event Modification ModifiedEvent;

        // This Entry's Parent Entry
        public PackEntry Parent { get; set; }

        // Name; calls RenameEvent if changed
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

        // Path
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

        // Tag whether entry is tagged for deletion
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

        // Tag whether entry has been modified
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

        // Compare by entry names
        public int CompareTo(PackEntry entry) {
            return entry != null ? Name.CompareTo(entry.Name) : 0;
        }
    }

    /*
     * A pack entry containing actual data.
     * Data is provided by a DataSource object.
     */
    [DebuggerDisplay("{Name}")]
    public class PackedFile : PackEntry {
        public DateTime EditTime {
            get;
            set;
        }

        private static readonly byte[] EMPTY = new byte[0];

        #region File Data access
        // retrieve the amount of available data
        public long Size {
            get { return Source.Size; }
        }
        // Retrieve the data from this object's source
        public byte[] Data {
            get {
                return Source == null ? EMPTY : Source.ReadData();
            }
            set {
                Source = new MemorySource(value);
                Modified = true;
                EditTime = DateTime.Now;
            }
        }
        // the data source object itself
        DataSource source;

        public DataSource Source {
            get { return source; }
            set {
                source = value;
                Modified = true;
                EditTime = DateTime.Now;
            }
        }
        #endregion

        #region Constructors
        public PackedFile() { }
        public PackedFile(string filename) {
            Name = Path.GetFileName(filename);
            Source = new FileSystemSource(filename);
            Modified = false;
            EditTime = File.GetLastWriteTime(filename);
        }
        public PackedFile(string packFile, string packedName, long offset, long len) {
            Name = Path.GetFileName(packedName);
            Source = new PackedFileSource(packedName, packFile, offset, len);
            Modified = false;
            EditTime = File.GetLastWriteTime(packFile);
        }
        #endregion
    }

    /*
     * A pack file entry that can contain other entries.
     * Provides additional events for content changes.
     * Note that entries are usually not removed from this; instead they are tagged
     * with "Deleted" and just not added anymore when the full model is rebuilt
     * the next time around.
     */
    public class VirtualDirectory : PackEntry {
        public delegate void ContentsEvent(PackEntry entry);
        // triggered when content is added
        public event ContentsEvent DirectoryAdded;
        public event ContentsEvent FileAdded;
        // triggered when file is removed
        public event ContentsEvent FileRemoved;

        // override deletion to tag all contained objects as deleted as well
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

        #region Contained entry access
        // the contained directories
        private SortedSet<VirtualDirectory> subdirectories = new SortedSet<VirtualDirectory> ();
        public SortedSet<VirtualDirectory> Subdirectories {
            get {
                return subdirectories;
            }
        }
        // the contained files
        private SortedSet<PackedFile> containedFiles = new SortedSet<PackedFile> ();
        public SortedSet<PackedFile> Files {
            get {
                return containedFiles;
            }
        }
        /*
         * Retrieve a list with all contained entries (files and directories).
         */
        public List<PackEntry> Entries {
            get {
                List<PackEntry> entries = new List<PackEntry> ();
                entries.AddRange (containedFiles);
                entries.AddRange (subdirectories);
                return entries;
            }
        }

        /*
         * Retrieve the directory with the given name.
         * Will create and add an empty one if it doesn't already exists.
         */
        public VirtualDirectory getSubdirectory(string subDir) {
            VirtualDirectory result = null;
            foreach (VirtualDirectory dir in subdirectories) {
                if (dir.Name.Equals (subDir)) {
                    result = dir;
                    break;
                }
            }
            if (result == null) {
                result = new VirtualDirectory { Parent = this, Name = subDir };
                Add (result);
            }
            return result;
        }

        /*
         * Retrieve the contained file with the given name.
         * Will return null if file does not exist.
         */
        public PackedFile GetFile(string name) {
            PackedFile result = null;
            foreach (PackedFile file in containedFiles) {
                if (file.Name.Equals (name)) {
                    result = file;
                    break;
                }
            }
            return result;
        }
        #endregion

        #region Add entries
        /*
         * Add the given directory to this.
         * Will notify the DirectoryAdded event after adding.
         */
        public void Add(VirtualDirectory dir) {
            subdirectories.Add(dir);
            dir.Parent = this;
            if (DirectoryAdded != null) {
                DirectoryAdded(dir);
            }
        }
        /*
         * Add the given content file.
         * Will notify the FileAdded event after adding.
         * If a file with the given name already exists and is deleted,
         * it is replaced with the given one.
         * If a file with the given name already exists and is not deleted,
         * an exception is thrown.
         */
        public void Add(PackedFile file) {
            if (containedFiles.Contains (file)) {
                PackedFile contained = null;
                foreach (PackedFile f in containedFiles) {
                    if (f.Name.Equals (file.Name)) {
                        contained = f;
                        break;
                    }
                }
                if (contained.Deleted) {
                    containedFiles.Remove (contained);
                    if (FileRemoved != null) {
                        FileRemoved (contained);
                    }
                } else {
                    // don't add the file
                    // probably best to add a notification event parameter
                    // to provide feedback about this
                    return;
                }
            }
            containedFiles.Add (file);
            file.Parent = this;
            if (FileAdded != null) {
                FileAdded (file);
            }
        }

        /*
         * Adds all file from the given directory path.
         */
        public void Add(string basePath) {
            string[] files = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
            foreach (string filepath in files) {
                string relativePath = filepath.Replace(Path.GetDirectoryName(basePath), "");
                Add(relativePath, new PackedFile(filepath));
            }
        }
        /*
         * Adds the given file to the given path, relative to this directory.
         */
        public void Add(string relativePath, PackedFile file) {
            char[] splitAt = { Path.DirectorySeparatorChar };
            string baseDir = Path.GetDirectoryName(relativePath);
            string[] dirs = baseDir != null ? baseDir.Split(splitAt, StringSplitOptions.RemoveEmptyEntries) : new string[0];
            VirtualDirectory current = this;
            if (dirs.Length > 0) {
                foreach (string dir in dirs) {
                    current = current.getSubdirectory(dir);
                }
            }
            file.Parent = current;
            current.Add(file);
        }
        #endregion
    }

    /*
     * A class providing data for a PackedFile content object.
     */
    public abstract class DataSource {
        public long Size {
            get;
            protected set;
        }
        public abstract byte[] ReadData();
    }

    /* Provides data from the local filesystem */
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
    /* Provides data from heap memory */
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
    /* Provides data from within a pack file */
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
