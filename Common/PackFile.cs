using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Common {
    [DebuggerDisplay("Filepath = {Filepath}")]
    public class PackFile {
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

        public PackFile(string path, PFHeader h) {
            header = h;
            Filepath = path;
            Root = new VirtualDirectory() { Name = Path.GetFileName(path) };
            DirAdded(Root);
        }

        public void Add(string fullPath, PackedFile file) {
            Root.Add(fullPath, file);
        }

        #region Entry Access
        // retrieves the names of all entries (directories and packed files)
        public List<string> FileList {
            get {
                List<string> result = new List<string>((int)header.FileCount);
                listAll(result, "", Root);
                return result;
            }
        }
        // lists all contained packed files
        public List<PackedFile> Files {
            get {
                List<PackedFile> result = new List<PackedFile>((int)header.FileCount);
                retrieveAll(result, Root);
                return result;
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
                List<string> result = new List<string>((int)header.FileCount);
                listAll(result, "", Root, false);
                return result.Count;
            }
        }
        #endregion

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

        #region Entry Iteration utility functions
        private static void listAll(List<string> addTo, string currentPath, VirtualDirectory filesFrom, bool includeDirs = true) {
            // add all files from subdirectories first
            foreach (VirtualDirectory dir in filesFrom.Subdirectories) {
                listAll(addTo, currentPath + Path.DirectorySeparatorChar + dir.Name, dir);
            }
            string newPath = currentPath + Path.DirectorySeparatorChar + filesFrom;
            if (includeDirs) {
                // add this path
                addTo.Add(newPath);
            }
            // add file names
            foreach (PackedFile file in filesFrom.Files) {
                addTo.Add(currentPath + Path.DirectorySeparatorChar + file.Name);
            }
        }
        private static void retrieveAll(List<PackedFile> files, VirtualDirectory filesFrom) {
            foreach (VirtualDirectory dir in filesFrom.Subdirectories) {
                retrieveAll(files, dir);
            }
            files.AddRange(filesFrom.Files);
        }
        #endregion
    }

    /*
     * Class containing general pack file information.
     */
    public class PFHeader {
        string identifier;

        public PFHeader(string id) {
            PackIdentifier = id;
            FileCount = 0;
            Version = 0;
            ReplacedPackFileName = "";
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
        // query/set name of pack file replaced by this
        public string ReplacedPackFileName {
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
                        // PFH2/3 contains a FileTime at 0x1C (I think) in addition to PFH0's header
                        result = 0x20;
                        break;
                    default:
                        // if this ever happens, go have a word with MS
                        throw new Exception("Unknown header ID " + PackIdentifier);
                }
                return result;
            }
        }
    }

    public enum PackType {
        // up to movie, ids are sequential
        Boot,
        Release,
        Patch,
        Mod,
        Movie,
        // have to force id value for boot; there are more of those special ones,
        // but we can't handle them yet
        BootX = 0x40
    }
}