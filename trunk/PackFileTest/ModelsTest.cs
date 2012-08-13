using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Filetypes;

namespace PackFileTest {
    public class ModelsTest<T, E> : PackedFileTest 
    where T : ModelContainer<E> 
    where E : ModelEntry {
        public ModelCodec<T, E> Codec {
            get; set;
        }
        public string ValidTypes {
            get; set;
        }
        public override int TestCount {
            get {
                return successes.Count + countErrors.Count + generalErrors.Count;
            }
        }
        
        List<string> countErrors = new List<string>();
        List<string> successes = new List<string>();
        List<string> emptyFiles = new List<string>();
        
        public override bool CanTest(PackedFile file) {
            return DBFile.typename(file.FullPath).Equals (ValidTypes);
        }
        
        public override void TestFile(PackedFile packed) {
            if (packed.Data.Length == 0) {
                emptyFiles.Add(packed.FullPath);
                return;
            }
            using (var stream = new MemoryStream(packed.Data)) {
                ModelFile<T> bmFile = Codec.Decode(stream);
                if (bmFile.Header.EntryCount != bmFile.Models.Count) {
                    countErrors.Add(string.Format("{0}: invalid count. Should be {1}, is {2}",
                                                  packed.Name, bmFile.Header.EntryCount, bmFile.Models.Count));
                } else {
                    successes.Add(string.Format("{0}", packed.FullPath));
                }
            }
        }
        
        public override void PrintResults() {
            if (TestCount != 0) {
                Console.WriteLine("BuildingModels test ({0}):", ValidTypes);
                Console.WriteLine("Supported Files: {0}", successes.Count);
                Console.WriteLine("Empty Files: {0}", successes.Count);
            }
            if (countErrors.Count != 0) {
                PrintList("Entry counts errors", countErrors);
            }
            if (generalErrors.Count != 0) {
                PrintList("General errors", generalErrors);
            }
        }
    }
}

