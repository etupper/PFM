using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Common;
using Filetypes;

namespace PackFileTest {
    class PackTest {
#pragma warning disable 414
        // just to keep track of what's available in testAll:
        // -mt: run building models test
        // -mb: run naval models test
        private static string[] OPTIONS = { 
            // -db: test db files; -t: also run tsv export/reimport test
            "-db", "-t",
            // -Xm: run building/naval model tests
            "-bm", "-nm",
            // -uv: run unit variant tests
            "-uv", 
            // -gf: run group formation tests
            "-gf", 
            // -os: optimize schema
            "-os", 
            // -pr: prepare release... split schemata into each of the games'
            "-pr", 
            // -w : write schema_user.xml after iterating all db files
            "-w",
            // -i : integrate other schema file
            "-i",
            // -v : verbose output
            "-v",
            // -tg: test game
            "-tg",
            // -x : don't wait for user input after finishing
            "-x",
            // -cr: check references
            "-cr",
            // -mx: find corresponding fields in mod tools xml files
            "-mx"
        };
#pragma warning restore 414

		bool testTsvExport = false;
        bool outputTables = false;
        bool verbose = false;
        bool waitForKey = true;


        // private List<PackedFileTest> tests = new List<PackedFileTest>();
        private List<PackedFileTest.TestFactory> testFactories = new List<PackedFileTest.TestFactory>();

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
            
            bool saveSchema = false;
            foreach (string dir in arguments) {
                if (dir.StartsWith("#") || dir.Trim().Equals("")) {
                    continue;
                }
                if (dir.Equals("-pr")) {
                    PrepareRelease();
                } else if (dir.Equals("-v")) {
                    Console.WriteLine("Running in verbose mode");
                    verbose = true;
                } else if (dir.Equals("-cr")) {
                    CheckReferences();
                } else if (dir.Equals("-x")) {
                    waitForKey = false;
                } else if (dir.StartsWith("-i")) {
                    string integrateFrom = dir.Substring(2);
                    SchemaIntegrator integrator = new SchemaIntegrator{
                        Verbose = verbose
                    };
                    string[] files = integrateFrom.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string file in files) {
                        integrator.IntegrateFile(file);
                    }
                } else if (dir.Equals("-t")) {
                    Console.WriteLine("TSV export/import enabled");
                    testTsvExport = true;
                } else if (dir.Equals("-db")) {
                    Console.WriteLine("Database Test enabled");
                    testFactories.Add(CreateDbTest);
                } else if (dir.Equals("-uv")) {
                    Console.WriteLine("Unit Variant Test enabled");
                    testFactories.Add(CreateUnitVariantTest);
                } else if (dir.StartsWith("-gf")) {
                    Console.WriteLine("Group formations test enabled");
                    GroupformationTest test = new GroupformationTest();
                    test.testFile(dir.Split(" ".ToCharArray())[1].Trim());
                } else if (dir.Equals("-w")) {
                    saveSchema = true;
                    Console.WriteLine("will save schema_user.xml after run");
                } else if (dir.Equals("-ot")) {
                    outputTables = true;
                    Console.WriteLine("will output tables of db files");
                } else if (dir.StartsWith("-tg")) {
                    string gameName = dir.Substring(3);
                    Game game = Game.ById(gameName);
                    Console.WriteLine("Testing game {0}", gameName);
                    PackedFileTest.TestAllPacks(testFactories, Path.Combine(game.DataDirectory), verbose);
//                } else if (dir.Equals("-bm")) {
//                    testFactories.Add(CreateBuildingModelTest);
//                    Console.WriteLine("building models test enabled");
//                } else if (dir.Equals("-nm")) {
//                    testFactories.Add(CreateNavalModelTest);
//                    Console.WriteLine("naval models test enabled");
                } else if (dir.StartsWith("-mx")) {
                    string[] split = dir.Substring(3).Split(Path.PathSeparator);
                    FindCorrespondingFields(split[0], split[1]);
                } else {
                    PackedFileTest.TestAllPacks(testFactories, dir, verbose);
                }
            }
            if (saveSchema) {
                SaveSchema();
            }
            if (waitForKey) {
                Console.WriteLine("Test run finished, press any key");
                Console.ReadKey();
            }
        }
        
        void FindCorrespondingFields(string packFile, string xmlDirectory) {
            TableNameCorrespondencyManager manager = new TableNameCorrespondencyManager();
            FieldCorrespondencyFinder finder = new FieldCorrespondencyFinder(packFile, xmlDirectory);
            finder.FindAllCorrespondencies();
            foreach (string tableName in finder.CorrespondingFields.Keys) {
                manager.TableMapping.Add(tableName, finder.CorrespondingFields[tableName]);
            }
            manager.SaveToFile();
        }
        
        void CheckReferences() {
            List<ReferenceChecker> checkers = ReferenceChecker.CreateCheckers();
            foreach (string packPath in Directory.GetFiles(Game.ETW.DataDirectory, "*pack")) {
                Console.WriteLine("adding {0}", packPath);
                PackFile packFile = new PackFileCodec().Open(packPath);
                foreach (ReferenceChecker checker in checkers) {
                    checker.PackFiles.Add(packFile);
                }
            }
            Console.WriteLine();
            Console.Out.Flush();
            foreach (ReferenceChecker checker in checkers) {
                checker.CheckReferences();
                Dictionary<PackFile, CheckResult> result = checker.FailedResults;
                foreach (PackFile pack in result.Keys) {
                    CheckResult r = result[pack];
                    Console.WriteLine("pack {0} failed reference from {1} to {2}",
                        pack.Filepath, r.ReferencingString, r.ReferencedString, string.Join(",", r.UnfulfilledReferences));
                }
            }
        }

        void PrepareRelease() {
            DBTypeMap.Instance.initializeFromFile("master_schema.xml");
            List<Thread> threads = new List<Thread>();
            foreach (Game game in Game.Games) {
                if (game.IsInstalled) {
                    string datapath = Path.Combine(game.GameDirectory, "data");
                    string outfile = string.Format("schema_{0}.xml", game.Id);
                    SchemaOptimizer optimizer = new SchemaOptimizer() {
                        PackDirectory = datapath,
                        SchemaFilename = outfile
                    };
                    ThreadStart start = new ThreadStart(optimizer.FilterExistingPacks);
                    Thread worker = new Thread(start);
                    threads.Add(worker);
                    worker.Start();
                    // Console.WriteLine("{0} entries removed for {1}", optimizer.RemovedEntries, game.Id);
                }
            }
            threads.ForEach(t => t.Join());
        }

        #region Test Factory Methods
