using Common;
using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    using RowValues = List<string>;

    public interface IValueSource {
        List<RowValues> Values { get; }
    }
    
    /*
     * SQL command inserting a new row to a table.
     */
    class InsertCommand : SqlCommand {
        // form of the insert command: groups are 1-table to insert into; 2-ValueSource
        public static Regex INSERT_RE = new Regex("insert into (.*) (.*)", RegexOptions.RightToLeft);

        IValueSource Source { get; set; }
        
        public override IEnumerable<PackedFile> PackedFiles {
            protected get {
                return base.PackedFiles;
            }
            set {
                base.PackedFiles = value;
                SelectCommand selectCommand = Source as SelectCommand;
                if (selectCommand != null) {
                    selectCommand.PackedFiles = value;
                    selectCommand.Execute();
                }
            }
        }
        
        /*
         * Parse the given string to create an insert command.
         */
        public InsertCommand (string toParse) {
            Match match = INSERT_RE.Match(toParse);
            foreach (string table in match.Groups[1].Value.Split(',')) {
                tables.Add(table.Trim());
            }
            Source = ParseValueSource (match.Groups[2].Value);
        }
        
        /*
         * Insert the previously given values into the db table.
         * A warning will be printed and no data added if the given data doesn't
         * fit the db file's structure.
         */
        public override void Execute() {
            // insert always into packed files at the save to file
            PackedFiles = SaveTo;
            foreach(PackedFile packed in PackedFiles) {
                DBFile file = PackedFileDbCodec.Decode(packed);
                foreach(RowValues insertValues in Source.Values) {
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
                        Console.WriteLine("Values: {0}", string.Join(",", insertValues));
                    }
                }
            }
        }

        /*
         * Save the pack file.
         */
        public override void Commit() {
            if (SaveTo != null) {
                Console.WriteLine("committing {0}", SaveTo.Filepath);
                foreach(PackedFile packed in PackedFiles) {
                    SaveTo.Add(packed, true);
                }
                new PackFileCodec().Save(SaveTo);
            }
        }
        
        IValueSource ParseValueSource(string toParse) {
            IValueSource source = null;
            if (FixedValues.VALUES_RE.IsMatch(toParse)) {
                source = new FixedValues(toParse);
            } else if (SelectCommand.SELECT_RE.IsMatch(toParse)) {
                SelectCommand selectCommand = new SelectCommand(toParse) {
                    PackedFiles = this.PackedFiles
                };
                selectCommand.Execute();
                source = selectCommand;
            }
            return source;
        }
    }
    
    /*
     * A value source providing exactly one row with fixed values;
     * this is parsed from an SQL "values(v1,v2,v3)" expression.
     */
    class FixedValues : IValueSource {
        public static Regex VALUES_RE = new Regex("values *\\(.*\\)");
        
        public List<RowValues> Values { get; private set; }
        
        public FixedValues(string toParse) {
            Values = new List<RowValues>();
            Match match = VALUES_RE.Match(toParse);
            RowValues insertValues = new List<string>();
            foreach(string val in match.Groups[1].Value.Split(',')) {
                insertValues.Add(val.Trim());
            }
            Values.Add(insertValues);
        }
    }
}

