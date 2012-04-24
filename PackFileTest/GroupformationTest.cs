using System;
using System.IO;
using Filetypes;

namespace PackFileTest {
    public class GroupformationTest {
        GroupformationCodec codec = new GroupformationCodec();
        public void testFile(string filename) {
            using (Stream stream = new MemoryStream(File.ReadAllBytes(filename))) {
                GroupformationFile gfFile = codec.Decode(stream);
                foreach(Groupformation formation in gfFile.Formations) {
                    Console.WriteLine("Formation: {0}", formation.Name);
                }
            }
        }
    }
}
