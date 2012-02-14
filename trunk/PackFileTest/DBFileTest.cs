using System;
using System.IO;
using System.Collections.Generic;
using Common;

namespace PackFileTest {
	public class DBFileTest : IComparable {
		public string packfileName;
		string Packfile { 
			get { return packfileName; }
			set { packfileName = value; }
		}
		
		public string generalError = "";
		public SortedSet<Tuple<string, int>> supported = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
		public SortedSet<Tuple<string, int>> noDefinition = new SortedSet<Tuple<string,int>>(VERSION_COMPARE);
		public SortedSet<Tuple<string, int>> noDefForVersion = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
		public SortedSet<Tuple<string, int>> invalidDefForVersion = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
		public SortedSet<Tuple<string, int, int>> downgradedVersions = new SortedSet<Tuple<string, int, int>>(DOWNGRADED_COMPARE);
		public SortedSet<Tuple<string,int>> emptyTables = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);

		public static SortedSet<DBFileTest> testAllPacks(string dir) {
			SortedSet<DBFileTest> tests = new SortedSet<DBFileTest> ();
			foreach (string file in Directory.EnumerateFiles (dir, "*.pack")) {
				DBFileTest test = new DBFileTest (file);
				test.testAllFiles ();
				tests.Add (test);
			}
			return tests;
		}

		public DBFileTest (string file) {
			packfileName = file;
		}
		
		public void testAllFiles() {
			string currentFile = "";
			try {
				PackFile packFile = new PackFileCodec().Open (packfileName);
				foreach (PackedFile packed in packFile.Files) {
					try {
						currentFile = packed.FullPath;
						if (currentFile.StartsWith ("db")) {
							testDbFile (packed);
						}
					} catch (Exception x) {
                        throw x;
					}
				}
			} catch (Exception x) {
				generalError = string.Format ("reading {0}: {1}", currentFile, x.Message);
			}
		}
		
		public void testDbFile(PackedFile file) {
			string type = DBFile.typename (file.FullPath);
			DBFileHeader header = PackedFileDbCodec.readHeader (file);
			Tuple<string,int> tuple = new Tuple<string, int> (type, header.Version);
			if (header.EntryCount == 0) {
				emptyTables.Add (tuple);
			} else if (DBTypeMap.Instance.IsSupported (type)) {
				try {
					TypeInfo info = DBTypeMap.Instance [type, header.Version];
					if (info != null) {
						if (testFile (file, info)) {
							supported.Add (tuple);
						} else if (!invalidDefForVersion.Contains (tuple)) {
							invalidDefForVersion.Add (tuple);
						}
					} else {
						TryDowngrade (file, type, header, tuple, ref info);
					}
				} catch {
					invalidDefForVersion.Add (tuple);
				}
			} else {
				noDefinition.Add (tuple);
			}
		}

		void TryDowngrade(PackedFile file, string type, DBFileHeader header, Tuple<string, int> tuple, ref TypeInfo info) {
			// downgrade...
			int downgradedVersion = -1;
			info = DBTypeMap.Instance [type, header.Version];
			for (int i = header.Version; i >= 0; i--) {
				info = DBTypeMap.Instance [type, i];
				if (info != null) {
					downgradedVersion = i;
					break;
				}
			}
			if (downgradedVersion != -1) {
				if (testFile (file, info)) {
					downgradedVersions.Add (new Tuple<string, int, int> (type, header.Version, downgradedVersion));
				} else {
					noDefForVersion.Add (tuple);
				}
			} else {
				noDefForVersion.Add (tuple);
			}
		}
		
		public int CompareTo(object o) {
			int result = 0;
			if (o is DBFileTest) {
				result = packfileName.CompareTo ((o as DBFileTest).packfileName);
			}
			return result;
		}

		bool testFile(PackedFile file, TypeInfo type) {
			bool result = true;
			try {
				DBFile dbFile = new PackedFileDbCodec().readDbFile (file);
				result = (dbFile.Entries.Count == dbFile.header.EntryCount);
			} catch {
				result = false;
			}
			return result;
		}

		class TableVersionComparer : IComparer<Tuple<string, int>> {
			public int Compare(Tuple<string, int> a, Tuple<string, int> b) {
				int result = a.Item1.CompareTo (b.Item1);
				if (result == 0) {
					result = a.Item2.CompareTo (a.Item2);
				}
				return result;
			}
		}

		class TableVersionDowngradedComparer : IComparer<Tuple<string, int, int>> {
			public int Compare(Tuple<string, int, int> a, Tuple<string, int, int> b) {
				int result = a.Item1.CompareTo (b.Item1);
				if (result == 0) {
					result = a.Item2.CompareTo (a.Item2);
				}
				return result;
			}
		}
		static IComparer<Tuple<string, int>> VERSION_COMPARE = new TableVersionComparer ();
		static IComparer<Tuple<string, int, int>> DOWNGRADED_COMPARE = new TableVersionDowngradedComparer ();
	}
}

