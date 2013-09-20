using Common;
using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    /*
     * SQL command inserting a new row to a table.
     */
    class InsertCommand : SqlCommand {
        // form of the insert command
        public static Regex INSERT_RE = new Regex("insert into (.*) values \\((.*)\\)");
  
        private List<string> insertValues = new List<string>();
        public PackFile ToSave { get; set; }
        
        /*
         * Parse the given string to create an insert command.
         */
        public InsertCommand (string toParse) {
            Match match = INSERT_RE.Match(toParse);
            foreach (string table in match.Groups[1].Value.Split(',')) {
                tables.Add(table.Trim());
            }
            foreach(string val in match.Groups[2].Value.Split(',')) {
                insertValues.Add(val.Trim());
            }
        }
        
        /*
         * Insert the previously given values into the db table.
         * A warning will be printed and no data added if the given data doesn't
         * fit the db file's structure.
         */
        public override void Execute() {
            foreach(PackedFile packed in PackedFiles) {
                DBFile file = PackedFileDbCodec.Decode(packed);
                if (file.CurrentType.Fields.Count == insertValues.Count) {
                    List<FieldInstance> newRow = file.GetNewEntry();
                    for (int i = 0; i < newRow.Count; i++) {
                        newRow[i].Value = insertValues[i];
                    }
                    file.Entries.Add (newRow);
                    packed.Data = PackedFileDbCodec.GetCodec(packed).Encode(file);
                } else {
                    Console.WriteLine("Cannot insert: was given {0} values, expecting {1} in {2}", 
                                      insertValues.Count, file.CurrentType.Fields.Count, packed.FullPath);
                }
            }
        }
        /*
         * Save the pack file.
         */
        public override void Commit() {
            if (ToSave != null) {
                Console.WriteLine("committing {0}", ToSave.Filepath);
                new PackFileCodec().Save(ToSave);
            }
        }
    }
}

