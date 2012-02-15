using System;
using System.IO;
using System.Collections.Generic;
using Common;

namespace PackFileTest {
    public class DBFileTest : IComparable {
        // to conveniently set breakpoints for certain tables.
        private string debug_at = "sound_events";

        public string Packfile {
            get;
            set;
        }
        private bool testTsv;

        #region Result lists
        public SortedSet<string> generalErrors = new SortedSet<string>();
        public SortedSet<Tuple<string, int>> supported = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
        public SortedSet<Tuple<string, int>> noDefinition = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
        public SortedSet<Tuple<string, int>> noDefForVersion = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
        public SortedSet<Tuple<string, int>> invalidDefForVersion = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
        public SortedSet<Tuple<string, int>> emptyTables = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
        public SortedSet<Tuple<string, int>> tsvFails = new SortedSet<Tuple<string, int>>(VERSION_COMPARE);
        #endregion

        public DBFileTest(string file, bool tsv) {
            Packfile = file;
            testTsv = tsv;
        }

        // tests all db files in this test's pack
        public void testAllFiles() {
            string currentFile = "";
            PackFile packFile = new PackFileCodec().Open(Packfile);
            foreach (PackedFile packed in packFile.Files) {
                try {
                    currentFile = packed.FullPath;
                    if (currentFile.StartsWith("db")) {
                        testDbFile(packed);
                    }
                } catch (Exception x) {
                    generalErrors.Add(string.Format("reading {0}: {1}", packed.FullPath, x.Message));
                }
            }
        }

        // test the given packed file as a database file
        // tests PackedFileCodec and the db definitions we have
        public void testDbFile(PackedFile file) {
            PackedFileDbCodec packedCodec = new PackedFileDbCodec();
            string type = DBFile.typename(file.FullPath);
            DBFileHeader header = PackedFileDbCodec.readHeader(file);
            Tuple<string, int> tuple = new Tuple<string, int>(type, header.Version);
            if (header.EntryCount == 0) {
                // special case: we will never find out the structure of a file
                // if it contains no data
                emptyTables.Add(tuple);
            } else if (DBTypeMap.Instance.IsSupported(type)) {
                SortedSet<Tuple<string, int>> addTo = null;
                try {
                    if (file.FullPath.EndsWith(debug_at)) {
                        Console.WriteLine("stop right here");
                    }
                    // a wrong db definition might not cause errors,
                    // but read less entries than there are
                    DBFile dbFile = packedCodec.readDbFile(file);
                    if (dbFile.Entries.Count == dbFile.Header.EntryCount) {
                        addTo = supported;
                        // only test tsv import/export if asked,
                        // it takes some time more than just the read checks
                        if (testTsv) {
                            testTsvExport(dbFile);
                        }
                    } else {
                        // didn't get what we expect
                        addTo = invalidDefForVersion;
                    }
                } catch {
                    addTo = invalidDefForVersion;
                }
                addTo.Add(tuple);
            } else {
                noDefinition.Add(tuple);
            }
        }

        // test the tsv codec
        public void testTsvExport(DBFile originalFile) {
            Tuple<string, int> tuple = new Tuple<string, int>(originalFile.CurrentType.name, originalFile.Header.Version);
            DBFile reimport;
            try {
                // export to tsv
                TextDbCodec codec = new TextDbCodec();
                string exportPath = Path.Combine(Path.GetTempPath(), "exportTest.tsv");
                if (originalFile.CurrentType.name.Equals(debug_at)) {
                    Console.WriteLine("stop right here");
                }
                using (Stream filestream = File.Open(exportPath, FileMode.Create)) {
                    codec.writeDbFile(filestream, originalFile);
                }
                // re-import
                using (Stream filestream = File.OpenRead(exportPath)) {
                    reimport = codec.readDbFile(filestream);
                    // check all read values against original ones
                    for (int row = 0; row < originalFile.Entries.Count; row++) {
                        for (int column = 0; column < originalFile.CurrentType.fields.Count; column++) {
                            FieldInstance originalValue = originalFile[row, column];
                            FieldInstance reimportValue = reimport[row, column];
                            if (!originalValue.Equals(reimportValue)) {
                                tsvFails.Add(tuple);
                            }
                        }
                    }
                }
            } catch (Exception x) {
                Console.WriteLine(x);
                tsvFails.Add(tuple);
            }
        }

        public int CompareTo(object o) {
            int result = 0;
            if (o is DBFileTest) {
                result = Packfile.CompareTo((o as DBFileTest).Packfile);
            }
            return result;
        }

        // i like sorted things
        class TableVersionComparer : IComparer<Tuple<string, int>> {
            public int Compare(Tuple<string, int> a, Tuple<string, int> b) {
                int result = a.Item1.CompareTo(b.Item1);
                if (result == 0) {
                    result = a.Item2.CompareTo(a.Item2);
                }
                return result;
            }
        }
        static IComparer<Tuple<string, int>> VERSION_COMPARE = new TableVersionComparer();
    }
}