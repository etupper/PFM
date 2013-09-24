using Common;
using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    /*
     * An SQL command deleting data from a table.
     * Can contain a where clause to selectively delete; will delete all data
     * if no where clause was given.
     */
    public class DeleteCommand : SqlCommand {
        // format of this command
        public static Regex DELETE_RE = new Regex("delete from (.*)(? where .*)", RegexOptions.RightToLeft);
        
        // the where clause
        private WhereClause whereClause;
  
        /*
         * Create delete command from given string.
         */
        public DeleteCommand (string toParse) {
            Match match = DELETE_RE.Match(toParse);
            ParseTables(match.Groups[1].Value);
            if (match.Groups.Count > 2) {
                whereClause = new WhereClause(match.Groups[2].Value);
            }
        }
        
        /*
         * Delete all entries matching the where clause if any was given,
         * or all entries if none was given.
         */
        public override void Execute() {
            foreach(PackedFile packed in PackedFiles) {
                DBFile dbFile = PackedFileDbCodec.Decode(packed);
                List<List<FieldInstance>> kept = new List<List<FieldInstance>>();
                foreach(List<FieldInstance> field in dbFile.Entries) {
                    if (whereClause != null && !whereClause.Accept(field)) {
                        kept.Add(field);
                    }
                }
                DBFile newDbFile = new DBFile(dbFile.Header, dbFile.CurrentType);
                newDbFile.Entries.AddRange(kept);
                packed.Data = PackedFileDbCodec.GetCodec(packed).Encode(newDbFile);
            }
        }
        /*
         * If the ToSave pack file was set, store its data.
         */
        public override void Commit() {
            if (SaveTo != null) {
                new PackFileCodec().Save(SaveTo);
            }
        }
    }
}

