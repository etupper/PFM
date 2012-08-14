using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Common;
using Filetypes;
using PackFileManager;

namespace PackFileTest {
    class PackTest {
#pragma warning disable 414
        // just to keep track of what's available in testAll:
		// -db: test db files; -t: tsv test for db files
		// -uv: unit variant files; -ot: create output of tables
        // -pr: prepare release... split schemata into each of the games'
        // -w : write schema_user.xml after iterating all db files
        // -mt: run models test
        private static string[] OPTIONS = { "-t", "-db", "uv", "gf", "-ot", "-pr", "-w", "-mt" };
#pragma warning restore 414

		bool testDbFiles = false;
		bool testTsvExport = false;
		bool testUnitVariants = false;
        // bool testGroupformations = false;

		private static string OPTIONS_FILENAME = "testoptions.txt";

        public static void Main(string[] args) {
            new PackTest().TestAll(args);
        }

        void TestAll(string[] args) {
            IEnumerable<string> arguments = args;
            if (args.Length == 0) {
                if (File.Exists(OPTIONS_FILENAME)) {
                    arguments = File.ReadAllLines(OPTIONS_FILENAME);
                } else {
                    Console.Error.Write("Missing options; use file {0}", OPTIONS_FILENAME);
                    return;
                }
            }
            // List<string> arguments = new List<string> ();
            bool saveSchema = false;
            bool outputTables = false;
            bool runModelTests = false;
            foreach (string dir in arguments) {
                if (dir.StartsWith("#")) {
                    continue;
                }
                if (dir.Equals("-pr")) {
                    DBTypeMap.Instance.initializeFromFile("master_schema.xml");
                    foreach (Game game in Game.GetGames()) {
#if DEBUG
                            //if (!game.Id.Equals("ETW")) continue;
#endif
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
                } else if (dir.Equals("-t")) {
                    Console.WriteLine("TSV export/import enabled");
                    testTsvExport = true;
                } else if (dir.Equals("-mt")) {
                    runModelTests = true;
                    Console.WriteLine("models test enabled");
                } else if (dir.Equals("-db")) {
                    Console.WriteLine("Database Test enabled");
                    testDbFiles = true;
                    DBTypeMap.Instance.initializeTypeMap(Directory.GetCurrentDirectory());
                } else if (dir.Equals("-uv")) {
                    Console.WriteLine("Unit Variant Test enabled");
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

                    testAllPacks(dir, outputTables, runModelTests, testTsvExport);
                }
            }
            if (saveSchema) {
                foreach (List<FieldInfo> typeInfos in DBTypeMap.Instance.TypeMap.Values) {
                    MakeFieldNamesUnique(typeInfos);
                }
                foreach (List<FieldInfo> typeInfos in DBTypeMap.Instance.GuidMap.Values) {
                    MakeFieldNamesUnique(typeInfos);
                }
                DBTypeMap.Instance.saveToFile(Directory.GetCurrentDirectory(), "user");
            }
            Console.Error.WriteLine("Test run finished, press any key");
            Console.ReadKey();
        }
        void MakeFieldNamesUnique(List<FieldInfo> fields) {
            List<string> used = new List<string>();
            for (int i = 0; i < fields.Count; i++) {
                FieldInfo info = fields[i];
                if (used.Contains(info.Name)) {
                    string newName = MakeNameUnique(info.Name, used, i+1);
                    info.Name = newName;
                }
                used.Add(info.Name);
            }
        }
        string MakeNameUnique(string name, ICollection<string> alreadyUsed, int index) {
            string result = name;
            int number = index;
            while (alreadyUsed.Contains(result)) {
                if (numberedFieldNameRe.IsMatch(result)) {
                    Match match = numberedFieldNameRe.Match(result);
                    number = int.Parse(match.Groups[2].Value) + 1;
                    result = string.Format("{0}{1}", match.Groups[1].Value, number);
                } else {
                    result = string.Format("{0}{1}", name, number);
                }
            }
            return result;
        }
        static readonly Regex numberedFieldNameRe = new Regex("([^0-9]*)([0-9]+)");

        // run db tests for all files in the given directory
        public void testAllPacks(string dir, bool outputTable, bool testModels, bool testTsv = false) {
			foreach (string file in Directory.EnumerateFiles(dir, "*.pack")) {
                SortedSet<PackedFileTest> tests = new SortedSet<PackedFileTest>();
                if (testDbFiles) {
                    tests.Add(new DBFileTest (testTsv, outputTable));

                    if (testModels) {
                        tests.Add(new ModelsTest<NavalModel, ShipPart> {
                            Codec = NavalModelCodec.Instance,
                            ValidTypes = "models_naval_tables"
                        });
                        tests.Add(new ModelsTest<BuildingModel, BuildingModelEntry> {
                            Codec = BuildingModelCodec.Instance,
                            ValidTypes = "models_building_tables"
                        });
                    }
				}
				if (testUnitVariants) {
                    tests.Add(new UnitVariantTest());
				}
                TestAllFiles(file, tests);
                List<string> failedTests = new List<string>();
                foreach (PackedFileTest test in tests) {
                    if (test.FailedTests.Count > 0) {
                        failedTests.Add(test.ToString());
                        failedTests.AddRange(test.FailedTests);
                        failedTests.Add("");
                        // output results
                        // test.PrintResults();
                    }
                }
                if (failedTests.Count > 0) {
                    Console.WriteLine(string.Format("{0} - {1}", 
                        Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(file))), Path.GetFileName(file)));
                    Console.WriteLine("Dir: {0}\nTests Run:{1}", dir, tests.Count);
                    Console.Out.Flush();
                    string all = string.Join("\n", failedTests);
                    Console.WriteLine(all);
                }
                Console.Out.Flush();
            }
		}

        // tests all files in this test's pack
        public void TestAllFiles(string packFilePath, ICollection<PackedFileTest> tests) {
            PackFile packFile = new PackFileCodec().Open(packFilePath);
            foreach (PackedFile packed in packFile.Files) {
                foreach (PackedFileTest test in tests) {
                    try {
                        if (test.CanTest(packed)) {
                            test.TestFile(packed);
                        }
                    } catch (Exception x) {
                        using (var outstream = File.Create(string.Format("failed_{0}.packed", packed.Name))) {
                            using (var datastream = new MemoryStream(packed.Data)) {
                                datastream.CopyTo(outstream);
                            }
                        }
                        test.generalErrors.Add(string.Format("reading {0}: {1}", packed.FullPath, x.Message));
                    }
                }
            }
        }
    }
	
	public abstract class PackedFileTest : IComparable {
		// public string Packfile { get; set; }
		public SortedSet<string> generalErrors = new SortedSet<string> ();
		public SortedSet<string> allTestedFiles = new SortedSet<string>();
		
		public abstract bool CanTest(PackedFile file);

        public virtual List<string> FailedTests {
            get {
                List<string> list = new List<string>();
                if (generalErrors.Count > 0) {
                    list.Add("General errors:");
                    list.AddRange(generalErrors);
                }
                return list;
            }
        }
		
		public abstract void TestFile(PackedFile file);

		public abstract void PrintResults();

        public virtual int TestCount {
            get {
                return 0;
            }
        }

		public int CompareTo(object o) {
			int result = GetType().GetHashCode() - o.GetType().GetHashCode();
			return result;
		}
		
		public static void PrintList(string label, ICollection<string> list) {
			if (list.Count != 0) {
				Console.WriteLine ("{0}: {1}", label, list.Count);
				foreach (string toPrint in list) {
					Console.WriteLine (toPrint);
				}
			}
		}
	}
}
