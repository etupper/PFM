using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Common {
    /*
     * A Pack file containing data in form of PackedFile entries from the Warscape engine.
     */
    [DebuggerDisplay("Filepath = {Filepath}")]
    public class PackFile : IEnumerable<PackedFile> {
        public delegate void ModifyEvent();
        public event ModifyEvent Modified;

        private PFHeader header;
        private bool modified;

        #region Attributes
        // header access
        public PFHeader Header {
            get { return header; }
        }

        // the path on the file system
        public string Filepath {
            get;
            private set;
        }

        // the root node of this file;
        // named with the file name, stripped from any FullPath query of entries
        public VirtualDirectory Root {
            get;
            private set;
        }

        // Query type from header; calls Modified when set
        public PackType Type {
            get { return Header.Type; }
            set {
                if (value != Header.Type) {
                    Header.Type = value;
                    IsModified = true;
                }
            }
        }
        // Modified attribute, calls Modified event after set
        public bool IsModified {
            get { return modified; }
            set {
                modified = value;
                if (Modified != null) {
                    Modified();
                }
            }
        }
        #endregion
  
        /*
         * Create pack file at the given path with the given header.
         */
        public PackFile(string path, PFHeader h) {
            header = h;
            Filepath = path;
            Root = new VirtualDirectory() { Name = Path.GetFileName(path) };
            DirAdded(Root);
        }
        /*
         * Create PackFile at the given path with a default header of type Mod and type PFH3.
         */
        public PackFile(string path) : this(path, new PFHeader("PFH3") {
            Type = PackType.Mod
        }) {}
        /*
         * Add the given file to this pack.
         */
        public void Add(PackedFile file, bool replace = false) {
            Root.Add(file.FullPath, file, replace);
        }

        #region Entry Access
        // lists all contained packed files
        public List<PackedFile> Files {
            get {
                return Root.AllFiles;
            }
        }
        // retrieves the packed file at the given path name
        public PackedFile this[string filepath] {
            get {
                string[] paths = Path.GetDirectoryName(filepath).Split(Path.DirectorySeparatorChar);
                VirtualDirectory dir = Root;
                foreach (string subDir in paths) {
                    dir = dir.getSubdirectory(subDir);
                }
                return dir.GetFile(Path.GetFileName(filepath));
            }
        }
        public int FileCount {
            get {
                return Root.AllFiles.Count;
            }
        }
        #endregion

        public override string ToString() {
            return string.Format("Pack file {0}", Filepath);
        }

        #region Event Handler for Entries
        // Set self to modified
        private void EntryModified(PackEntry file) {
            IsModified = true;
        }
        // Set modified
        private void EntryRenamed(PackEntry file, string name) {
            EntryModified(file);
        }
        // Register modified and rename handlers
        private void EntryAdded(PackEntry file) {
            file.ModifiedEvent += EntryModified;
            file.RenameEvent += EntryRenamed;
            IsModified = true;
        }
        // Unregister modified and rename handlers
        private void EntryRemoved(PackEntry entry) {
            entry.ModifiedEvent -= EntryModified;
            entry.RenameEvent -= EntryRenamed;
        }
        // Call EntryAdded and register Added and Removed handlers
        private void DirAdded(PackEntry dir) {
            EntryAdded(dir);
            (dir as VirtualDirectory).FileAdded += EntryAdded;
            (dir as VirtualDirectory).DirectoryAdded += DirAdded;
            (dir as VirtualDirectory).FileRemoved += EntryRemoved;
        }
        #endregion

        public IEnumerator<PackedFile> GetEnumerator() {
            return Files.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    /*
     * Class containing general pack file information.
     */
    public class PFHeader {
        string identifier;

        public PFHeader(string id) {
            Type = PackType.Mod;
            // Rome II headers are longer
            DataStart = "PFH4".Equals(id) ? 0x28 : 0x20;
            PackIdentifier = id;
            FileCount = 0;
            Version = 0;
            ReplacedPackFileNames = new List<string>();
        }
        
        /*
         * Create a header from the given one.
         */
        public PFHeader(PFHeader toCopy) : this(toCopy.identifier) {
            Type = toCopy.Type;
            ReplacedPackFileNames.AddRange(toCopy.ReplacedPackFileNames);
        }
  
        /*
         * The lenght in bytes of the entry containing the filenames
         * replaced by this pack file.
         */
        public int ReplacedFileNamesLength {
            get {
                // start with 0 byte for each name
                int result = ReplacedPackFileNames.Count;
                // add actual names' lengths
                ReplacedPackFileNames.ForEach(name => result += name.Length);
                return result;
            }
        }

        // query/set identifier
        // throws Exception if unknown
        public string PackIdentifier {
            get {
                return identifier;
            }
            set {
                switch (value) {
                    case "PFH0":
                    case "PFH2":
                    case "PFH3":
                    case "PFH4":
                        break;
                    default:
                        throw new Exception("Unknown Header Type " + value);
                }
                identifier = value;
            }
        }
        // query/set pack type
        public PackType Type { get; set; }
        // query/set version
        public int Version { get; set; }
        // query/set offset for data in file
        public long DataStart { get; set; }
        // query/set number of contained files
        public UInt32 FileCount { get; set; }
        // query/set names of pack file replaced by this
        public UInt32 Unknown { get; set; }
        public List<string> ReplacedPackFileNames {
            get;
            set;
        }
        // query length of header itself
        public int Length {
            get {
                int result;
                switch (PackIdentifier) {
                    case "PFH0":
                        result = 0x18;
                        break;
                    case "PFH2":
                    case "PFH3":
                        // PFH2+ contain a FileTime at 0x1C (I think) in addition to PFH0's header
                        result = 0x20;
                        break;
                    case "PFH4":
                        result = 0x1C;
                        break;
                    default:
                        // if this ever happens, go have a word with MS
                        throw new Exception("Unknown header ID " + PackIdentifier);
                }
                return result;
            }
        }
        public long AdditionalInfo {
            get; set;
        }
    }
 
    /*
     * Types of pack files.
     */
    public enum PackType {
        // up to movie, ids are sequential
        Boot,
        Release,
        Patch,
        Mod,
        Movie,
        // have to force id value for boot; there are more of those special ones,
        // but we can't handle them yet
        Sound = 17,
        Music = 18,
        Sound1 = 0x17,
        Music1 = 0x18,
        BootX = 0x40,
        Shader2 = 0x41,
        Shader1 = 0x42
    }
}