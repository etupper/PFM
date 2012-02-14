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

        private string filepath;
        public string Filepath {
            get {
                return filepath;
            }
        }
        public List<string> FileList {
            get {
                List<string> result = new List<string>((int)header.FileCount);
                listAll(result, "", root);
                return result;
            }
        }
        public List<PackedFile> Files {
            get {
                List<PackedFile> result = new List<PackedFile>((int)header.FileCount);
                retrieveAll(result, root);
                return result;
            }
        }
        public PackedFile this [string filepath] {
			get {
				string[] paths = Path.GetDirectoryName (filepath).Split (Path.DirectorySeparatorChar);
				VirtualDirectory dir = root;
				foreach (string subDir in paths) {
					dir = dir.getSubdirectory (subDir);
				}
				return dir.GetFile (Path.GetFileName (filepath));
			}
		}
        public PFHeader Header {
            get { return header; }
        }
        public int FileCount {
            get {
                List<string> result = new List<string>((int)header.FileCount);
                listAll(result, "", root, false);
                return result.Count;
            }
        }
        private VirtualDirectory root;
        private PFHeader header;

        public PackFile(string path, PFHeader h) {
            header = h;
            filepath = path;
            root = new VirtualDirectory() { Name = Path.GetFileName(path) };
            DirAdded(root);
        }

        public VirtualDirectory Root {
            get { return root; }
        }

        public PackType Type {
            get { return Header.Type; }
            set { 
                if (value != Header.Type) { 
                    Header.Type = value; 
                    IsModified = true; 
                } 
            }
        }
        private bool modified;
        public bool IsModified {
            get { return modified; }
            set {
                modified = value;
                if (Modified != null) {
                    Modified();
                }
            }
        }
        public void Add(PackedFile file) {
            Add(file.FullPath, file);
        }
        public void Add(string fullPath, PackedFile file) {
            Root.Add(fullPath, file);
        }
        private void EntryModified(PackEntry file) {
            IsModified = true;
        }
        private void EntryRenamed(PackEntry file, string name) {
            EntryModified(file);
        }
        private void EntryAdded(PackEntry file) {
            file.ModifiedEvent += EntryModified;
            file.RenameEvent += EntryRenamed;
        }
        private void EntryRemoved(PackEntry entry) {
            entry.ModifiedEvent -= EntryModified;
            entry.RenameEvent -= EntryRenamed;
        }
        private void DirAdded(PackEntry dir) {
            EntryAdded(dir);
            (dir as VirtualDirectory).FileAdded += EntryAdded;
            (dir as VirtualDirectory).DirectoryAdded += DirAdded;
            (dir as VirtualDirectory).FileRemoved += EntryRemoved;
        }
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

        // private 
    }

    public class PFHeader {
        private string replacedPackFile = "";

        public PFHeader(string id) {
            PackIdentifier = id;
        }

        string identifier;
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
        public PackType Type { get; set; }
        public int Version { get; set; }
        public long DataStart { get; set; }
        public UInt32 FileCount { get; set; }

        public string ReplacedPackFileName {
            get { return replacedPackFile; }
            set { replacedPackFile = value; }
        }

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
        Boot,
        Release,
        Patch,
        Mod,
        Movie,
        BootX = 0x40
    }
}