//        PackedFileTest CreateBuildingModelTest() {
//            return new ModelsTest<BuildingModel> {
//                Codec = BuildingModelCodec.Instance,
//                ValidTypes = "models_building_tables",
//                Verbose = verbose
//            };
//        }
//        PackedFileTest CreateNavalModelTest() {
//            return new ModelsTest<ShipModel> {
//                Codec = ShipModelCodec.Instance,
//                ValidTypes = "models_naval_tables",
//                Verbose = verbose
//            };
//        }
        PackedFileTest CreateUnitVariantTest() {
            return new UnitVariantTest();
        }
        PackedFileTest CreateDbTest() {
            return new DBFileTest {
                TestTsv = testTsvExport,
                OutputTable = outputTables,
                Verbose = verbose
            };
        }
        #endregion

        #region Save Schema
        static readonly Regex NumberedFieldNameRe = new Regex("([^0-9]*)([0-9]+)");
        void SaveSchema() {
            foreach (List<FieldInfo> typeInfos in DBTypeMap.Instance.TypeMap.Values) {
                MakeFieldNamesUnique(typeInfos);
            }
            foreach (List<FieldInfo> typeInfos in DBTypeMap.Instance.GuidMap.Values) {
                MakeFieldNamesUnique(typeInfos);
            }
            DBTypeMap.Instance.saveToFile(Directory.GetCurrentDirectory(), "user");
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
                if (NumberedFieldNameRe.IsMatch(result)) {
                    Match match = NumberedFieldNameRe.Match(result);
                    number = int.Parse(match.Groups[2].Value) + 1;
                    result = string.Format("{0}{1}", match.Groups[1].Value, number);
                } else {
                    result = string.Format("{0}{1}", name, number);
                }
            }
            return result;
        }
        #endregion
    }
	
}
