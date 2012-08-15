using System;
using System.IO;
using System.Collections.Generic;
using Common;

namespace PackFileTest {
    /*
     * Base class for tests run against packed files.
     */
    public abstract class PackedFileTest : IComparable {
        public delegate PackedFileTest TestFactory();

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
        
        // run db tests for all files in the given directory
        public static void TestAllPacks(ICollection<TestFactory> testFactories, string dir) {
            List<PackedFileTest> tests = new List<PackedFileTest>();
            foreach(TestFactory createTest in testFactories) {
                tests.Add(createTest());
            }
            foreach (string file in Directory.EnumerateFiles(dir, "*.pack")) {
                TestAllFiles(tests, file);
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
        public static void TestAllFiles(ICollection<PackedFileTest> tests, string packFilePath) {
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
}

