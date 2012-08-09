using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Common {
    public class XmlImporter {
        // table to contained fields
        public SortedDictionary<string, List<FieldInfo>> descriptions = new SortedDictionary<string, List<FieldInfo>>();
        public SortedDictionary<GuidTypeInfo, List<FieldInfo>> guidToDescriptions = new SortedDictionary<GuidTypeInfo, List<FieldInfo>>();
        
        private static string[] GUID_SEPARATOR = { "," };
        
        public static readonly string GUID_TAG = "guid";

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
                    string id;
                    List<FieldInfo> fields = new List<FieldInfo> ();
                    bool verifyEquality = false;
                    
                    XmlAttribute attribute = tableNode.Attributes["name"];
                    if (attribute != null) {
                        id = attribute.Value.Replace("_tables", "");
                        if (unify) {
                            id = unifyName (id);
                        }
                        descriptions.Add(id, fields);
                    } else {
                        id = tableNode.Attributes[GUID_TAG].Value.Trim();
                        string[] guids = id.Split(GUID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                        foreach(string s in guids) {
                            string guid = s.Trim();
                            string table_name = tableNode.Attributes["table_name"].Value.Trim();
                            string table_version = tableNode.Attributes["table_version"].Value.Trim();
                            GuidTypeInfo info = new GuidTypeInfo(guid, table_name, int.Parse(table_version));
                            if (!guidToDescriptions.ContainsKey(info)) {
                                guidToDescriptions.Add(info, fields);
                            } else {
                                verifyEquality = true;
                            }
                        }
                    }

					for (int i = 0; i < tableNode.ChildNodes.Count; i++) {
						XmlNode fieldNode = tableNode.ChildNodes [i];
						FieldInfo field = FromNode (fieldNode, unify);
						if (unify) {
							field.Name = unifyName (field.Name);
						}
						fields.Add (field);
					}

                    // defensive code block.
                    // used when the guid part was first introduced to make sure that
                    // guids didn't get shared across games
                    if (verifyEquality) {
                        id = tableNode.Attributes[GUID_TAG].Value.Trim();
                        string table_name = tableNode.Attributes["table_name"].Value.Trim();
                        string table_version = tableNode.Attributes["table_version"].Value.Trim();
                        string[] guids = id.Split(GUID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                        foreach(string s in guids) {
                            string guid = s.Trim();
                            GuidTypeInfo info = new GuidTypeInfo(guid, table_name, int.Parse(table_version));
                            List<FieldInfo> existing = guidToDescriptions[info];
                            if (!Enumerable.SequenceEqual<FieldInfo>(fields, existing)) {
                                Console.WriteLine("{0} was:", info.Guid);
                                existing.ForEach(f => Console.WriteLine(f));
                                Console.WriteLine("is:");
                                fields.ForEach(f => Console.WriteLine(f));
                                throw new InvalidDataException();
                            }
                        }
                    }

                    verifyEquality = false;
				}
			}
		}

        FieldInfo FromNode(XmlNode fieldNode, bool unify) {
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
        
        public bool LogWriting {
            get; set;
        }
        
        //int currentIndent = 0;
        public XmlExporter (Stream stream) {
			writer = new StreamWriter (stream);
        }

        public void export(SortedDictionary<string, List<FieldInfo>> nameMap, SortedDictionary<GuidTypeInfo, List<FieldInfo>> guidMap) {
			writer.WriteLine ("<schema>");
            writeTables(nameMap, new VersionedTableInfoFormatter());
            SortedDictionary<List<GuidTypeInfo>, List<FieldInfo>> cleaned = CompileSameDefinitions(guidMap);
            writeTables(cleaned, new GuidTableInfoFormatter());
			writer.WriteLine ("</schema>");
			writer.Close ();
		}
        
        private SortedDictionary<List<GuidTypeInfo>, List<FieldInfo>> CompileSameDefinitions(SortedDictionary<GuidTypeInfo, List<FieldInfo>> guidMap) {
            SortedDictionary<List<GuidTypeInfo>, List<FieldInfo>> result = 
                new SortedDictionary<List<GuidTypeInfo>, List<FieldInfo>>(GuidListComparer.Instance);
            foreach(GuidTypeInfo guid in guidMap.Keys) {
                List<GuidTypeInfo> addTo = new List<GuidTypeInfo>();
                List<FieldInfo> typeDef = guidMap[guid];

                foreach(List<GuidTypeInfo> guids in result.Keys) {
                    if (!guids[0].TypeName.Equals(guid.TypeName)) {
                        continue;
                    }
                    if (Enumerable.SequenceEqual<FieldInfo>(typeDef, result[guids])) {
                        addTo = guids;
                        break;
                    }
                }

                addTo.Add (guid);
                addTo.Sort();
                result[addTo] = typeDef;
            }
            return result;
        }
        
        private void writeTables<T>(SortedDictionary<T, List<FieldInfo>> tableDescriptions, TableInfoFormatter<T> formatter) {
            foreach (T name in tableDescriptions.Keys) {
                List<FieldInfo> descriptions = tableDescriptions[name];
                writeTable(name, descriptions, formatter);
            }
        }
        
        void writeTable<T>(T id, List<FieldInfo> descriptions, TableInfoFormatter<T> format) {
            if (LogWriting) {
                Console.WriteLine ("writing table {0}", id);
            }
			writer.WriteLine (format.FormatHeader(id));
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

        public static string tableToString(string name, GuidTypeInfo guid, List<FieldInfo> description) {
            string result = "";
            using (StreamReader reader = new StreamReader(new MemoryStream())) {
                XmlExporter exporter = new XmlExporter(reader.BaseStream);
                exporter.writeTable(name, description, new VersionedTableInfoFormatter());
                List<GuidTypeInfo> info = new List<GuidTypeInfo>();
                info.Add(guid);
                exporter.writeTable(info, description, new GuidTableInfoFormatter());
                reader.BaseStream.Position = 0;
                result = reader.ReadToEnd();
            }
            return result;
        }
    }

    #region Formatting
    abstract class TableInfoFormatter<T> {
        public abstract string FormatHeader(T toWrite);
        public string FormatField(FieldInfo description) {
            StringBuilder builder = new StringBuilder ("    <field ");
            builder.Append(FormatFieldContent(description));
            builder.Append ("/>");
            return builder.ToString();
        }
        public virtual string FormatFieldContent(FieldInfo description) {
            StringBuilder builder = new StringBuilder();
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
            return builder.ToString();
        }
    }
    class VersionedTableInfoFormatter : TableInfoFormatter<string> {
        public override string FormatHeader(string type) {
            return string.Format("  <table name='{0}_tables'>", type); 
        }
    }
    class GuidTableInfoFormatter : TableInfoFormatter<List<GuidTypeInfo>> {
        static string HEADER_FORMAT = "  <table table_name='{1}'\n" +
            "         table_version='{2}'\n" +
            "         " + XmlImporter.GUID_TAG +"='{0}'>";
        static string GUID_SEPARATOR = ",\n               ";
        public override string FormatHeader(List<GuidTypeInfo> info) {
            List<string> guidList = new List<string>(info.Count);
            info.ForEach(i => { guidList.Add(i.Guid); });
            return string.Format(HEADER_FORMAT, string.Join(GUID_SEPARATOR, guidList), info[0].TypeName, info[0].Version);
        }
    }
    #endregion
    
    public class GuidListComparer : Comparer<List<GuidTypeInfo>> {
        public static readonly GuidListComparer Instance = new GuidListComparer();
        
        public override int Compare(List<GuidTypeInfo> x, List<GuidTypeInfo> y) {
            if (x.Count == 0) {
                return y.Count == 0 ? 0 : 1;
            } else  if (y.Count == 0) {
                return -1;
            }
            return x[0].CompareTo(y[0]);
        }
    }
}

