using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace PackFileTest {
    class PackTest {
#pragma warning disable 414
        // just to keep track of what's available in testAll:
		// -db: test db files; -t: tsv test for db files
		// -uv: unit variant files
        private static string[] OPTIONS = { "-t", "-db", "uv" };
#pragma warning restore 414

		bool testDbFiles = false;
		bool testTsvExport = false;
		bool testUnitVariants = false;

		private static string OPTIONS_FILENAME = "testoptions.txt";

        public static void Main(string[] args) {
            new PackTest().testAll(args);
        }

        void testAll(string[] args) {
			// List<string> arguments = new List<string> ();
			// run tests for all packs in all dirs given in the options file
			if (File.Exists (OPTIONS_FILENAME)) {
				foreach (string dir in File.ReadAllLines(OPTIONS_FILENAME)) {
					if (dir.StartsWith ("#")) {
						continue;
					}
					if (dir.Equals ("-t")) {
						Console.WriteLine ("TSV export/import enabled");
						testTsvExport = true;
					} else if (dir.Equals ("-db")) {
						Console.WriteLine ("Database Test enabled");
						testDbFiles = true;
						DBTypeMap.Instance.initializeTypeMap (Directory.GetCurrentDirectory ());
					} else if (dir.Equals ("-uv")) {
						Console.WriteLine ("Unit Variant Test enabled");
						testUnitVariants = true;
					} else {
						ICollection<PackedFileTest> tests = testAllPacks (dir, testTsvExport);
						Console.WriteLine ("Dir: {0}\nTests Run:{1}", dir, tests.Count);
						foreach (PackedFileTest test in tests) {
							Console.WriteLine (test.Packfile);
							// output results
							test.printResults ();
						}
					}
				}
				Console.WriteLine ("Test run finished, press any key");
				Console.ReadKey ();
			} else {
				Console.Error.Write ("Missing options file {0}", OPTIONS_FILENAME);
			}
		}

        // run db tests for all files in the given directory
        public SortedSet<PackedFileTest> testAllPacks(string dir, bool testTsv) {
			SortedSet<PackedFileTest> tests = new SortedSet<PackedFileTest> ();
			foreach (string file in Directory.EnumerateFiles(dir, "*.pack")) {
				if (testDbFiles) {
					DBFileTest test = new DBFileTest (file, testTsv);
					test.testAllFiles ();
					tests.Add (test);
				}
				if (testUnitVariants) {
					UnitVariantTest test = new UnitVariantTest (file);
					test.testAllFiles ();
					tests.Add (test);
				}
			}
			return tests;
		}

    }
	
	public abstract class PackedFileTest : IComparable {
		public string Packfile { get; set; }
		public SortedSet<string> generalErrors = new SortedSet<string> ();
		public SortedSet<string> allTestedFiles = new SortedSet<string>();
		
		public abstract bool canTest(PackedFile file);
		
		public abstract void testFile(PackedFile file);

		public abstract void printResults();

		// tests all files in this test's pack
		public void testAllFiles() {
			PackFile packFile = new PackFileCodec ().Open (Packfile);
			foreach (PackedFile packed in packFile.Files) {
				try {
					if (canTest (packed)) {
						allTestedFiles.Add (packed.FullPath);
						testFile (packed);
					}
				} catch (Exception x) {
					generalErrors.Add (string.Format ("reading {0}: {1}", packed.FullPath, x.Message));
				}
			}
		}
		
		public int CompareTo(object o) {
			int result = 0;
			if (o is PackedFileTest) {
				result = Packfile.CompareTo ((o as PackedFileTest).Packfile);
			}
			return result;
		}
		
		public static void printList(string label, ICollection<string> list) {
			if (list.Count != 0) {
				Console.WriteLine ("{0}: {1}", label, list.Count);
				foreach (string toPrint in list) {
					Console.WriteLine (toPrint);
				}
			}
		}
	}
}
