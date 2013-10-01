using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DBTableControl
{
    public static class DBUtil
    {
        public static bool isMatch(string input, string pattern, MatchType option = MatchType.Simple)
        {
            // Simple text match path.
            if (option == MatchType.Simple)
            {
                if (input.Equals(pattern))
                {
                    return true;
                }
            }

            // Wildcard match path.
            if (option == MatchType.Wildcard)
            {

            }

            // Regex match path.
            if (option == MatchType.Regex)
            {
                Regex matchregex = new Regex(pattern);
                return matchregex.IsMatch(input);
            }

            return false;
        }

        public enum MatchType
        {
            Simple,
            Wildcard,
            Regex
        }
    }
}
