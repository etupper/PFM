using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace PackFileTest {
	class PackTest {
		public static void Main(string[] args) {
			DBTypeMap.Instance.initializeTypeMap (Directory.GetCurrentDirectory());
			//DBTypeMap.Instance.fromXmlSchema (Directory.GetCurrentDirectory ());
			 
			testAll (args);
			//testSingle();
			//testMultiple();
		}
		static void testAll(string[] args) {
			foreach (string dir in args) {
				ICollection<DBFileTest> tests = DBFileTest.testAllPacks (dir);
				Console.WriteLine ("Dir: {0}\nTests Run:{1}", dir, tests.Count);
				foreach (DBFileTest test in tests) {
					Console.WriteLine (test.packfileName);
					if (test.generalError != "") {
						Console.WriteLine ("General error: {0}", test.generalError);
					} else {
						Console.WriteLine ("Supported Files: {0}", test.supported.Count);
						Console.WriteLine ("Empty Files: {0}", test.emptyTables.Count);
						printList ("No description", test.noDefinition);
						printList ("no definition for version", test.noDefForVersion);
						printList ("invalid description", test.invalidDefForVersion);
						printList ("downgraded versions", test.downgradedVersions);
						
						extractFiles (dir, new PackFile (test.packfileName), test.noDefForVersion);
						extractFiles (dir, new PackFile (test.packfileName), test.invalidDefForVersion);
						extractFiles (dir, new PackFile (test.packfileName), test.noDefinition);
					}
					Console.WriteLine ();
//				TestRunner runner = new TestRunner (dir);
//				runner.testAllPacks ();
				}
            }
            Console.ReadKey();
        }
		
		static void extractFiles(string dir, PackFile pack, ICollection<Tuple<string, int>> toExtract) {
			if (toExtract.Count != 0) {
				string path = Path.Combine (dir, "failed");
				Directory.CreateDirectory (path);
				foreach (Tuple<string, int> failed in toExtract) {
					string failType = failed.Item1;
					string failPath = string.Format ("db\\{0}_tables\\{0}", failType);
					PackedFile found = null;
					foreach (PackedFile packed in pack.FileList) {
						if (packed.Filepath.Equals (failPath)) {
							found = packed;
							break;
						}
					}
					if (found != null) {
						string filePath = Path.Combine (path, string.Format ("{0}_{1}", failType, failed.Item2));
						// Console.WriteLine (filePath);
						File.WriteAllBytes (Path.Combine (dir, filePath), found.Data);
					} else {
						Console.WriteLine ("cant extract {0}", failPath);
					}
				}
			}
		}
		
		static void printList(string label, ICollection<Tuple<string, int>> list) {
			if (list.Count != 0) {
				Console.WriteLine ("{0}: {1}", label, list.Count);
				foreach (Tuple<string, int> tableVersion in list) {
					Console.WriteLine ("Type {0}, Version {1}", tableVersion.Item1, tableVersion.Item2);
				}
			}
		}
		static void printList(string label, ICollection<Tuple<string, int, int>> list) {
			if (list.Count != 0) {
				Console.WriteLine ("{0}: {1}", label, list.Count);
				foreach (Tuple<string, int, int> tableVersion in list) {
					Console.WriteLine ("Type {0}, Version {1} downgraded {2}", tableVersion.Item1, tableVersion.Item2, tableVersion.Item3);
				}
			}
		}

		static void testMultiple() {
			testSingle ("/opt/mono/packs/etw/etw-patch2.pack", true);
//			testSingle ("/opt/mono/packs/ntw/ntw-patch2.pack");
//			testSingle ("/opt/mono/packs/ntw/ntw-patch4.pack");
			testSingle ("/opt/mono/packs/stw/s2-patch.pack", true);
		}
		static void testSingle() {
			testSingle ("/opt/mono/packs/stw/s2-patch.pack", true);
		}

		static void testSingle(string path, bool verbose = false) {
			TestRunner runner = new TestRunner (Directory.GetCurrentDirectory (), verbose);
			runner.testPackFile (path);
			Console.WriteLine ("unknown files: {0}", runner.unknownFiles.Count);
			foreach (string f in runner.unknownFiles) {
				Console.WriteLine (f);
			}
		}
	}

	class TestRunner {
		string directory;
		List<TestFailure> failures = new List<TestFailure> ();
		public SortedSet<string> unknownFiles = new SortedSet<string> ();
		int testedFiles = 0;
		
		bool Verbose {
			get;
			set;
		}
			

		public TestRunner (string dir, bool verbose = true) {
			directory = dir;
			Verbose = verbose;
		}
		
		public void testAllPacks() {
			foreach (string file in Directory.EnumerateFiles (directory, "*.pack")) {
				testedFiles = 0;
				failures.Clear ();
				// unknownFiles.Clear ();
				testPackFile (file);
			}
			Console.WriteLine ("unknown files: {0}", unknownFiles.Count);
			foreach (string f in unknownFiles) {
				Console.WriteLine (f);
			}
		}
		DBFile fromPacked(PackedFile packedFile) {
			PackedFileDbCodec dbCodec = new PackedFileDbCodec (packedFile);
			DBFile result = null;
			if (DBTypeMap.Instance.IsSupported(DBFile.typename(packedFile.Filepath))) {
				result = dbCodec.readDbFile (packedFile);
			} else {
				Stream readFrom = new MemoryStream (packedFile.Data, 0, (int)packedFile.Size);
				DBFileHeader header = PackedFileDbCodec.readHeader (readFrom);
				if (header.EntryCount == 0) {
					result = new DBFile (header, null);
				}
			}
			return result;
		}
		public void testPackFile(string file) {
			try {
				Console.WriteLine ("testing pack file {0}", file);
				PackFile packFile = new PackFile (file);
				PFHeader pfh = packFile.Header;
				string log = string.Format ("type: {0}/{1}, supersedes {2}, contains {3} files", 
				pfh.Type, pfh.Version, pfh.ReplacedPackFileName, pfh.FileCount);
				Console.WriteLine (log);
				Console.Out.Flush ();
				string testDir = "test";
				if (!Directory.Exists (testDir)) {
					Directory.CreateDirectory (testDir);
				}

				foreach (PackedFile packedFile in packFile.FileList) {
					if (packedFile.Filepath.StartsWith ("db")) {
						string version = "<unknown>";
						try {
							if (packedFile.Size != 0) {
								DBFile dbFile = fromPacked (packedFile);
								if (dbFile != null && dbFile.header.EntryCount != 0) {
									testedFiles++;
									if (dbFile.header.EntryCount != dbFile.Entries.Count) {
										failures.Add (new TestFailure 
											(string.Format ("invalid count for {0}: was {1}, expected {2}", 
											packedFile.Filepath, dbFile.Entries.Count, dbFile.header.EntryCount)));
										continue;
									}
									byte[] bytes = dbFile.GetBytes ();
									int size = bytes.Length;
									if (size < packedFile.Size) {
										failures.Add (new TestFailure (string.Format 
											("{2} @ {3:x}: \nexpected {0} bytes (up to {4:x}), got {1} (to {5:x})", 
											packedFile.Size, size, packedFile.Filepath, packedFile.offset, 
											(packedFile.offset + packedFile.Size), (packedFile.offset + (ulong)size))));
										if (Verbose) {
											dumpPackedFile (file, packedFile);
										}
									} else {
										packedFile.ReplaceData (dbFile.GetBytes ());
									}
								} else if (dbFile == null) {
									unknownFiles.Add (string.Format ("{0}", typename (packedFile), packedFile.offset, packFile.Filepath));
								}
							}
						} catch (Exception x) {
							TestFailure failure = new TestFailure {
							packedFile = packedFile,
							Description = string.Format ("{0} when trying to read \n{1} \nversion {2} starting at {3:x6} (size {4})", 
									x.Message, packedFile.Filepath, version, packedFile.offset, packFile.Size)
							};
							failures.Add (failure);
							
							if (Verbose) {
								dumpAndRead (file, packedFile);
							}
							// Console.WriteLine (failure.Description);
						}
					}
				}
				if (failures.Count == 0) {
					testSave (packFile);
				}
				// Console.WriteLine ("unknown files: {0}: {1}", unknownFiles.Count, string.Join (",", unknownFiles));
				Console.WriteLine ("passed {0}/{1}", testedFiles - failures.Count, testedFiles);
				foreach (TestFailure failure in failures) {
					Console.WriteLine (failure.Description);
					Console.WriteLine ();
				}
			} catch (Exception) {
			}
			Console.Out.Flush ();
		}
		
		private static void dumpAndRead(string file, PackedFile packedFile) {
			string dumpFile = dumpPackedFile (file, packedFile);
			readPackedFromFile (packedFile, dumpFile);
			Console.Out.Flush ();
		}
		
		private static void readPackedFromFile(PackedFile packedFile, string dumpFile) {
			try {
				Console.WriteLine ("re-reading {0}", packedFile.Filepath);
				FileStream stream = File.OpenRead (dumpFile);
				PackedFileDbCodec codec = new PackedFileDbCodec (packedFile);
				DBFileHeader header = PackedFileDbCodec.readHeader (stream);
				Console.WriteLine ("file: {0}, version {1}", packedFile.Filepath, header.Version);
//				TypeInfo info = DBTypeMap.Instance [typename (packedFile)] [header.Version];
//				BinaryReader reader = new BinaryReader (stream);
				codec.readDbFile (packedFile);
				Console.WriteLine ();
				//codec.readDbFile (stream, DBTypeMap.Instance [typename (packedFile)]);
				stream.Close ();
			} catch (Exception) {
				Console.WriteLine ("re-read failed");
			}
		}
		
		private static string dumpPackedFile(string file, PackedFile packedFile) {
			// dump raw file (directly from packed file with given size) into file
			byte[] bytes = new byte[packedFile.Size + 1];
			Array.Copy (packedFile.Data, bytes, packedFile.Size);
			string dumpedName = Path.Combine (Path.GetDirectoryName (file), Path.GetFileName (packedFile.Filepath) + "_raw");
			File.WriteAllBytes (dumpedName, bytes);
			return dumpedName;
		}
		
		private static void dumpDbFile(PackedFile packed, DBFile file) {
			Console.WriteLine ("dumping file {0} @ {1:x}", packed.Filepath, packed.offset);
			for (int j = 0; j < file.Entries.Count; j++) {
				List<FieldInstance> entry = file.Entries [j];
				Console.WriteLine ("entry {0}:", j);
				for (int i = 0; i < entry.Count; i++) {
					Console.WriteLine ("field {0}: {1}", i, entry[i].Value);
				}
				Console.WriteLine ();
			}
			Console.Out.Flush ();
		}

		private void testSave(PackFile packFile) {
			packFile.SaveAs ("verification.pack");
			packFile = new PackFile (packFile.Filepath);
			PackFile savedPack = new PackFile ("verification.pack");
			for (int i = 0; i < packFile.FileList.Count; i++) {
				PackedFile packed = packFile.FileList [i];
				PackedFile other = savedPack.FileList [i];
				if (!packed.Filepath.Equals (other.Filepath)) {
					List<string> list = new List<string> ();
					foreach (PackedFile f in packFile.FileList) {
						list.Add (f.Filepath);
					}
					string list1 = string.Join (",", list);
					list.Clear ();
					foreach (PackedFile f in savedPack.FileList) {
						list.Add (f.Filepath);
					}
					string list2 = string.Join (",", list);
					failures.Add (new TestFailure 
						(string.Format ("different packed files found: {0} and {1}", packed.Filepath, other.Filepath)));
					Console.WriteLine (list1);
					Console.WriteLine (list2);
					break;
				}
				if (packed.Filepath.StartsWith ("db") && packed.Size != 0) {
					try {
						DBFile original = fromPacked (packed);
						if (original != null) {
							DBFile saved = fromPacked (other);
							if (!equals (original, saved)) {
								failures.Add (new TestFailure (string.Format ("{0}: save result differs", packed.Filepath)));
							}
						}
					} catch (Exception x) {
						failures.Add (new TestFailure (string.Format
							("{0}\nPackfile {1}\npacked file {2}, offset {3}, size {4}", x.Message, packFile.Filepath, packed.Filepath, packed.offset, packed.Size)));
//						Console.WriteLine (x);
//						Console.WriteLine ("Packfile {0}", packFile);
//						Console.WriteLine ("packed file {0}, offset {1}, size {2}", packed.Filepath, packed.offset, packed.Size);
//						throw x;
					}
				}
			}
		}
		
		private static void dumpReadEntry(FieldInfo info, string val) {
			Console.WriteLine ("read {0} field: value {1}", info.TypeName, val);
		}
		
		private static string filename(PackedFile file) {
			string dbFile = file.Filepath;
			string result = dbFile.Substring (dbFile.LastIndexOf ('\\') + 1).Replace ("_tables", "");
			return result;
		}
		
		private static string typename(PackedFile file) {
			// string dbFile = file.Filepath;
			string result = file.Filepath.Split ('\\')[1].Replace("_tables", "");
			// string result = dbFile.Substring (dbFile.LastIndexOf ('\\') + 1).Replace ("_tables", "");
			return result;
		}
		
		public static bool equals(DBFile file1, DBFile file2) {
			bool result = file1.header.Equals (file2.header);
			try {
				if (result) {
					List<List<FieldInstance>> entries1 = file1.Entries;
					List<List<FieldInstance>> entries2 = file2.Entries;
					for (int j = 0; j < entries1.Count; j++) {
						List<FieldInstance> fields1 = entries1 [j];
						List<FieldInstance> fields2 = entries2 [j];
						for (int i = 0; i < fields1.Count; i++) {
							result &= fields1 [i].Equals (fields2 [i]);
							if (!result) {
								break;
							}
						}
					}
				}
			} catch (Exception x) {
				Console.Error.WriteLine (x);
				result = false;
			}
			return result;
		}
	}
	
	class TestFailure {
		// public PackFile packFile;
		public PackedFile packedFile;
		public string Description { get; set; }
		public TestFailure () {}

		public TestFailure (string description) {
			Description = description;
		}
	}
}
