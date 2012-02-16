﻿using System;
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
				// I'm guessing... they year doesn't seem to make sense though.
				long time = -1;
				for (int i = 0; i < file.Header.FileCount; i++) {
					uint size = reader.ReadUInt32 ();
					sizes += size;
					if (file.Header.Type == PackType.BootX) {
						time = reader.ReadInt64 ();
					} else {
						time = -1;
					}
					StringBuilder builder2 = new StringBuilder ();
					char ch2 = reader.ReadChar ();
					while (ch2 != '\0') {
						builder2.Append (ch2);
						ch2 = reader.ReadChar ();
						// this is easier because we can use the Path methods
						// under both Windows and Unix
						if (ch2 == '\\') {
							ch2 = Path.DirectorySeparatorChar;
						}
					}
					string packedFileName = builder2.ToString ();
					PackedFile packed = new PackedFile (file.Filepath, packedFileName, offset, size);
					if (time != -1) {
						packed.EditTime = new DateTime (time);
					}
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
			if (packType > 4 && packType != 0x40) {
				throw new InvalidDataException ("Unknown pack type " + packType);
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
            if (header.Version == 1) {
                // read pack file reference
                header.ReplacedPackFileName =
                    new string(ASCIIEncoding.ASCII.GetChars(reader.ReadBytes(replacedPackFilenameLength - 1)));
                // skip the null byte
                reader.ReadByte();
                header.DataStart += replacedPackFilenameLength;
            }
			return header;
		}

        public void writeToFile(string FullPath, PackFile packFile) {
			using (BinaryWriter writer = new BinaryWriter(new FileStream(FullPath, FileMode.Create), Encoding.ASCII)) {
				writer.Write (packFile.Header.PackIdentifier.ToCharArray ());
				writer.Write ((int)packFile.Header.Type);
				writer.Write ((int)packFile.Header.Version);
				if (packFile.Header.ReplacedPackFileName.Length != 0) {
					// if we write a string at all, account for 0 byte
					writer.Write ((int)packFile.Header.ReplacedPackFileName.Length + 1);
				} else {
					writer.Write ((int)0);
				}
				UInt32 indexSize = 0;
				List<PackedFile> toWrite = new List<PackedFile> ((int)packFile.Header.FileCount);
				foreach (PackedFile file in packFile.Files) {
					if (!file.Deleted) {
						if (file.Size != 0) {
							indexSize += (uint)file.FullPath.Length + 5;
							if (packFile.Header.Type == PackType.BootX) {
								// additional bytes for time
								indexSize += 8;
							}
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
				if (packFile.Header.ReplacedPackFileName.Length > 0) {
					writer.Write (packFile.Header.ReplacedPackFileName.ToCharArray ());
					writer.Write ((byte)0);
				}

				// write file list
				string separatorString = "" + Path.DirectorySeparatorChar;
				foreach (PackedFile file in toWrite) {
					if (file.Size != 0) {
						writer.Write ((int)file.Size);
						if (packFile.Header.Type == PackType.BootX) {
							writer.Write (file.EditTime.Ticks);
						}
						// pack pathes use backslash, we replaced when reading
						string packPath = file.FullPath.Replace (separatorString, "\\");
						writer.Write (packPath.ToCharArray ());
						writer.Write ('\0');
					}
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
}
