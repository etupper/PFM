using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace Filetypes {
	/*
	 * A class parsing dbfiles from and to data streams in packed file format.
	 */
    public class PackedFileDbCodec : Codec<DBFile> {
        string typeName;
		
		public delegate void EntryLoaded(FieldInfo info, string value);
		public delegate void HeaderLoaded(DBFileHeader header);
		public delegate void LoadingPackedFile(PackedFile packed);

        public bool AutoadjustGuid { get; set; }

		#region Internal
		// header markers
		static UInt32 GUID_MARKER = BitConverter.ToUInt32 (new byte[] { 0xFD, 0xFE, 0xFC, 0xFF}, 0);
		static UInt32 VERSION_MARKER = BitConverter.ToUInt32 (new byte[] { 0xFC, 0xFD, 0xFE, 0xFF}, 0);
		#endregion

        public static PackedFileDbCodec GetCodec(PackedFile file) {
            return new PackedFileDbCodec(DBFile.typename(file.FullPath));
        }

        public static DBFile Decode(PackedFile file) {
            PackedFileDbCodec codec = FromFilename(file.FullPath);
            return codec.Decode(file.Data);
        }
        public static PackedFileDbCodec FromFilename(string filename) {
            return new PackedFileDbCodec(DBFile.typename(filename));
        }

        private PackedFileDbCodec(string type) {
            typeName = type;
            if (!DBTypeMap.Instance.IsSupported(typeName)) {
                throw new DBFileNotSupportedException(string.Format("No DB definition found for {0}", typeName));
            }
            AutoadjustGuid = true;
        }

		#region Read
		/*
		 * Reads a db file from stream, using the version information
		 * contained in the header read from it.
		 */
		public DBFile Decode(Stream stream) {
			BinaryReader reader = new BinaryReader (stream);
			reader.BaseStream.Position = 0;
			DBFileHeader header = readHeader (reader);
            foreach(TypeInfo realInfo in DBTypeMap.Instance.GetVersionedInfos(typeName, header.Version)) {
                try {
#if DEBUG
                    //Console.WriteLine("Parsing with info {0}", string.Join(",", realInfo.Fields));
#endif  
                    DBFile result = ReadFile(reader, header, realInfo);
                    return result;
#if DEBUG
                } catch (Exception e) {
                    Console.Error.WriteLine(e);
                } 
#else
                } catch {}
#endif
            }
            throw new DBFileNotSupportedException(string.Format("No applicable type definition found"));
		}
        public DBFile ReadFile(BinaryReader reader, DBFileHeader header, TypeInfo info) {
#if DEBUG
            if (info.Name.Equals("campaign_map_towns_and_ports_tables")) {
                Console.WriteLine("we're here");
            }
#endif
            reader.BaseStream.Position = header.Length;
            DBFile file = new DBFile (header, info);
            int i = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length) {
//            for (int i = 0; i < header.EntryCount; i++) {
                try {
                    file.Entries.Add (readFields (reader, info));
                    i++;
                } catch (Exception x) {
                    string message = string.Format ("{2} at entry {0}, db version {1}", i, file.Header.Version, x.Message);
                    throw new DBFileNotSupportedException (message, x);
                }
            }
            if (file.Entries.Count != header.EntryCount) {
                throw new DBFileNotSupportedException (string.Format ("Expected {0} entries, got {1}", header.EntryCount, file.Entries.Count));
            }
            // auto-adjust header guid
            if (AutoadjustGuid && !info.ApplicableGuids.Contains(header.GUID)) {
#if DEBUG
                Console.WriteLine("adjusting guid from {0} to {1}", header.GUID, info.ApplicableGuids[0]);
#endif
                header.GUID = info.ApplicableGuids[0];
            }
            return file;
        }
        public DBFile Decode(byte[] data) {
            using (MemoryStream stream = new MemoryStream(data, 0, data.Length)) {
                return Decode(stream);
            }
        }
		#endregion

        public static bool CanDecode(PackedFile packedFile, out string display) {
            bool result = true;
            string key = DBFile.typename(packedFile.FullPath);
            if (DBTypeMap.Instance.IsSupported(key)) {
                try {
                    DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
                    int maxVersion = DBTypeMap.Instance.MaxVersion(key);
                    if (maxVersion != 0 && header.Version > maxVersion) {
                        display = string.Format("{0}: needs {1}, has {2}", key, header.Version, DBTypeMap.Instance.MaxVersion(key));
                        result = false;
                    } else {
                        display = string.Format("Version: {0}", header.Version);
                    }
                } catch (Exception x) {
                    display = string.Format("{0}: {1}", key, x.Message);
                }
            } else {
                display = string.Format("{0}: no definition available", key);
                result = false;
            }
            return result;
        }

        #region Read Header
        public static DBFileHeader readHeader(PackedFile file) {
            using (MemoryStream stream = new MemoryStream(file.Data, (int) 0, (int) file.Size)) {
                return readHeader(stream);
            }
        }
		public static DBFileHeader readHeader(Stream stream) {
			return readHeader (new BinaryReader (stream));
		}
        public static DBFileHeader readHeader(BinaryReader reader) {
			byte index = reader.ReadByte ();
			int version = 0;
			string guid = "";
			bool hasMarker = false;
			uint entryCount = 0;
			
			try {
				if (index != 1) {
					// I don't think those can actually occur more than once per file
					while (index == 0xFC || index == 0xFD) {
						var bytes = new List<byte> (4);
						bytes.Add (index);
						bytes.AddRange (reader.ReadBytes (3));
						UInt32 marker = BitConverter.ToUInt32 (bytes.ToArray (), 0);
						if (marker == GUID_MARKER) {
							guid = IOFunctions.readCAString (reader);
							index = reader.ReadByte ();
						} else if (marker == VERSION_MARKER) {
							hasMarker = true;
							version = reader.ReadInt32 ();
							index = reader.ReadByte ();
							// break;
						} else {
							throw new DBFileNotSupportedException (string.Format ("could not interpret {0}", marker));
						}
					}
				}
				entryCount = reader.ReadUInt32 ();
			} catch {
			}
			DBFileHeader header = new DBFileHeader (guid, version, entryCount, hasMarker);
			return header;
		}
        #endregion

        // creates a list of field values from the given type.
        // stream needs to be positioned at the beginning of the entry.
        private List<FieldInstance> readFields(BinaryReader reader, TypeInfo ttype, bool skipHeader = true) {
			if (!skipHeader) {
				readHeader (reader);
			}
			List<FieldInstance> entry = new List<FieldInstance> ();
			for (int i = 0; i < ttype.Fields.Count; ++i) {
				FieldInfo field = ttype.Fields [i];

                FieldInstance instance = null;
				try {
#if DEBUG
					// Console.WriteLine ("db file codec decoding {1} at {0}", reader.BaseStream.Position, field);
#endif
                    instance = field.CreateInstance();
                    instance.Decode(reader);
					entry.Add (instance);
				} catch (Exception x) {
					throw new InvalidDataException (string.Format 
                        ("Failed to read field {0}/{1}, type {3} ({2})", i, ttype.Fields.Count, x.Message, instance.Info.TypeName));
				}
			}
			return entry;
		}

        #region Write
        public void Encode(Stream stream, DBFile file) {
			BinaryWriter writer = new BinaryWriter (stream);
            file.Header.EntryCount = (uint) file.Entries.Count;
			WriteHeader (writer, file.Header);
			writeFields (writer, file);
			writer.Flush ();
		}
        public byte[] Encode(DBFile file) {
            using (MemoryStream stream = new MemoryStream()) {
                Encode(stream, file);
                return stream.ToArray();
            }
        }

        public static void WriteHeader(BinaryWriter writer, DBFileHeader header) {
			if (header.GUID != "") {
				writer.Write (GUID_MARKER);
				IOFunctions.writeCAString (writer, header.GUID);
			}
			if (header.Version != 0) {
				writer.Write (VERSION_MARKER);
				writer.Write (header.Version);
			}
			writer.Write ((byte)1);
			writer.Write (header.EntryCount);
		}

        public void writeFields(BinaryWriter writer, DBFile file) {
            foreach (List<FieldInstance> entry in file.Entries) {
                writeEntry (writer, entry);
            }
        }

        private void writeEntry(BinaryWriter writer, List<FieldInstance> fields) {
            for (int i = 0; i < fields.Count; i++) {
                try {
                    FieldInstance field = fields[i];
                    field.Encode(writer);
                } catch (Exception x) {
                    Console.WriteLine(x);
                    throw x;
                }
            }
		}
        #endregion
    }
	
}
