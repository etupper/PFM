using Filetypes;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbSql {
    using QueryResult = List<FieldInstance>;

    class WhereClause {
        public static Regex WHERE_RE = new Regex("where (.*)");
        private string fieldName;
        private Regex matchRe;
        public WhereClause(string toParse) {
            string[] split = WHERE_RE.Match(toParse).Groups[1].Value.Split('=');
            fieldName = split[0].Trim();
            matchRe = new Regex(split[1]);
        }
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

