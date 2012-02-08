using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace DecodeTool {       
    public class Util {
		static String notAllowed = @"[^a-zA-Z0-9\s\.\\\-_/]";
		
        public static String formatHex(byte[] bytes) {
			if (bytes.Length == 0)
				return "";
			StringBuilder result = new StringBuilder (3 * bytes.Length);
			result.Append (string.Format ("{0:x2}", bytes [0]));
			for (int i = 1; i < bytes.Length; i++) {
				result.Append (string.Format (" {0:x2}", bytes[i]));
			}
			return result.ToString ();
		}

        public static string decodeSafe(TypeDescription d, BinaryReader reader) {
            int ignored;
            return decodeSafe(d, reader, out ignored);
        }
        public static string decodeSafe(TypeDescription d, BinaryReader reader, out int length) {
			string result = "failure";
			length = 0;
			try {
				result = d.Decode (reader);
				length = d.GetLength (result);
				//result = Regex.Replace (result, notAllowed, "?");
			} catch (Exception x) {
				result = x.Message.Replace ("\n", "-");
			}
			return result;
		}

        public static string ToString(string name, TypeDescription description) {
			string result;
			if (description == Types.OptStringType) {
				result = string.Format ("->,Boolean,1;{0},String", name);
			} else if (description is VarBytesDescription) {
				result = string.Format ("{0},{1}", name, (description as VarBytesDescription).GetLength(""));
			} else {
				result = string.Format ("{0},{1}", name, description.TypeName);
			}
			return result;
		}

        public static List<TypeDescription> Convert(TypeInfo info, ref List<String> names) {
            bool nextOptional = false;
            names.Clear();
            List<TypeDescription> descriptions = new List<TypeDescription>();
            foreach (FieldInfo field in info.fields) {
                TypeDescription description;
                if (field.Mod == FieldInfo.Modifier.NextFieldIsConditional) {
                    nextOptional = true;
                    continue;
                } else if (nextOptional) {
                    description = Types.OptStringType;
                } else {
                    description = Convert(field);
                }
                nextOptional = false;
                names.Add(field.Name);
                descriptions.Add(description);
            }
            return descriptions;
        }

        public static TypeDescription Convert(FieldInfo info) {
			switch (info.Type) {
			case PackTypeCode.Boolean:
				return Types.BoolType;
			case PackTypeCode.UInt16:
				return Types.ShortType;
			case PackTypeCode.Int32:
			case PackTypeCode.UInt32:
				return Types.IntType;
			case PackTypeCode.Single:
				return Types.SingleType;
			case PackTypeCode.String:
				return Types.StringType;
			case PackTypeCode.Empty:
				return new VarBytesDescription (info.Length);
			}
			throw new InvalidDataException ("unknown type");
		}
    }
}
