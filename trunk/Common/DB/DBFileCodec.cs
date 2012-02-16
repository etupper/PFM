using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Common {
	/*
	 * Implemented by classes that can read and write db files.
	 */
	public interface DbCodec {
		DBFile readDbFile(Stream stream);
		void writeDbFile(Stream stream, DBFile dbFile);
	}
	
	/*
	 * A class parsing dbfiles from and to data streams in packed file format.
	 */
    public class PackedFileDbCodec : Codec<DBFile> {
		public static readonly PackedFileDbCodec Instance = new PackedFileDbCodec();
		
		public delegate void EntryLoaded(FieldInfo info, string value);
		public delegate void HeaderLoaded(DBFileHeader header);
		public delegate void LoadingPackedFile(PackedFile packed);

		#region Internal
		// header markers
		static UInt32 GUID_MARKER = BitConverter.ToUInt32 (new byte[] { 0xFD, 0xFE, 0xFC, 0xFF}, 0);
		static UInt32 VERSION_MARKER = BitConverter.ToUInt32 (new byte[] { 0xFC, 0xFD, 0xFE, 0xFF}, 0);
		#endregion

        public static byte[] Encode(DBFile file) {
            using (MemoryStream stream = new MemoryStream()) {
                PackedFileDbCodec.Instance.Encode(stream, file);
                return stream.ToArray();
            }
        }

		#region Read
		public DBFile readDbFile(PackedFile file) {
			return Decode(file);
		}
        public DBFile Decode(PackedFile file) {
            return readDbFile(file.FullPath, file.Data);
        }

		public DBFile readDbFile(string typeName, byte[] data) {
			return readDbFile (typeName, new MemoryStream (data, 0, data.Length));
		}
		/*
		 * Reads a db file from stream, using the version information
		 * contained in the header read from it.
		 */
		public DBFile readDbFile(string filename, Stream stream) {
			BinaryReader reader = new BinaryReader (stream);
			reader.BaseStream.Position = 0;
			DBFileHeader header = readHeader (reader);
            string type = DBFile.typename(filename);
            if (!DBTypeMap.Instance.IsSupported(type)) {
                throw new DBFileNotSupportedException(string.Format("No DB definition found for {0}", type));
            }
            TypeInfo realInfo = DBTypeMap.Instance[type, header.Version];
			DBFile file = new DBFile (header, realInfo);
			reader.BaseStream.Position = header.Length;

			for (int i = 0; i < header.EntryCount; i++) {
				try {
					file.Entries.Add (readFields (reader, realInfo));
				} catch (Exception x) {
					string message = string.Format ("{2} at entry {0}, db version {1}", i, file.Header.Version, x.Message);
					throw new DBFileNotSupportedException (message, x);
				}
			}
			if (file.Entries.Count != header.EntryCount) {
				throw new DBFileNotSupportedException (string.Format ("Expected {0} entries, got {1}", header.EntryCount, file.Entries.Count));
			}
			return file;
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
			for (int i = 0; i < ttype.fields.Count; ++i) {
				FieldInfo field = ttype.fields [i];

				try {
					//Console.WriteLine ("decoding at {0}", reader.BaseStream.Position);
					string value = field.Decode (reader);
					entry.Add (new FieldInstance (field, value));
				} catch (Exception x) {
					throw new InvalidDataException (string.Format ("Failed to read field {0}/{1} ({2})", i, ttype.fields.Count, x.Message));
				}
			}
			return entry;
		}

        #region Write
        public void writeDbFile(Stream stream, DBFile file) {
			BinaryWriter writer = new BinaryWriter (stream);
            file.Header.EntryCount = (uint) file.Entries.Count;
			writeHeader (writer, file.Header);
			writeFields (writer, file);
			writer.Flush ();
		}

        public void Encode(Stream stream, DBFile file) {
            writeDbFile(stream, file);
        }

        public void writeHeader(BinaryWriter writer, DBFileHeader header) {
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
				FieldInstance field = fields [i];
				field.Info.Encode (writer, field.Value);
                } catch (Exception x) {
                    Console.WriteLine(x);
                    throw x;
			}
		}
		}
        #endregion
    }
	
	/*
	 */
    public class TextDbCodec : Codec<DBFile> {
        static char[] QUOTES = { '"' };
		static char[] TABS = { '\t' };
		static string format = "\"{0}\"";

        public static readonly Codec<DBFile> Instance = new TextDbCodec();

        public static byte[] Encode(DBFile file) {
            using (MemoryStream stream = new MemoryStream()) {
                TextDbCodec.Instance.Encode(stream, file);
                return stream.ToArray();
            }
        }

        public DBFile readDbFile(Stream stream) {
            return Decode (new StreamReader (stream));
        }

        public DBFile Decode(PackedFile file) {
            using (Stream stream = new MemoryStream(file.Data)) {
                return readDbFile(stream);
            }
        }

		// read from given stream
        public DBFile Decode(StreamReader reader) {
            // another tool might have saved tabs and quotes around this 
            // (at least open office does)
            string typeInfoName = reader.ReadLine().Replace("\t", "").Trim(QUOTES);
            string versionStr = reader.ReadLine().Replace("\t", "").Trim(QUOTES);
			int version;
			switch (versionStr) {
			case "1.0":
				version = 0;
				break;
			case "1.2":
				version = 1;
				break;
			default:
				version = int.Parse (versionStr);
				break;
			}
			TypeInfo info = DBTypeMap.Instance [typeInfoName, version];
			// ignore header line (type names)
			reader.ReadLine ();
			List<List<FieldInstance>> entries = new List<List<FieldInstance>> ();
			string str2;
			while (!reader.EndOfStream) {
				str2 = reader.ReadLine ();
				string[] strArray;
				try {
					strArray = str2.Split (TABS, StringSplitOptions.None);
					List<FieldInstance> item = new List<FieldInstance> ();
					for (int i = 0; i < strArray.Length; i++) {
						FieldInfo fieldInfo = info.fields [i];
						string str3 = Unformat (strArray [i]);
						item.Add (new FieldInstance (fieldInfo, str3));
					}
					entries.Add (item);
				} catch (Exception x) {
					// Console.WriteLine (x);
				}
			}
			DBFileHeader header = new DBFileHeader ("", version, (uint)entries.Count, version != 0);
			DBFile file = new DBFile (header, info);
			file.Entries.AddRange (entries);
			return file;
		}
		private string Format(string input) {
			return string.Format (format, Regex.Escape (input));
		}
		private string Unformat(string formatted) {
			string result = Regex.Unescape (formatted);
            if (result.StartsWith("\"")) {
                // remove one leading and trailing quote if present
                result = result.Substring(1, result.Length - 2);
            }
            return result;
		}

		// write the given file to stream
        public void Encode(Stream stream, DBFile file) {
            StreamWriter writer = new StreamWriter (stream);
            writer.WriteLine (file.CurrentType.name);
			writer.WriteLine (Convert.ToString (file.Header.Version));
            foreach (FieldInfo info2 in file.CurrentType.fields) {
                writer.Write (info2.Name + "\t");
            }
            writer.WriteLine ();
            foreach (List<FieldInstance> list in file.Entries) {
				string str = Format(list [0].Value);
				for (int i = 1; i < list.Count; i++) {
					string current = list [i].Value;
					str += "\t" + Format (current);
                }
				writer.WriteLine (str);
            }
			writer.Flush ();
        }
    }
}
