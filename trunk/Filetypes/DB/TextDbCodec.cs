using System;
using System.Collections.Generic;
using System.IO;

namespace Filetypes {
    /*
     * TSV import/export.
     */
    public class TextDbCodec : Codec<DBFile> {
        static char[] QUOTES = { '"' };
        static char[] TABS = { '\t' };
        static char[] GUID_SEPARATOR = { ':' };
        
        public static readonly Codec<DBFile> Instance = new TextDbCodec();
        
        public static byte[] Encode(DBFile file) {
            using (MemoryStream stream = new MemoryStream()) {
                TextDbCodec.Instance.Encode(stream, file);
                return stream.ToArray();
            }
        }

        public DBFile Decode(Stream stream) {
            return Decode (new StreamReader (stream));
        }

        // read from given stream
        public DBFile Decode(StreamReader reader) {
            // another tool might have saved tabs and quotes around this 
            // (at least open office does)
            string typeInfoName = reader.ReadLine ().Replace ("\t", "").Trim (QUOTES);
            string[] split = typeInfoName.Split(GUID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 2) {
                typeInfoName = split[0];
            }
            string versionStr = reader.ReadLine ().Replace ("\t", "").Trim (QUOTES);
            int version;
            switch (versionStr) {
            case "1.0":
                version = 0;
                break;
            case "1.2":
                version = 1;
                break;
            default:
                version = int.Parse (versionStr);
                break;
            }

            // ignore the table name header line
            reader.ReadLine();
   
            DBFile file = null;
            long parseStart = reader.BaseStream.Position;
            
            bool parseSuccessful = true;
            foreach(TypeInfo info in DBTypeMap.Instance.GetVersionedInfos(typeInfoName, version)) {
                reader.BaseStream.Seek(parseStart, SeekOrigin.Begin);
                string line = reader.ReadLine ();
                string[] strArray = line.Split (TABS, StringSplitOptions.None);
                // verify we have matching amount of fields
                if (strArray.Length != info.Fields.Count) {
                    continue;
                }
                List<List<FieldInstance>> entries = new List<List<FieldInstance>> ();
                while (!reader.EndOfStream) {
                    line = reader.ReadLine ();
                    try {
                        strArray = line.Split (TABS, StringSplitOptions.None);
                        List<FieldInstance> item = new List<FieldInstance> ();
                        for (int i = 0; i < strArray.Length; i++) {
                            FieldInstance field = info.Fields [i].CreateInstance();
                            string fieldValue = CsvUtil.Unformat (strArray [i]);
                            field.Value = fieldValue;
                            item.Add (field);
                        }
                        entries.Add (item);
                    } catch {
                        // Console.WriteLine (x);
                        parseSuccessful = false;
                        break;
                    }
                }
                if (parseSuccessful) {
                    DBFileHeader header = new DBFileHeader (info.ApplicableGuids[0], version, (uint)entries.Count, version != 0);
                    file = new DBFile (header, info);
                    file.Entries.AddRange (entries);
                    break;
                }
            }
            return file;
        }

        // write the given file to stream
        public void Encode(Stream stream, DBFile file) {
            StreamWriter writer = new StreamWriter (stream);
            // write header
            writer.WriteLine (file.CurrentType.Name);
            writer.WriteLine (Convert.ToString (file.Header.Version));
            foreach (FieldInfo info2 in file.CurrentType.Fields) {
                writer.Write (info2.Name + "\t");
            }
            writer.WriteLine ();
            // write entries
            foreach (List<FieldInstance> list in file.Entries) {
                string str = CsvUtil.Format (list [0].Value);
                for (int i = 1; i < list.Count; i++) {
                    string current = list [i].Value;
                    str += "\t" + CsvUtil.Format (current);
                }
                writer.WriteLine (str);
            }
            writer.Flush ();
        }
    }
}

