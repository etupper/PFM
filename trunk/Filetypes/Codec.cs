﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Common {
    public interface Codec<T> {
        T Decode(Stream file);
        void Encode(Stream stream, T toEncode);
    }
	
	// utilities for csv export
	public class CsvUtil {
		static string format = "\"{0}\"";

		public static string Format(string input) {
			return string.Format (format, Regex.Escape (input));
		}

		public static string Unformat(string formatted) {
			string result = Regex.Unescape (formatted);
			if (result.StartsWith ("\"")) {
				// remove one leading and trailing quote if present
				result = result.Substring (1, result.Length - 2);
			}
			return result;
		}
	}
}