using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Common;
using Filetypes;


namespace DbSql {
    using QueryResult = List<FieldInstance>;

    class MainClass {

        public static void Main(string[] args) {
            Script script = new Script();
            foreach(string arg in args) {
                if (arg.StartsWith("-p")) {
                    script.Pack = new PackFileCodec().Open(arg.Substring(2));
                } else if (arg.StartsWith("-tm")) {
                    script.TypeMapFile = arg.Substring(3);
                } else if (arg.StartsWith("-s")) {
                    script.ExecuteLine(arg.Substring(2));
                } else if (arg.StartsWith("-f")) {
                    string file = arg.Substring(2);
                    foreach(string line in File.ReadLines(file)) {
                        script.ExecuteLine(line);
                    }
                }
            }
        }
    }
    
    public class Script {
        public PackFile Pack { 
            get; 
            set; 
        }
        public string TypeMapFile {
            set {
                DBTypeMap.Instance.initializeFromFile(value);
            }
        }
        public void ExecuteLine(string line) {
            if (line.StartsWith("pack")) {
                Pack = new PackFileCodec().Open(line.Substring(5));
            } else if (line.StartsWith("schema")) {
                TypeMapFile = line.Substring(7);
            } else {
                SqlCommand command = ParseCommand(line);
                command.Execute();
                command.Commit();
            }
        }
        public SqlCommand ParseCommand(string sql) {
            SqlCommand command = null;
            if (SelectCommand.SELECT_RE.IsMatch(sql)) {
                command = new SelectCommand(sql) {
                    PackedFiles = Pack
                };
            } else if (InsertCommand.INSERT_RE.IsMatch(sql)) {
                command = new InsertCommand(sql) {
                    ToSave = Pack,
                    PackedFiles = Pack
                };
            } else if (UpdateCommand.UPDATE_RE.IsMatch(sql)) {
                command = new UpdateCommand(sql) {
                    ToSave = Pack,
                    PackedFiles = Pack
                };
            } else if (DeleteCommand.DELETE_RE.IsMatch(sql)) {
                command = new DeleteCommand(sql) {
                    ToSave = Pack,
                    PackedFiles = Pack
                };
            } else if (HelpCommand.HELP_RE.IsMatch(sql)) {
                command = new HelpCommand(sql) {
                    PackedFiles = Pack
                };
            }
            return command;
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
