using Common;
using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    using QueryResult = List<FieldInstance>;
    /*
     * Retrieve data from a table; can include a where clause to limit the rows to output.
     */
    public class SelectCommand : FieldCommand {
        // form of the select statement
        public static Regex SELECT_RE = new Regex("select (.*) from (.*)( *where .*)?", RegexOptions.RightToLeft);
        
        private WhereClause whereClause;
  
        /*
         * Parse given string to create select command.
         */
        public SelectCommand(string toParse) {
            Match match = SELECT_RE.Match(toParse);
            ParseFields (match.Groups[1].Value);
            ParseTables (match.Groups[2].Value);
            if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value)) {
                whereClause = new WhereClause(match.Groups[3].Value);
            }
        }
  
        /*
         * Output the selected fields from the table from rows matching the given where clause,
         * or from all rows if none was given.
         */
        public override void Execute() {
            // Console.WriteLine("selecting {0} from {1}", string.Join(",", Fields), string.Join(",", tables));
            List<QueryResult> result = new List<QueryResult>();
            foreach(DBFile db in DbFiles) {
                foreach(QueryResult row in db.Entries) {
                    if (whereClause != null && !whereClause.Accept(row)) {
                        continue;
                    }
                    if (AllFields) {
                        result.Add(row);
                    } else {
                        QueryResult rowResult = new QueryResult();
                        foreach(FieldInstance instance in row) {
                            if (Fields.Contains(instance.Info.Name)) {
                                rowResult.Add(instance);
                            }
                        }
                        result.Add(rowResult);
                    }
                }
            }
            foreach(QueryResult r in result) {
                Console.WriteLine(string.Join(",", r));
            }
        }
    }
}

