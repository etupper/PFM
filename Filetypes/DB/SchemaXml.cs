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
    using TypeInfoList = List<TypeInfo>;
    
    public class XmlImporter {
        // table to contained fields
        List<TypeInfo> typeInfos = new List<TypeInfo>();
        public List<TypeInfo> Imported {
            get {
                return typeInfos;
            }
        }
        
        // private static string[] GUID_SEPARATOR = { "," };
        
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
                    int version = 0;
                    FieldInfoList fields = new FieldInfoList ();
                    // bool verifyEquality = false;
                    
                    XmlAttribute attribute = tableNode.Attributes["name"];
                    if (attribute != null) {
                        // pre-GUID table
                        id = attribute.Value;
                        if (unify) {
                            id = UnifyName (id);
                        }
                    } else {
                        // table with GUIDs
                        // id = tableNode.Attributes[GUID_TAG].Value.Trim();
                        id = tableNode.Attributes["table_name"].Value.Trim();
                        string table_version = tableNode.Attributes["table_version"].Value.Trim();
                        version = int.Parse(table_version);
                    }

                    FillFieldList(fields, tableNode.ChildNodes, unify);
                    TypeInfo info = new TypeInfo(fields) {
                        Name = id,
                        Version = version
                    };
                    typeInfos.Add(info);
				}
			}
		}
        
        void FillFieldList(List<FieldInfo> fields, XmlNodeList nodes, bool unify = false) {
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
            FieldInfo description = null;
            try {
    			XmlAttributeCollection attributes = fieldNode.Attributes;
    			string name = attributes ["name"].Value;
    			string type = attributes ["type"].Value;
                
    			description = Types.FromTypeName (type);
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
            } catch (Exception e) {
                Console.WriteLine(e);
                throw e;
            }
			return description;
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
            Export(DBTypeMap.Instance.AllInfos);
        }

        public void Export(List<TypeInfo> infos) {
#if DEBUG
            Console.WriteLine("storing {0} infos", infos.Count);
#endif
			writer.WriteLine ("<schema>");
            // WriteTables(infos, new VersionedTableInfoFormatter());
            List<TypeInfo> cleaned = CompileSameDefinitions(infos);
#if DEBUG
            Console.WriteLine("cleaned: {0}", cleaned.Count);
#endif
            WriteTables(cleaned, new GuidTableInfoFormatter());
			writer.WriteLine ("</schema>");
			writer.Close ();
		}
        
        private void WriteTables(List<TypeInfo> tableDescriptions, TableInfoFormatter<TypeInfo> formatter) {
            foreach (TypeInfo typeInfo in tableDescriptions) {
                WriteTable(typeInfo, formatter);
            }
        }
        
        void WriteTable(TypeInfo id, TableInfoFormatter<TypeInfo> format) {
            if (LogWriting) {
                Console.WriteLine ("writing table {0}", id);
            }
			writer.WriteLine (format.FormatHeader(id));
            WriteFieldInfos (id.Fields);
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
        private List<TypeInfo> CompileSameDefinitions(List<TypeInfo> sourceList) {
            Dictionary<string, List<TypeInfo>> typeMap = new Dictionary<string, List<TypeInfo>>();
            
            foreach(TypeInfo typeInfo in sourceList) {
                if (!typeMap.ContainsKey(typeInfo.Name)) {
                    List<TypeInfo> addTo = new List<TypeInfo>();
                    addTo.Add(typeInfo);
                    typeMap.Add(typeInfo.Name, addTo);
                } else {
                    bool added = false;
                    foreach(TypeInfo existing in typeMap[typeInfo.Name]) {
                        if (Enumerable.SequenceEqual<FieldInfo>(typeInfo.Fields, existing.Fields)) {
                            existing.ApplicableGuids.AddRange(typeInfo.ApplicableGuids);
                            added = true;
                            break;
                        }
                    }
                    if (!added) {
                        typeMap[typeInfo.Name].Add(typeInfo);
                    }
                }
            }
            List<TypeInfo> result = new List<TypeInfo>();
            foreach(List<TypeInfo> infos in typeMap.Values) {
                result.AddRange(infos);
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
                TypeInfo info = new TypeInfo(description) {
                    Name = guid.TypeName,
                    Version = guid.Version
                };
                if (!string.IsNullOrEmpty(guid.Guid)) {
                    info.ApplicableGuids.Add(guid.Guid);
                }
                exporter.WriteTable(info, new GuidTableInfoFormatter());
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
    class GuidTableInfoFormatter : TableInfoFormatter<TypeInfo> {
        static string HEADER_FORMAT = "  <table table_name='{1}'" + Environment.NewLine +
            "         table_version='{2}'" + Environment.NewLine +
            "         " + XmlImporter.GUID_TAG +"='{0}'>";
        static string GUID_SEPARATOR = string.Format(",{0}               ", Environment.NewLine);

        public override string FormatHeader(TypeInfo info) {
            List<string> guids = new List<string>(info.ApplicableGuids.Count);
            info.ApplicableGuids.ForEach(i => { if (!guids.Contains(i)) { guids.Add(i);  }});
            return string.Format(HEADER_FORMAT, string.Join(GUID_SEPARATOR, guids), info.Name, info.Version);
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

