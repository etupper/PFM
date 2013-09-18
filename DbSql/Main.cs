using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Common;
using Filetypes;


namespace DbSql {
    using QueryResult = List<FieldInstance>;

    class MainClass {

        public static void Main(string[] args) {
            SqlCommand command = null;
            PackFile pack = null;
            string sql = null;
            foreach(string arg in args) {
                if (arg.StartsWith("-p")) {
                    pack = new PackFileCodec().Open(arg.Substring(2));
                } else if (arg.StartsWith("-tm")) {
                    DBTypeMap.Instance.initializeFromFile(arg.Substring(3));
                } else if (arg.StartsWith("-s")) {
                    sql = arg.Substring(2);
                    if (SelectCommand.SELECT_RE.IsMatch(sql)) {
                        command = new SelectCommand(sql);
                    } else if (InsertCommand.INSERT_RE.IsMatch(sql)) {
                        command = new InsertCommand(sql) {
                            ToSave = pack
                        };
                    } else if (UpdateCommand.UPDATE_RE.IsMatch(sql)) {
                        command = new UpdateCommand(sql) {
                            ToSave = pack
                        };
                    } else if (DeleteCommand.DELETE_RE.IsMatch(sql)) {
                        command = new DeleteCommand(sql) {
                            ToSave = pack
                        };
                    } else if (HelpCommand.HELP_RE.IsMatch(sql)) {
                        command = new HelpCommand(sql);
                    }
                }
            }
            if (pack != null && command != null) {
                command.PackedFiles = pack;
                command.Execute();
                command.Commit();
            }
        }
    }
    
    public abstract class SqlCommand {
        public virtual void Commit() {}
        protected bool AllTables { get; set; }
        
        private IEnumerable<PackedFile> allPackedFiles;
        public IEnumerable<PackedFile> PackedFiles { 
            protected get {
                List<PackedFile> filtered = new List<PackedFile>();
                if (allPackedFiles == null) {
                    return filtered;
                }
                foreach(PackedFile file in allPackedFiles) {
                    if (!file.FullPath.StartsWith("db")) {
                        continue;
                    }
                    string tableType = DBFile.typename(file.FullPath);
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
        /*public List<string> Tables { 
            get { 
                return tables;
            }
            set { 
                tables.Clear();
                tables.AddRange(value);
            }
        }*/
        public abstract void Execute();

        protected void ParseTables(string parse) {
            foreach (string table in parse.Split(',')) {
                if (!string.IsNullOrEmpty(table.Trim())) {
                    tables.Add (table.Trim());
                }
            }
        }
    }
    
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
