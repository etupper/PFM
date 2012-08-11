using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
		public int Length {
			get {
				int result = 5;
				result += (GUID.Length != 0) ? 78 : 0;
				result += HasVersionMarker ? 8 : 0;
				return result;
			}
		}

        public DBFileHeader(string guid, int version, uint entryCount, bool marker) {
            GUID = guid;
            Version = version;
            EntryCount = entryCount;
            HasVersionMarker = marker;
        }

		#region Framework Overrides
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
		#endregion
            }

	/*
	 * Class representing a database file.
	 */
    public class DBFile {
        private List<List<FieldInstance>> entries = new List<List<FieldInstance>>();
        public DBFileHeader Header;
        public TypeInfo CurrentType {
            get;
            set;
        }

		#region Attributes
		// the entries of this file
        public List<List<FieldInstance>> Entries {
            get {
                return this.entries;
            }
        }

        // access by row/column
		public FieldInstance this [int row, int column] {
			get {
				return entries [row][column];
			}
		}
		#endregion

        #region Constructors
        public DBFile (DBFileHeader h, TypeInfo info) {
			Header = h;
			CurrentType = info;
		}

        public DBFile (DBFile toCopy) : this(toCopy.Header, toCopy.CurrentType) {
			Header = new DBFileHeader (toCopy.Header.GUID, toCopy.Header.Version, toCopy.Header.EntryCount, toCopy.Header.HasVersionMarker);
			toCopy.entries.ForEach (entry => entries.Add (new List<FieldInstance> (entry)));
		}
        #endregion

        public List<FieldInstance> GetNewEntry() {
			List<FieldInstance> newEntry = new List<FieldInstance> ();
			foreach (FieldInfo field in CurrentType.fields) {
				newEntry.Add (new FieldInstance (field, field.DefaultValue));
			}
			return newEntry;
		}

        public void Import(DBFile file) {
			if (CurrentType.name != file.CurrentType.name) {
				throw new DBFileNotSupportedException 
					("File type of imported DB doesn't match that of the currently opened one", this);
			}
			// check field type compatibility
			for (int i = 0; i < file.CurrentType.fields.Count; i++) {
				if (file.CurrentType.fields [i].TypeCode != CurrentType.fields [i].TypeCode) {
					throw new DBFileNotSupportedException 
						("Data structure of imported DB doesn't match that of currently opened one at field " + i, this);
				}
			}
			DBFileHeader h = file.Header;
			Header = new DBFileHeader (h.GUID, h.Version, h.EntryCount, h.HasVersionMarker);
			CurrentType = file.CurrentType;
			// this.entries = new List<List<FieldInstance>> ();
			entries.AddRange (file.entries);
            Header.EntryCount = (uint) entries.Count;
		}

		public static string typename(string fullPath) {
			return Path.GetFileName(Path.GetDirectoryName (fullPath));
		}
	}
}
