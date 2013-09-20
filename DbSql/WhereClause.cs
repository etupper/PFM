using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    using QueryResult = List<FieldInstance>;
 
    /*
     * A class filtering out db rows by placing conditions on them.
     */
    public class WhereClause {
        // form  of the where clause
        public static Regex WHERE_RE = new Regex("where (.*)");
        
        // the field to match
        private string fieldName;
        // the RE to match against
        private Regex matchRe;
        
        /*
         * Parse given string to create where clause.
         */
        public WhereClause(string toParse) {
            string[] split = WHERE_RE.Match(toParse).Groups[1].Value.Split('=');
            fieldName = split[0].Trim();
            matchRe = new Regex(split[1]);
        }
        /*
         * Query if the given row matches this where clause.
         */
        public bool Accept(QueryResult row) {
            bool result = true;
            foreach(FieldInstance instance in row) {
                if (instance.Info.Name.Equals(fieldName)) {
                    result = matchRe.IsMatch(instance.Value);
                    break;
                }
            }
            return result;
        }
    }
}

