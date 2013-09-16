using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Common;
using Filetypes;
using PackFileTest.Mapping;

namespace PackFileTest {
    class PackTest {
#pragma warning disable 414
        // just to keep track of what's available in testAll:
        // -mt: run building models test
        // -mb: run naval models test
        private static string[] OPTIONS = { 
            // -tm: initialize DBTypeMap from given game
            "-tm",
            // -db: test db files; -t: also run tsv export/reimport test
            "-db", "-t",
            // -Xm: run building/naval model tests
            "-bm", "-nm",
            // -uv: run unit variant tests
            "-uv", 
            // -g: set games to run against (default: none)
            "-g",
            // -gf: run group formation tests
            "-gf", 
            // -os: optimize schema
            "-os", 
            // -pr: prepare release... split schemata into each of the games'
            "-pr", 
            // -w : write schema_user.xml after iterating all db files
            "-w",
            // -ca: read CA schema files and get the references
            "-ca",
            // -vd: read CA files and verify if each line in pack data has an xml correspondence
            "-vs",
            // -as: add entries from other schema file by GUID if they don't exists already
            "-as",
            // -cs: read from CA schema directory and correct references
            "-cs",
            // -i : integrate other schema file, overwrite existing entries
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
            "-mx",
            // -dg: dump guids from pack db files
            "-dg"
        };
#pragma warning restore 414

		bool testTsvExport = false;
        bool outputTables = false;
        bool verbose = false;
        bool waitForKey = true;

        private List<PackedFileTest.TestFactory> testFactories = new List<PackedFileTest.TestFactory>();
        private List<Game> games = new List<Game>();
        private SchemaIntegrator integrator = new SchemaIntegrator {
            OverwriteExisting = true,
            IntegrateExisting = true,
            CheckAllFields = true
        };

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
            
            // make sure R2 knows its directory (no autodetect)
            if (File.Exists("gamedir_r2tw.txt")) {
                Game.R2TW.GameDirectory = File.ReadAllText("gamedir_r2tw.txt").Trim();
            }
            
