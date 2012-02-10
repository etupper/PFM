using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Common.Properties;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics;

namespace Common {
	/*
	 * Class representing the first bytes in a db file;
	 * these contain information about db version, maybe an id,
	 * and a count of contained entries.
	 */
    public class DBFileHeader {
        public string GUID { get; set; }
        public bool HasVersionMarker { get; set; }

        public int Version { get; set; }
        public uint EntryCount { get; set; }

        public DBFileHeader(string guid, int version, uint entryCount, bool marker) {
            GUID = guid;
            Version = version;
            EntryCount = entryCount;
            HasVersionMarker = marker;
        }

        public override bool Equals(object other) {
            bool result = false;
            if (other is DBFileHeader) {
                DBFileHeader header2 = (DBFileHeader)other;
                result = GUID.Equals(header2.GUID);
                result &= Version.Equals(header2.Version);
                result &= EntryCount.Equals(header2.EntryCount);
            }
            return result;
        }
        public override int GetHashCode() {
            return GUID.GetHashCode();
        }
        public int Length {
            get {
                int result = 5;
                result += (GUID.Length != 0) ? 78 : 0;
                result += HasVersionMarker ? 8 : 0;
                return result;
            }
        }
    }

	/*
	 * Class representing a database file.
	 */
    public class DBFile {

        private List<List<FieldInstance>> entries = new List<List<FieldInstance>>();
        public DBFileHeader header;
		private TypeInfo typeInfo;

		#region Attributes
        public int TotalwarHeaderVersion {
            get { return header.Version; }
                }
		public TypeInfo CurrentType {
			get {
				return typeInfo;
                }
            }

		public List<List<FieldInstance>> Entries {
			get {
				return this.entries;
        }
                        }
		#endregion

        #region Constructors
        public DBFile (DBFileHeader h, TypeInfo info) {
			header = h;
			typeInfo = info;
                        }

        public DBFile (DBFile toCopy) : this(toCopy.header, toCopy.typeInfo) {
            header = new DBFileHeader (toCopy.header.GUID, toCopy.header.Version, toCopy.header.EntryCount, toCopy.header.HasVersionMarker);
            toCopy.entries.ForEach (entry => entries.Add (new List<FieldInstance> (entry)));
            }
        #endregion

        public byte[] GetBytes() {
            byte[] buffer;
			MemoryStream stream = new MemoryStream ();
			new PackedFileDbCodec(this.CurrentType).writeDbFile (stream, this);
                    buffer = stream.ToArray();
			stream.Dispose ();
            return buffer;
        }

        public List<FieldInstance> GetNewEntry() {
            List<FieldInstance> newEntry = new List<FieldInstance> ();
            foreach (FieldInfo field in typeInfo.fields) {
                newEntry.Add (new FieldInstance(field, field.DefaultValue));
                        }
            return newEntry;
                        }

        public void Import(DBFile file) {
			if (typeInfo.name != file.typeInfo.name) {
				throw new DBFileNotSupportedException ("File type of imported DB doesn't match that of the currently opened one", this);
			}
			DBFileHeader h = file.header;
			header = new DBFileHeader (h.GUID, h.Version, h.EntryCount, h.HasVersionMarker);
			typeInfo = file.typeInfo;
			this.entries = new List<List<FieldInstance>> ();
			entries.AddRange (file.entries);
		}

		public static string filename(string fullPath) {
            return fullPath.Substring (fullPath.LastIndexOf ('\\')+1).Replace ("_tables", "");
        }
        public static string typename(string fullPath) {
			return fullPath.Split ('\\') [1].Split ('/') [0].Replace ("_tables", "");
            }
        }
                }

