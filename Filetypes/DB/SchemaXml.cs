using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Filetypes {
    using GuidList = List<GuidTypeInfo>;
    using FieldInfoList = List<FieldInfo>;
    
    public class XmlImporter {
        // table to contained fields
        public SortedDictionary<string, FieldInfoList> descriptions = new SortedDictionary<string, FieldInfoList>();
        public SortedDictionary<string, FieldInfoList> Descriptions {
            get {
                return descriptions;
            }
        }
        public SortedDictionary<GuidTypeInfo, FieldInfoList> guidToDescriptions = new SortedDictionary<GuidTypeInfo, FieldInfoList>();
        public SortedDictionary<GuidTypeInfo, FieldInfoList> GuidToDescriptions {
            get {
                return guidToDescriptions;
            }
        }
        
        private static string[] GUID_SEPARATOR = { "," };
        
        public static readonly string GUID_TAG = "guid";

        TextReader reader;
        public XmlImporter (Stream stream) {
            reader = new StreamReader (stream);
        }

        static string UnifyName(string name) {
            return name.ToLower ().Replace (" ", "_");
        }

        public void Import(bool unify = false) {
			XmlDocument doc = new XmlDocument ();
			doc.Load (reader);
			foreach (XmlNode node in doc.ChildNodes) {
				foreach (XmlNode tableNode in node.ChildNodes) {
                    string id;
                    FieldInfoList fields = new FieldInfoList ();
                    bool verifyEquality = false;
                    
                    XmlAttribute attribute = tableNode.Attributes["name"];
                    if (attribute != null) {
                        // pre-GUID table
                        id = attribute.Value;
                        if (unify) {
                            id = UnifyName (id);
                        }
                        Descriptions.Add(id, fields);
                    } else {
                        // table with GUIDs
                        id = tableNode.Attributes[GUID_TAG].Value.Trim();
                        string table_name = tableNode.Attributes["table_name"].Value.Trim();
                        string table_version = tableNode.Attributes["table_version"].Value.Trim();
                        string[] guids = id.Split(GUID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                        // create separate entry for each GUID
                        foreach(string s in guids) {
                            string guid = s.Trim();
                            GuidTypeInfo info = new GuidTypeInfo(guid, table_name, int.Parse(table_version));
                            if (!GuidToDescriptions.ContainsKey(info)) {
                                GuidToDescriptions.Add(info, fields);
                            } else {
                                verifyEquality = true;
                            }
                        }
                    }

                    FillFieldList(fields, tableNode.ChildNodes, unify);

                    // defensive code block.
                    // used when the guid part was first introduced to make sure that
                    // guids didn't get shared across games
                    if (verifyEquality) {
                        VerifyEquality(tableNode, fields);
                    }

                    verifyEquality = false;
				}
			}
		}
        
        void FillFieldList(FieldInfoList fields, XmlNodeList nodes, bool unify = false) {
            // add all fields
            foreach(XmlNode fieldNode in nodes) {
                FieldInfo field = FromNode (fieldNode, unify);
                if (unify) {
                    field.Name = UnifyName (field.Name);
                }
                fields.Add (field);
            }
        }
        
        /* 
         * Collect the given node's attributes and create a field from them. 
         */
        FieldInfo FromNode(XmlNode fieldNode, bool unify) {
			XmlAttributeCollection attributes = fieldNode.Attributes;
			string name = attributes ["name"].Value;
			string type = attributes ["type"].Value;
			FieldInfo description = Types.FromTypeName (type);
			description.Name = name;
			if (attributes ["fkey"] != null) {
				string reference = attributes ["fkey"].Value;
				if (unify) {
					reference = UnifyName (reference);
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
            
            ListType list = description as ListType;
            if (list != null) {
                FillFieldList(list.Infos, fieldNode.ChildNodes, unify);
            }
                
			return description;
		}

        void VerifyEquality(XmlNode tableNode, FieldInfoList fields) {
            string id = tableNode.Attributes[GUID_TAG].Value.Trim();
            string table_name = tableNode.Attributes["table_name"].Value.Trim();
            string table_version = tableNode.Attributes["table_version"].Value.Trim();
            string[] guids = id.Split(GUID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            foreach(string s in guids) {
                string guid = s.Trim();
                GuidTypeInfo info = new GuidTypeInfo(guid, table_name, int.Parse(table_version));
                FieldInfoList existing = GuidToDescriptions[info];
                if (!Enumerable.SequenceEqual<FieldInfo>(fields, existing)) {
                    Console.WriteLine("{0} was:", info.Guid);
                    existing.ForEach(f => Console.WriteLine(f));
                    Console.WriteLine("is:");
                    fields.ForEach(f => Console.WriteLine(f));
                    throw new InvalidDataException();
                }
            }
        }
    }

    public class XmlExporter {
        TextWriter writer;
        
        public bool LogWriting {
            get; set;
        }
        
        public XmlExporter (Stream stream) {
			writer = new StreamWriter (stream);
        }
        
        public void Export() {
            Export(DBTypeMap.Instance.TypeMap, DBTypeMap.Instance.GuidMap);
        }

        public void Export(SortedDictionary<string, FieldInfoList> nameMap, SortedDictionary<GuidTypeInfo, FieldInfoList> guidMap) {
			writer.WriteLine ("<schema>");
            WriteTables(nameMap, new VersionedTableInfoFormatter());
            SortedDictionary<GuidList, FieldInfoList> cleaned = CompileSameDefinitions(guidMap);
            WriteTables(cleaned, new GuidTableInfoFormatter());
			writer.WriteLine ("</schema>");
			writer.Close ();
		}
        
        private void WriteTables<T>(SortedDictionary<T, FieldInfoList> tableDescriptions, TableInfoFormatter<T> formatter) {
            foreach (T name in tableDescriptions.Keys) {
                FieldInfoList descriptions = tableDescriptions[name];
                WriteTable(name, descriptions, formatter);
            }
        }
        
        void WriteTable<T>(T id, FieldInfoList descriptions, TableInfoFormatter<T> format) {
            if (LogWriting) {
                Console.WriteLine ("writing table {0}", id);
            }
			writer.WriteLine (format.FormatHeader(id));
            WriteFieldInfos (descriptions);
			writer.WriteLine ("  </table>");
            writer.Flush();
		}
        
        void WriteFieldInfos(FieldInfoList descriptions, int indent = 4) {
            foreach (FieldInfo description in descriptions) {
                StringBuilder builder = new StringBuilder(new string(' ', indent));
                builder.Append("<field ");
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
                if (description.TypeCode == TypeCode.Object) {
                    builder.Append(">");
                    writer.WriteLine(builder.ToString());
                    
                    // write contained elements
                    ListType type = description as ListType;
                    WriteFieldInfos(type.Infos, indent + 2);
                    
                    // end list tag
                    builder.Clear();
                    builder.Append(new string(' ', indent));
                    builder.Append("</field>");
                } else {
                    builder.Append ("/>");
                }
                writer.WriteLine (builder.ToString ());
            }
        }

        /*
         * Collect all GUIDs with the same type name and definition structure to store them in a single entry.
         */
        private SortedDictionary<GuidList, FieldInfoList> CompileSameDefinitions(SortedDictionary<GuidTypeInfo, FieldInfoList> guidMap) {
            SortedDictionary<GuidList, FieldInfoList> result = 
                new SortedDictionary<GuidList, FieldInfoList>(GuidListComparer.Instance);
            foreach(GuidTypeInfo guid in guidMap.Keys) {
                GuidList addTo = new GuidList();
                FieldInfoList typeDef = guidMap[guid];

                foreach(GuidList guids in result.Keys) {
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
        
  
        /*
         * Create string from single definition entry.
         */
        public static string TableToString(GuidTypeInfo guid, FieldInfoList description) {
            string result = "";
            using (var stream = new MemoryStream()) {
                XmlExporter exporter = new XmlExporter(stream);
                if (string.IsNullOrEmpty(guid.Guid)) {
                    exporter.WriteTable(guid.TypeName, description, new VersionedTableInfoFormatter());
                } else {
                    GuidList info = new GuidList();
                    info.Add(guid);
                    exporter.WriteTable(info, description, new GuidTableInfoFormatter());
                }
                stream.Position = 0;
                result = new StreamReader(stream).ReadToEnd();
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
    /*
     * Formats header without GUID; table name only.
     */
    class VersionedTableInfoFormatter : TableInfoFormatter<string> {
        public override string FormatHeader(string type) {
            return string.Format("  <table name='{0}'>", type); 
        }
    }
    /*
     * Formats header with tablename/version and list of applicable GUIDs.
     */
    class GuidTableInfoFormatter : TableInfoFormatter<GuidList> {
        static string HEADER_FORMAT = "  <table table_name='{1}'" + Environment.NewLine +
            "         table_version='{2}'" + Environment.NewLine +
            "         " + XmlImporter.GUID_TAG +"='{0}'>";
        static string GUID_SEPARATOR = string.Format(",{0}               ", Environment.NewLine);

        public override string FormatHeader(GuidList info) {
            List<string> guids = new List<string>(info.Count);
            info.ForEach(i => { guids.Add(i.Guid); });
            return string.Format(HEADER_FORMAT, string.Join(GUID_SEPARATOR, guids), info[0].TypeName, info[0].Version);
        }
    }
    #endregion
    
    public class GuidListComparer : Comparer<GuidList> {
        public static readonly GuidListComparer Instance = new GuidListComparer();
        
        public override int Compare(GuidList x, GuidList y) {
            if (x.Count == 0) {
                return y.Count == 0 ? 0 : 1;
            } else  if (y.Count == 0) {
                return -1;
            }
            return x[0].CompareTo(y[0]);
        }
    }
}

