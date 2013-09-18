using Common;
using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    public class DeleteCommand : SqlCommand {
        public static Regex DELETE_RE = new Regex("delete from (.*)( where .*)", RegexOptions.RightToLeft);
        
        private WhereClause whereClause;
        public PackFile ToSave { get; set; }

        public DeleteCommand (string toParse) {
            Match match = DELETE_RE.Match(toParse);
            ParseTables(match.Groups[1].Value);
            if (match.Groups.Count > 2) {
                whereClause = new WhereClause(match.Groups[2].Value);
            }
        }
        
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
        
        public override void Commit() {
            if (ToSave != null) {
                new PackFileCodec().Save(ToSave);
            }
        }
    }
}

