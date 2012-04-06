using System;
using System.IO;
using System.Collections.Generic;
using Common;

namespace PackFileTest {
	public class UnitVariantTest : PackedFileTest {
        UnitVariantCodec codec = new UnitVariantCodec();
        
        SortedSet<string> supported = new SortedSet<string>();
		SortedSet<string> wrongSize = new SortedSet<string>();
		SortedSet<string> wrongData = new SortedSet<string>();
		
		public UnitVariantTest (string filepath) {
			Packfile = filepath;
		}
		
		public override bool canTest(PackedFile file) {
			return file.FullPath.EndsWith (".unit_variant");
		}
		
		public override void testFile(PackedFile file) {
            byte[] original = file.Data;
            UnitVariantFile uvFile = null;
            using (MemoryStream stream = new MemoryStream(original, 0, original.Length)) {
                uvFile = codec.Decode(stream);
            }
			byte[] bytes = UnitVariantCodec.Encode (uvFile);
			if (file.Size != bytes.Length) {
				wrongSize.Add (file.FullPath);
			} else {
				// verify data
				byte[] origData = file.Data;
				for (int i = 0; i < origData.Length; i++) {
					if (origData [i] != bytes [i]) {
						wrongData.Add (file.FullPath);
						return;
					}
				}
				supported.Add (file.FullPath);
			}
		}
		
		public override void printResults() {
			if (allTestedFiles.Count != 0) {
				Console.WriteLine ("Unit Variant Test:");
				Console.WriteLine ("Successful: {0}/{1}", supported.Count, allTestedFiles.Count);
				printList ("Wrong Size", wrongSize);
				printList ("Wrong Data", wrongData);
			}
		}
	}
}

