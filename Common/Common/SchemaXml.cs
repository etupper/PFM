using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace Common {
    public class XmlImporter {
        // table to contained fields
        public SortedDictionary<string, List<FieldInfo>> descriptions = new SortedDictionary<string, List<FieldInfo>>();

        TextReader reader;
        public XmlImporter (Stream stream) {
            reader = new StreamReader (stream);
        }

        static string unifyName(string name) {
            return name.ToLower ().Replace (" ", "_");
        }

        public void import(bool unify = false) {
			XmlDocument doc = new XmlDocument ();
			doc.Load (reader);
			foreach (XmlNode node in doc.ChildNodes) {
				foreach (XmlNode tableNode in node.ChildNodes) {
					string tableName = tableNode.Attributes ["name"].Value.Replace("_tables", "");
					if (unify) {
						tableName = unifyName (tableName);
					}
					List<FieldInfo> fields = new List<FieldInfo> ();
					for (int i = 0; i < tableNode.ChildNodes.Count; i++) {
						XmlNode fieldNode = tableNode.ChildNodes [i];
						FieldInfo field = fromNode (fieldNode, unify);
						if (unify) {
							field.Name = unifyName (field.Name);
						}
						fields.Add (field);
					}
					descriptions.Add (tableName, fields);
				}
			}
		}
        public string fieldName(string table, int index) {
            return descriptions [table] [index].Name;
        }

        FieldInfo fromNode(XmlNode fieldNode, bool unify) {
			XmlAttributeCollection attributes = fieldNode.Attributes;
			string name = attributes ["name"].Value;
			string type = attributes ["type"].Value;
			FieldInfo description = Types.FromTypeName (type);
			description.Name = name;
			if (attributes ["fkey"] != null) {
				string reference = attributes ["fkey"].Value;
				if (unify) {
					reference = unifyName (reference);
				}
				description.ForeignReference = reference;
			}
			if (attributes ["version_start"] != null) {
				description.StartVersion = int.Parse (attributes ["version_start"].Value);
			}
			if (attributes ["version_end"] != null) {
				description.LastVersion = int.Parse (attributes ["version_end"].Value);
			}
			if (attributes ["pk"] != null) {
				description.PrimaryKey = true;
			}
			return description;
		}
    }

    public class XmlExporter {
        // table to (field to fkey)
        public static Dictionary<string, Dictionary<string, string>> fkeys = new Dictionary<string, Dictionary<string, string>> ();
        TextWriter writer;
        //int currentIndent = 0;
        public XmlExporter (Stream stream) {
			writer = new StreamWriter (stream);
        }

        public void export(SortedDictionary<string, List<FieldInfo>> tableDescriptions) {
			writer.WriteLine ("<schema>");
			List<string> sorted = new List<string> (tableDescriptions.Keys);
			sorted.Sort ();
			foreach (string tableName in sorted) {
				writeTable (tableName, tableDescriptions [tableName]);
			}
			writer.WriteLine ("</schema>");
			writer.Close ();
		}

        public void writeTable(string name, List<FieldInfo> descriptions) {
			Console.WriteLine ("writing table {0}", name);
			writer.WriteLine ("  <table name='{0}_tables'>", name);
			foreach (FieldInfo description in descriptions) {
				StringBuilder builder = new StringBuilder ("    <field ");
				if (!description.ForeignReference.Equals ("")) {
					builder.Append (string.Format ("fkey='{0}' ", description.ForeignReference));
				}
				builder.Append (string.Format ("name='{0}' ", description.Name));
				builder.Append (string.Format ("type='{0}' ", description.TypeName));
				if (description.PrimaryKey) {
					builder.Append ("pk='true' ");
				}
				if (description.StartVersion != 0) {
					builder.Append ("version_start='" + description.StartVersion + "' ");
				}
				if (description.LastVersion < int.MaxValue) {
					builder.Append ("version_end='" + description.LastVersion + "' ");
				}
				builder.Append ("/>");
				writer.WriteLine (builder.ToString ());
			}
			writer.WriteLine ("  </table>");
            writer.Flush();
		}

        public static string tableToString(string name, List<FieldInfo> description) {
            string result = "";
            using (StreamReader reader = new StreamReader(new MemoryStream())) {
                new XmlExporter(reader.BaseStream).writeTable(name, description);
                reader.BaseStream.Position = 0;
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}

