using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common {

    /*
     * Reads and writes Pack files from and to filesystem files.
     * I guess we could generalize to streams, but not much point to that for now.
     */
    public class PackFileCodec {
        public delegate void HeaderLoadedEvent(PFHeader header);
        public delegate void PackedFileLoadedEvent(PackedFile packed);
        public delegate void PackFileLoadedEvent(PackFile pack);

        public event HeaderLoadedEvent HeaderLoaded;
        public event PackedFileLoadedEvent PackedFileLoaded;
        public event PackFileLoadedEvent PackFileLoaded;
		
        public PackFile Open(string packFullPath) {
			PackFile file;
			long sizes = 0;
			using (var reader = new BinaryReader(new FileStream(packFullPath, FileMode.Open), Encoding.ASCII)) {
				PFHeader header = readHeader (reader);
				file = new PackFile (packFullPath, header);
				OnHeaderLoaded (header);

				long offset = file.Header.DataStart;
				for (int i = 0; i < file.Header.FileCount; i++) {
					uint size = reader.ReadUInt32 ();
					sizes += size;
                    switch (file.Header.Type) {
                        case PackType.BootX:
                        case PackType.Shader1:
                        case PackType.Shader2:
                            header.AdditionalInfo = reader.ReadInt64();
                            break;
                        default:
                            break;
                    }
                    string packedFileName = IOFunctions.readZeroTerminatedAscii(reader);
                    // this is easier because we can use the Path methods
                    // under both Windows and Unix
                    packedFileName = packedFileName.Replace('\\', Path.DirectorySeparatorChar);

					PackedFile packed = new PackedFile (file.Filepath, packedFileName, offset, size);
					file.Add (packedFileName, packed);
					offset += size;
					this.OnPackedFileLoaded (packed);
				}
			}
			this.OnFinishedLoading (file);
			file.IsModified = false;
			return file;
		}
		
		public virtual PFHeader readHeader(BinaryReader reader) {
			PFHeader header;
			string packIdentifier = new string (reader.ReadChars (4));
			header = new PFHeader (packIdentifier);
			int packType = reader.ReadInt32 ();
            switch (packType) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 0x40:
                case 0x41:
                case 0x42:
                    break;
                default:
                    throw new InvalidDataException("Unknown pack type " + packType);
            }
			header.Type = (PackType)packType;
			header.Version = reader.ReadInt32 ();
			int replacedPackFilenameLength = reader.ReadInt32 ();
			reader.BaseStream.Seek (0x10L, SeekOrigin.Begin);
			header.FileCount = reader.ReadUInt32 ();
			UInt32 indexSize = reader.ReadUInt32 ();
			header.DataStart = header.Length + indexSize;

			// skip the time
			reader.BaseStream.Seek (header.Length, SeekOrigin.Begin);
            for (int i = 0; i < header.Version; i++) {
                header.ReplacedPackFileNames.Add(IOFunctions.readZeroTerminatedAscii(reader));
            }
            header.DataStart += replacedPackFilenameLength;
			return header;
		}

        public void writeToFile(string FullPath, PackFile packFile) {
            using (BinaryWriter writer = new BinaryWriter(new FileStream(FullPath, FileMode.Create), Encoding.ASCII)) {
                writer.Write (packFile.Header.PackIdentifier.ToCharArray ());
                writer.Write ((int)packFile.Header.Type);
                writer.Write ((int)packFile.Header.Version);
                writer.Write (packFile.Header.ReplacedFileNamesLength);
                UInt32 indexSize = 0;
                List<PackedFile> toWrite = new List<PackedFile> ((int)packFile.Header.FileCount);
                foreach (PackedFile file in packFile.Files) {
                    if (!file.Deleted) {
                        indexSize += (uint)file.FullPath.Length + 5;
                        switch (packFile.Header.Type) {
                        case PackType.BootX:
                        case PackType.Shader1:
                        case PackType.Shader2:
                            indexSize += 8;
                            break;
                        default:
                            break;
                        }
                        toWrite.Add (file);
                    }
                }
                writer.Write (toWrite.Count);
                writer.Write (indexSize);

                // File Time
                if (packFile.Header.PackIdentifier == "PFH2" || packFile.Header.PackIdentifier == "PFH3") {
                    Int64 fileTime = DateTime.Now.ToFileTimeUtc ();
                    writer.Write (fileTime);
                }

                // Write File Names stored from opening the file
                foreach (string replacedPack in packFile.Header.ReplacedPackFileNames) {
                    writer.Write (replacedPack.ToCharArray ());
                    writer.Write ((byte)0);
                }

                // pack entries are stored alphabetically in pack files
                toWrite.Sort (new PackedFileNameComparer ());

                // write file list
                string separatorString = "" + Path.DirectorySeparatorChar;
                foreach (PackedFile file in toWrite) {
                    writer.Write ((int)file.Size);
                    switch (packFile.Header.Type) {
                    case PackType.BootX:
                    case PackType.Shader1:
                    case PackType.Shader2:
                        writer.Write(packFile.Header.AdditionalInfo);
                        break;
                    default:
                        break;
                    }
                    // pack pathes use backslash, we replaced when reading
                    string packPath = file.FullPath.Replace (separatorString, "\\");
                    writer.Write (packPath.ToCharArray ());
                    writer.Write ('\0');
                }
                foreach (PackedFile file in toWrite) {
                    if (file.Size > 0) {
                        byte[] bytes = file.Data;
                        writer.Write (bytes);
                    }
                }
            }
        }

        private void OnHeaderLoaded(PFHeader header) {
            if (this.HeaderLoaded != null) {
                this.HeaderLoaded(header);
            }
        }
        private void OnFinishedLoading(PackFile pack) {
            if (this.PackFileLoaded != null) {
                this.PackFileLoaded(pack);
            }
        }
        private void OnPackedFileLoaded(PackedFile packed) {
            if (this.PackedFileLoaded != null) {
                this.PackedFileLoaded(packed);
            }
        }
    }

    /*
     * Compares two PackedFiles by name.
     */
    class PackedFileNameComparer : IComparer<PackedFile> {
        public int Compare(PackedFile a, PackedFile b) {
            return a.FullPath.CompareTo(b.FullPath);
        }
    }
}
