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

            DBFile file = null;
            long parseStart = reader.BaseStream.Position;
            
            bool parseSuccessful = true;
            foreach(TypeInfo info in DBTypeMap.Instance.GetVersionedInfos(typeInfoName, version)) {
                reader.BaseStream.Seek(parseStart, SeekOrigin.Begin);
                string line = reader.ReadLine ();
                // the title line isn't written with trailing tabs anymore...
                // but it used to, so to stay compatible with earlier exported TSVs,
                // remove empty entries
                string[] strArray = line.Split (TABS, StringSplitOptions.RemoveEmptyEntries);
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
#if DEBUG
                    } catch (Exception x) {
                        Console.WriteLine (x);
#else
                    } catch {
#endif
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
            List<string> toWrite = new List<string>();
            file.CurrentType.Fields.ForEach(f => toWrite.Add(f.Name));
            writer.Write(string.Join("\t", toWrite));
            // write entries
            file.Entries.ForEach(e => {
                toWrite.Clear();
                e.ForEach(field => toWrite.Add(field.Value));
                writer.Write(string.Join("\t", toWrite));
            });
            writer.Flush ();
        }
    }
}

