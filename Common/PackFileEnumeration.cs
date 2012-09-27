using System;
using System.IO;
using System.Collections.Generic;

namespace Common {
    #region Single Pack File Iteration
    /*
     * Enumerates the packed files of a single pack.
     */
    public class PackedFileEnumerable : IEnumerable<PackedFile> {
        private string filepath;
        public PackedFileEnumerable(string path) {
            filepath = path;
        }
        public IEnumerator<PackedFile> GetEnumerator() {
            return new PackFileEnumerator(filepath);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new PackFileEnumerator(filepath);
        }
        public override string ToString() {
            return filepath;
        }
    }
    public class PackFileEnumerator : IEnumerator<PackedFile>, IDisposable {
        BinaryReader reader;
        PFHeader header;
        long offset;
        uint currentFileIndex;
        string filepath;
        PackedFile currentFile = null;
        long startPosition;
        public PFHeader Header {
            get {
                return header;
            }
        }
        public PackFileEnumerator(string path) {
            filepath = path;
            reader = new BinaryReader(File.OpenRead(path));
            header = PackFileCodec.ReadHeader(reader);
            startPosition = reader.BaseStream.Position;
            Reset();
        }

        public void Reset() {
            currentFileIndex = 0;
            reader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
            offset = header.DataStart;
            currentFile = null;
        }

        public bool MoveNext() {
            currentFileIndex++;
            if (currentFileIndex > header.FileCount) {
                return false;
            } 
            uint size = reader.ReadUInt32();
            switch (Header.Type) {
                case PackType.BootX:
                case PackType.Shader1:
                case PackType.Shader2:
                    header.AdditionalInfo = reader.ReadInt64();
                    break;
                default:
                    break;
            }
            try {
                string packedFileName = IOFunctions.readZeroTerminatedAscii(reader);
                // this is easier because we can use the Path methods
                // under both Windows and Unix
                packedFileName = packedFileName.Replace('\\', Path.DirectorySeparatorChar);

                currentFile = new PackedFile(filepath, packedFileName, offset, size);
                offset += size;

                return true;
            } catch (Exception ex) {
                Console.WriteLine("Failed enumeration of {2}/{3} file in {0}: {1}", 
                    Path.GetFileName(filepath), ex, currentFileIndex, header.FileCount);
                Console.WriteLine("Current position in file: {0}; last succesful file: {1}", 
                    reader.BaseStream.Position, Current.FullPath);
            }
            return false;
        }

        public void Dispose() {
            reader.Dispose();
        }

        public PackedFile Current {
            get {
                return currentFile;
            }
        }
        object System.Collections.IEnumerator.Current {
            get {
                return Current;
            }
        }
    }
    #endregion

    #region Multiple Pack File Enumeration
    /*
     * Enumerates the packed files across several packs.
     */
    public class MultiPackEnumerable : IEnumerable<PackedFile> {
        IEnumerable<string> paths;
        public MultiPackEnumerable(IEnumerable<string> files) {
            paths = files;
        }
        public IEnumerator<PackedFile> GetEnumerator() {
            return new MultiPackEnumerator(paths);
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return new MultiPackEnumerator(paths);
        }
        public override string ToString() {
            return string.Join(new string(Path.PathSeparator, 1), paths);
        }
    }

    public class MultiPackEnumerator : DelegatingEnumerator<PackedFile> {
        IEnumerator<string> paths;
        public MultiPackEnumerator(IEnumerable<string> files) {
            paths = files.GetEnumerator();
        }
        public override void Reset() {
            base.Reset();
            paths.Reset();
        }
        protected override IEnumerator<PackedFile> NextEnumerator() {
            IEnumerator<PackedFile> result = null;
            if (paths.MoveNext()) {
                result = new PackFileEnumerator(paths.Current);
            }
            return result;
        }
        public override void Dispose() {
            base.Dispose();
            paths.Dispose();
        }
    }
    #endregion
}

