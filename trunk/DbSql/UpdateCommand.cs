using Common;
using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    class UpdateCommand : FieldCommand {
        public static Regex UPDATE_RE = new Regex("update (.*) set (.*)( where .*)", RegexOptions.RightToLeft);

        private WhereClause whereClause;
        
        List<string> assignedValues = new List<string>();
        public PackFile ToSave { get; set; }

        public UpdateCommand(string toParse) {
            Match m = UPDATE_RE.Match(toParse);
            ParseTables(m.Groups[1].Value);
            foreach(string fieldAssignment in m.Groups[2].Value.Split(',')) {
                string[] assignment = fieldAssignment.Split('=');
                Fields.Add(assignment[0]);
                assignedValues.Add(assignment[1]);
            }
            if (m.Groups.Count > 3) {
                whereClause = new WhereClause(m.Groups[3].Value);
            }
        }
        
        public override void Execute() {
            foreach(PackedFile packed in PackedFiles) {
                DBFile dbFile = PackedFileDbCodec.Decode(packed);
                foreach(List<FieldInstance> fieldInstance in dbFile.Entries) {
                    if (whereClause != null && !whereClause.Accept(fieldInstance)) {
                        continue;
                    }
                    AdjustValues(fieldInstance);
                }
                packed.Data = PackedFileDbCodec.GetCodec(packed).Encode(dbFile);
            }
        }
        
        public override void Commit() {
            if (ToSave != null) {
                new PackFileCodec().Save(ToSave);
            }
        }
        
        private void AdjustValues(List<FieldInstance> fields) {
            foreach(FieldInstance field in fields) {
                if (Fields.Contains(field.Info.Name)) {
                    int index = Fields.IndexOf(field.Info.Name);
                    field.Value = assignedValues[index];
                }
            }
        }
    }
}