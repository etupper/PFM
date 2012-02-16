using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace PackFileTest {
    class PackTest {
#pragma warning disable 414
        // just to keep track of what's available in testAll
        private static string[] OPTIONS = { "-t" };
#pragma warning restore 414
		
		private static string OPTIONS_FILENAME = "testoptions.txt";

        public static void Main(string[] args) {
            testAll(args);
        }

        static void testAll(string[] args) {
			List<string> arguments = new List<string> ();
			// run tests for all packs in all dirs given in the options file
			if (File.Exists (OPTIONS_FILENAME)) {
				bool testTsvExport = false;
				foreach (string dir in File.ReadAllLines(OPTIONS_FILENAME)) {
					if (dir.Equals ("-t")) {
						Console.WriteLine ("TSV export/import enabled");
						testTsvExport = true;
					} else {
						ICollection<DBFileTest> tests = testAllPacks (dir, testTsvExport);
						Console.WriteLine ("Dir: {0}\nTests Run:{1}", dir, tests.Count);
						foreach (DBFileTest test in tests) {
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
        public static SortedSet<DBFileTest> testAllPacks(string dir, bool testTsv) {
            DBTypeMap.Instance.initializeTypeMap(Directory.GetCurrentDirectory());
            SortedSet<DBFileTest> tests = new SortedSet<DBFileTest>();
            foreach (string file in Directory.EnumerateFiles(dir, "*.pack")) {
                DBFileTest test = new DBFileTest(file, testTsv);
                test.testAllFiles();
                // tests.Add (test);

                Console.WriteLine(test.Packfile);
                Console.WriteLine("Supported Files: {0}", test.supported.Count);
                Console.WriteLine("Empty Files: {0}", test.emptyTables.Count);
                printList("General errors", test.generalErrors);
                printList("No description", test.noDefinition);
                printList("no definition for version", test.noDefForVersion);
                printList("invalid description", test.invalidDefForVersion);
                printList("Tsv exports", test.tsvFails);
                Console.WriteLine();
            }
            return tests;
        }

        // write files that failed to filesystem individually for later inspection
        static void extractFiles(string dir, PackFile pack, ICollection<Tuple<string, int>> toExtract) {
            if (toExtract.Count != 0) {
                string path = Path.Combine(dir, "failed");
                Directory.CreateDirectory(path);
                foreach (Tuple<string, int> failed in toExtract) {
                    string failType = failed.Item1;
                    string failPath = string.Format("db\\{0}_tables\\{0}", failType);
                    PackedFile found = null;
                    foreach (PackedFile packed in pack.Files) {
                        if (packed.FullPath.Equals(failPath)) {
                            found = packed;
                            break;
                        }
                    }
                    if (found != null) {
                        string filePath = Path.Combine(path, string.Format("{0}_{1}", failType, failed.Item2));
                        File.WriteAllBytes(Path.Combine(dir, filePath), found.Data);
                    } else {
                        Console.WriteLine("cant extract {0}", failPath);
                    }
                }
            }
        }

        #region Print Utilities
        static void printList(string label, ICollection<string> list) {
            if (list.Count != 0) {
                Console.WriteLine("{0}: {1}", label, list.Count);
                foreach (string toPrint in list) {
                    Console.WriteLine(toPrint);
                }
            }
        }

        static void printList(string label, ICollection<Tuple<string, int>> list) {
            if (list.Count != 0) {
                Console.WriteLine("{0}: {1}", label, list.Count);
                foreach (Tuple<string, int> tableVersion in list) {
                    Console.WriteLine("Type {0}, Version {1}", tableVersion.Item1, tableVersion.Item2);
                }
            }
        }
        static void printList(string label, ICollection<Tuple<string, int, int>> list) {
            if (list.Count != 0) {
                Console.WriteLine("{0}: {1}", label, list.Count);
                foreach (Tuple<string, int, int> tableVersion in list) {
                    Console.WriteLine("Type {0}, Version {1} downgraded {2}", tableVersion.Item1, tableVersion.Item2, tableVersion.Item3);
                }
            }
        }
        #endregion
    }
}