            bool saveSchema = false;
            foreach (string dir in arguments) {
                if (dir.StartsWith("#") || dir.Trim().Equals("")) {
                    continue;
                }
                if (dir.Equals("-pr")) {
                    PrepareRelease();
                } else if (dir.StartsWith("-g")) {
                    games.Clear();
                    string gamesArg = dir.Substring(2);
                    if ("ALL".Equals(gamesArg)) {
                        games.AddRange(Game.Games);
                    } else {
                        string[] gameIds = gamesArg.Split(new char[] { ','}, StringSplitOptions.RemoveEmptyEntries);
                        foreach(string gameId in gameIds) {
                            games.Add(Game.ById(gameId));
                        }
                    }
                } else if (dir.Equals("-v")) {
                    Console.WriteLine("Running in verbose mode");
                    verbose = true;
                } else if (dir.Equals("-cr")) {
                    CheckReferences();
                } else if (dir.Equals("-x")) {
                    waitForKey = false;
                } else if (dir.StartsWith("-tm")) {
                    string typeMapFile = dir.Substring(3);
                    DBTypeMap.Instance.initializeFromFile(typeMapFile);
                } else if (dir.StartsWith("-ca")) {
                    string path = dir.Substring(3);
                    IntegrateAll(path, integrator.AddCaData);
                } else if (dir.StartsWith("-as")) {
                    string path = dir.Substring(3);
                    IntegrateAll(path, integrator.AddSchemaFile);
                } else if (dir.StartsWith("-i")) {
                    string path = dir.Substring(2);
                    IntegrateAll(path, integrator.IntegrateFile);
                } else if (dir.StartsWith("-vd")) {
                    string path = dir.Substring(3);
                    IntegrateAll(path, integrator.VerifyData);
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
                    foreach(Game game in games) {
                        Console.WriteLine("Testing game {0}", game.Id);
                        PackedFileTest.TestAllPacks(testFactories, Path.Combine(game.DataDirectory), verbose);
                    }
                } else if (dir.StartsWith("-mx")) {
                    string[] split = dir.Substring(3).Split(Path.PathSeparator);
                    FindCorrespondingFields(split[0], split[1]);
                } else if (dir.StartsWith("-cs")) {
                    ReplaceSchemaNames(dir.Substring(3));
                } else if (dir.StartsWith("-dg")) {
                    string file = dir.Substring(3);
                    PackFile pack = null;
                    List<string> tables = new List<string>();
                    foreach(string line in File.ReadAllLines(file)) {
                        if (pack == null) {
                            pack = new PackFileCodec().Open(line);
                        } else {
                            tables.Add(line);
                        }
                    }
                    DumpAllGuids(pack, tables);
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
        
        delegate void Integrate(string path);
        
        void IntegrateAll(string paths, Integrate integrate)  {
            if (games.Count == 0) {
                Console.WriteLine("Warning: no games set for integration!");
            }
            foreach(Game g in games) {
                integrator.VerifyAgainst = g;
                integrator.Verbose = verbose;
                string[] files = paths.Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
                foreach(string file in files) {
                    integrate(file);
                }
            }
        }
        
        void DumpAllGuids(PackFile pack, List<string> tables) {
            foreach(PackedFile file in pack) {
                if (file.FullPath.StartsWith("db")) {
                    string table = DBFile.typename(file.FullPath);
                    if (tables.Contains(table)) {
                        DBFileHeader header = PackedFileDbCodec.readHeader(file);
                        Console.WriteLine("{0} - {1}", table, header.GUID);
                    }
                }
            }
        }

        void ReplaceSchemaNames(string xmlDirectory) {
            Dictionary<Tuple<string, string>, string> renamedFields = new Dictionary<Tuple<string, string>, string>();
            foreach (string table in DBTypeMap.Instance.DBFileTypes) {
                string lookupString = table.Replace("_tables", "");
                Console.WriteLine("table {0}", table);
                List<TypeInfo> infos = DBTypeMap.Instance.GetAllInfos(table);
                string guid;
                foreach (TypeInfo typeInfo in infos) {
                    List<CaFieldInfo> caInfos = CaFieldInfo.ReadInfo(xmlDirectory, lookupString, out guid);

                    foreach (FieldInfo info in typeInfo.Fields) {
                        string newName = FieldMappingManager.Instance.GetXmlFieldName(lookupString, info.Name);
                        if (newName != null) {
                            // remember rename to be able to correct references to this later
                            Tuple<string, string> tableFieldTuple = new Tuple<string, string>(table, info.Name);
                            if (!renamedFields.ContainsKey(tableFieldTuple)) {
                                renamedFields.Add(tableFieldTuple, newName);
                            }
                            info.Name = newName;
                            Console.WriteLine("{0}->{1}", info.Name, newName);

                            CaFieldInfo caInfo = CaFieldInfo.FindInList(caInfos, newName);
                            if (caInfo != null) {
                                FieldReference reference = caInfo.Reference;
                                if (reference != null) {
                                    reference.Table = string.Format("{0}_tables", reference.Table);
                                }
                                info.FieldReference = reference;
                            }
                        }
                    }
                }
            }
        }

        void FindCorrespondingFields(string packFile, string modToolsDirectory) {
            string xmlDirectory = Path.Combine(modToolsDirectory, "db");
            //string empireDesignDirectory = Path.Combine(modToolsDirectory, "EmpireDesignData");

            //foreach (string twadFile in Directory.GetFiles(xmlDirectory, "TWaD_*")) {
            //    string xmlFileName = Path.GetFileName(twadFile).Replace("TWaD_", "");
            //    if (xmlFileName.StartsWith("TExc")) {
            //        continue;
            //    }
            //    string xmlFilePath = Path.Combine(xmlDirectory, xmlFileName);
            //    if (File.Exists(xmlFilePath)) {
            //        string tableName = Path.GetFileNameWithoutExtension(xmlFileName);
            //        new UniqueTableGenerator(xmlDirectory, tableName).GenerateTable();
            //        File.Copy(xmlFilePath, Path.Combine(empireDesignDirectory, xmlFileName), true);
            //    }
            //}

            FieldMappingManager manager = FieldMappingManager.Instance;
            FieldCorrespondencyFinder finder = new FieldCorrespondencyFinder(packFile, xmlDirectory);
            // finder.RetainExistingMappings = true;
            finder.FindAllCorrespondencies();
            Console.WriteLine("saving");
            manager.Save();
        }
        
        void CheckReferences() {
            foreach(Game game in games) {
                if (!game.IsInstalled) {
                    continue;
                }
                List<ReferenceChecker> checkers = ReferenceChecker.CreateCheckers();
                foreach (string packPath in Directory.GetFiles(game.DataDirectory, "*pack")) {
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
        }

        void PrepareRelease() {
            DBTypeMap.Instance.initializeFromFile("master_schema.xml");
            List<Thread> threads = new List<Thread>();
            foreach (Game game in Game.Games) {
                if (game.IsInstalled) {
                    string datapath = game.DataDirectory;
                    string outfile = string.Format("schema_{0}.xml", game.Id);
                    SchemaOptimizer optimizer = new SchemaOptimizer() {
                        PackDirectory = datapath,
                        SchemaFilename = outfile
                    };
                    //optimizer.FilterExistingPacks();
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
            DBTypeMap.Instance.SaveToFile(Directory.GetCurrentDirectory(), "user");
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
