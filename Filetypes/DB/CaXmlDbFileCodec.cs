using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace Filetypes {
    public class CaXmlDbFileCodec : Codec<DBFile> {
        private string xmlPath;
        
        static Dictionary<string, TypeInfo> allInfos = new Dictionary<string, TypeInfo>();
        
        public CaXmlDbFileCodec(string path) {
            xmlPath = path;
        }
        
        public TypeInfo TypeInfoByTableName(string tablename) {
            TypeInfo info = null;
            if (!allInfos.ContainsKey(tablename)) {
                allInfos[tablename] = LoadTypeInfos(tablename);
            }
            allInfos.TryGetValue(tablename, out info);
            return info;
        }
        
        public DBFile Decode(Stream stream) {
            DBFile result = null;
            using (TextReader reader = new StreamReader(stream)) {
                XmlDocument doc = new XmlDocument ();
                doc.Load (reader);
                
                foreach(XmlNode dataroot in doc.ChildNodes) {
                    string guid = "";
                    foreach(XmlNode entry in dataroot.ChildNodes) {
                        if ("edit_uuid".Equals(entry.Name)) {
                            guid = entry.InnerText;
                            continue;
                        }
                        
                        string recordName = entry.Name;
                        TypeInfo typeinfo;
                        if (!allInfos.TryGetValue(recordName, out typeinfo)) {
                            typeinfo = LoadTypeInfos(recordName);
                            allInfos[recordName] = typeinfo;
                        }
                        
                        if (result == null) {
                            DBFileHeader header = new DBFileHeader(guid, 0, 0, false);
                            result = new DBFile(header, typeinfo);
                        }

                        Dictionary<string, string> fieldValues = new Dictionary<string, string>();
                        List<string> requireTranslation = new List<string>();
                        foreach(XmlNode row in entry.ChildNodes) {
                            fieldValues[row.Name] = row.InnerText;
                            XmlAttribute at = row.Attributes["requires_translation"];
                            if (at != null && "true".Equals(at.Value)) {
                                requireTranslation.Add(row.Name);
                            }
                        }
                        List<FieldInstance> fields = result.GetNewEntry();
                        foreach(FieldInstance field in fields) {
                            string val;
                            try {
                                if (fieldValues.TryGetValue(field.Name, out val)) {
                                    if (field.Info.TypeName.Equals("boolean")) {
                                        field.Value = "1".Equals(val) ? "true" : "false";
                                    } else {
                                        field.Value = val;
                                    }
                                    field.RequiresTranslation = requireTranslation.Contains(field.Name);
                                }
                            } catch (Exception e) {
                                Console.WriteLine("Wait a minute!");
                                throw e;
                            }
                        }
                        result.Entries.Add(fields);
                    }
                }
            }
            return result;
        }

        private FieldInfo InfoByName(List<FieldInfo> infos, string fieldName) {
            FieldInfo result = null;
            foreach(FieldInfo info in infos) {
                if (info.Name.Equals(fieldName)) {
                    result = info;
                    break;
                }
            }
            return result;
        }
        
        private TypeInfo LoadTypeInfos(string name) {
            string twadFilename = string.Format("TWaD_{0}.xml", name.Replace("_tables", ""));
            string twadPath = Path.Combine(xmlPath, twadFilename);
            if (!File.Exists(twadPath)) {
                return null;
            }
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            string guid = "";
            using (var reader = File.OpenText(twadPath)) {
                XmlDocument defDoc = new XmlDocument();
                defDoc.Load(reader);
                foreach(XmlNode root in defDoc.ChildNodes) {
                    foreach(XmlNode fieldNode in root.ChildNodes) {
                        if ("edit_uuid".Equals(fieldNode.Name)) {
                            guid = fieldNode.InnerText;
                        } else {
                            fieldInfos.Add(CreateInfoFromNode(fieldNode));
                        }
                    }
                }
            }
            TypeInfo typeInfo = new TypeInfo(fieldInfos) {
                Name = name
            };
            typeInfo.ApplicableGuids.Add(guid);
            return typeInfo;
        }
        
        private FieldInfo CreateInfoFromNode(XmlNode node) {
            bool optional = "0".Equals(node["required"].InnerText);
            XmlNode typeNode = node["field_type"];
            string typeText = typeNode.InnerXml;
            if ("text".Equals(typeText)) {
                typeText = string.Format("{0}string_ascii", (optional ? "opt" : ""));
            }
            FieldInfo info = Types.FromTypeName(typeText);
            info.Optional = optional;
            info.Name = node["name"].InnerText;
            info.PrimaryKey = "1".Equals(node["primary_key"].InnerText);
            XmlNode refTableNode = node["column_source_table"];
            if (refTableNode != null) {
                string refTable = refTableNode.InnerText;
                string refColumn = node["column_source_column"].InnerText;
                // Console.WriteLine("reference found: {0}:{1}", string.Format("{0}_tables", refTable), refColumn);
                info.FieldReference = new FieldReference(string.Format("{0}_tables", refTable), refColumn);
            }
            return info;
        }

        public void Encode(Stream stream, DBFile dbFile) {
        }
    }
}

