using System;
using System.Collections.Generic;
using System.IO;
using Common;
using PackFileManager;

namespace PackFileTest {
    class PackTest {
#pragma warning disable 414
        // just to keep track of what's available in testAll:
		// -db: test db files; -t: tsv test for db files
		// -uv: unit variant files; -ot: create output of tables
        // -pr: prepare release... split schemata into each of the games'
        private static string[] OPTIONS = { "-t", "-db", "uv", "gf", "-ot", "-pr" };
#pragma warning restore 414

		bool testDbFiles = false;
		bool testTsvExport = false;
		bool testUnitVariants = false;
        // bool testGroupformations = false;

		private static string OPTIONS_FILENAME = "testoptions.txt";

        public static void Main(string[] args) {
            new PackTest().testAll(args);
        }

        void testAll(string[] args) {
			// List<string> arguments = new List<string> ();
			// run tests for all packs in all dirs given in the options file
			if (File.Exists (OPTIONS_FILENAME)) {
                bool saveSchema = false;
                bool outputTables = false;
				foreach (string dir in File.ReadAllLines(OPTIONS_FILENAME)) {
					if (dir.StartsWith ("#")) {
						continue;
					}
                    if (dir.Equals("-pr")) {
                        DBTypeMap.Instance.initializeFromFile("master_schema.xml");
                        foreach(Game game in Game.GetGames()) {
                            if (game.IsInstalled) {
                                string datapath = Path.Combine(game.GameDirectory, "data");
                                string outfile = string.Format("schema_{0}.xml", game.Id);
                                SchemaOptimizer optimizer = new SchemaOptimizer() {
                                    PackDirectory = datapath,
                                    SchemaFilename = outfile
                                };
                                optimizer.FilterExistingPacks();
                                Console.WriteLine("{0} entries removed for {1}", optimizer.RemovedEntries, game.Id);
                            }
                        }
                        return;
					} else if (dir.Equals ("-t")) {
						Console.WriteLine ("TSV export/import enabled");
						testTsvExport = true;
					} else if (dir.Equals ("-db")) {
						Console.WriteLine ("Database Test enabled");
						testDbFiles = true;
						DBTypeMap.Instance.initializeTypeMap (Directory.GetCurrentDirectory ());
					} else if (dir.Equals ("-uv")) {
						Console.WriteLine ("Unit Variant Test enabled");
						testUnitVariants = true;
                    } else if (dir.StartsWith("-gf")) {
                        Console.WriteLine("Group formations test enabled");
                        // testGroupformations = true;
                        GroupformationTest test = new GroupformationTest();
                        test.testFile(dir.Split(" ".ToCharArray())[1].Trim());
                    } else if (dir.Equals("-w")) {
                        saveSchema = true;
                        Console.WriteLine("will save schema_user.xml after run");
                    } else if (dir.Equals("-ot")) {
                        outputTables = true;
                        Console.WriteLine("will output tables of db files");
					} else {
                        if (outputTables) {
                            string schemaFile = "schema_optimized.xml";
                            if (dir.Contains("stw")) {
                                schemaFile = "schema_stw.xml";
                            } else if (dir.Contains("ntw")) {
                                schemaFile = "schema_ntw.xml";
                            } else if (dir.Contains("etw")) {
                                schemaFile = "schema_etw.xml";
                            }
                            SchemaOptimizer optimizer = new SchemaOptimizer() {
                                PackDirectory = dir,
                                SchemaFilename = schemaFile
                            };
                            optimizer.FilterExistingPacks();
                            Console.WriteLine("Optimizer removed {0} entries from schema {1}", optimizer.RemovedEntries, schemaFile);
                        }
                        
						ICollection<PackedFileTest> tests = testAllPacks (dir, outputTables, testTsvExport);
						Console.WriteLine ("Dir: {0}\nTests Run:{1}", dir, tests.Count);
                        Console.Out.Flush();
						foreach (PackedFileTest test in tests) {
                            if (test.TestCount > 0) {
                                Console.WriteLine(test.Packfile);
                                // output results
                                test.printResults();
                            }
						}
					}
				}
                if (saveSchema) {
                    DBTypeMap.Instance.saveToFile(Directory.GetCurrentDirectory());
                }
				Console.Error.WriteLine ("Test run finished, press any key");
				Console.ReadKey ();
			} else {
				Console.Error.Write ("Missing options file {0}", OPTIONS_FILENAME);
			}
		}

        // run db tests for all files in the given directory
        public SortedSet<PackedFileTest> testAllPacks(string dir, bool outputTable, bool testTsv = false) {
			SortedSet<PackedFileTest> tests = new SortedSet<PackedFileTest> ();
			foreach (string file in Directory.EnumerateFiles(dir, "*.pack")) {
				if (testDbFiles) {
					DBFileTest test = new DBFileTest (file, testTsv, outputTable);
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

        public virtual int TestCount {
            get {
                return 0;
            }
        }

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
