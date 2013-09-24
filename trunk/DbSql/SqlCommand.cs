using Common;
using Filetypes;
using System;
using System.Collections.Generic;

namespace DbSql {
    public abstract class SqlCommand {
        public virtual void Commit() {}
        protected bool AllTables { get; set; }
        // the pack file to store upon commit
        public PackFile SaveTo { get; set; }
        
        private IEnumerable<PackedFile> allPackedFiles;
        public virtual IEnumerable<PackedFile> PackedFiles { 
            protected get {
                List<PackedFile> filtered = new List<PackedFile>();
                if (allPackedFiles == null) {
                    return filtered;
                }
                foreach(PackedFile file in allPackedFiles) {
                    if (!file.FullPath.StartsWith("db")) {
                        continue;
                    }
                    string tableType = DBFile.Typename(file.FullPath);
                    if (AllTables || tables.Contains(tableType)) {
                        filtered.Add(file);
                    }
                }
                return filtered;
            }
            set {
                allPackedFiles = value;
            }
        }
        protected IEnumerable<DBFile> DbFiles {
            get {
                List<DBFile> result = new List<DBFile>();
                foreach(PackedFile packed in PackedFiles) {
                    try {
                        result.Add(PackedFileDbCodec.Decode(packed));
                    } catch (Exception) {
                    }
                }
                return result;
            }
        }
        
        protected List<string> tables = new List<string>();
        public abstract void Execute();

        protected void ParseTables(string parse) {
            foreach (string table in parse.Split(',')) {
                if (!string.IsNullOrEmpty(table.Trim())) {
                    tables.Add (table.Trim());
                }
            }
        }
    }
    
    /*
     * Subclass only working on certain fields within a table.
     */
    public abstract class FieldCommand : SqlCommand {
        private List<string> fields = new List<string>();
        public List<string> Fields { 
            get { 
                return fields;
            }
            set { 
                fields.Clear();
                fields.AddRange(value);
            }
        }
        protected bool AllFields {
            get {
                return fields.Count == 1 && fields[0].Equals("*");
            }
        }
        protected void ParseFields(string parse) {
            foreach (string field in parse.Split(',')) {
                fields.Add(field.Trim());
            }
        }
    }
}

