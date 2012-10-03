using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using Common;
using Filetypes;
using PackFileTest.Mapping;

//using ValueList = System.Collections.Generic.List<string>;
//using Table = System.Tuple<string, System.Collections.Generic.List<string>>;
using NameMapping = System.Tuple<string, string>;

namespace PackFileTest {
    /*
     * Finds the field correspondencies between the db file schema and the ones used by CA.
     * Does this by comparing all values within a column, so it depends on
     * the pack file containing the exact same patch level as the xml files.
     * Also, might fail on columns with very few values or where there are several columns
     * in a table which have the exact same values.
     */
    public class FieldCorrespondencyFinder {
        string xmlDirectory;

        Dictionary<string, MappedTable> mappedTables = new Dictionary<string, MappedTable>();
        
        public FieldCorrespondencyFinder (string packFile, string xmlDir) {
            xmlDirectory = xmlDir;
            DBTypeMap.Instance.InitializeTypeMap(Directory.GetCurrentDirectory());
            // initialize patchFileValues from pack file
            PackFile pack = new PackFileCodec().Open(packFile);
            foreach(PackedFile contained in pack.Files) {
                if (contained.FullPath.StartsWith("db")) {
                    // no need to resolve if it's done already...
                    string tableName = DBFile.typename(contained.FullPath).Replace("_tables", "");
                    try {
                        PackedFileDbCodec codec = PackedFileDbCodec.GetCodec(contained);
                        codec.AutoadjustGuid = false;
                        DBFile dbFile = codec.Decode(contained.Data);

                        MappedTable table = new MappedTable(tableName);
                        ValuesFromPack(table, dbFile);
                        ValuesFromXml(table);

                        mappedTables[tableName] = table;
                    } catch (Exception e) {
#if DEBUG
                        Console.Error.WriteLine(e.Message);
#endif
                    }
                }
            }
        }
        
        public void FindAllCorrespondencies() {
            TableNameCorrespondencyManager.Instance.Clear();

            foreach(MappedTable table in mappedTables.Values) {
                FindCorrespondencies(table);

                if (table.IsFullyMapped) {
                    TableNameCorrespondencyManager.Instance.TableMapping.Add(table.TableName, table.MappingAsTuples);
                } else if (!table.IsCompatible) {
                    TableNameCorrespondencyManager.Instance.IncompatibleTables.Add(table.TableName, table.MappingAsTuples);
                } else {
                    TableNameCorrespondencyManager.Instance.PartialTableMapping.Add(table.TableName, table.MappingAsTuples);
                }
                TableNameCorrespondencyManager.Instance.UnmappedPackedFields.Add(table.TableName, table.UnmappedPackFieldNames);
                TableNameCorrespondencyManager.Instance.UnmappedXmlFields.Add(table.TableName, table.UnmappedXmlFieldNames);
                table.IgnoredXmlFieldNames.ForEach(f => TableNameCorrespondencyManager.Instance.AddIgnoredField(table.TableName, f));
                TableNameCorrespondencyManager.Instance.TableGuidMap[table.TableName] = table.Guid;
            }
        }
        
        /*
         * Find the corresponding fields for all columns in the given table.
         */
        void FindCorrespondencies(MappedTable table) {
            
            foreach(string field in table.PackData.Fields) {
                NameMapping mapping = FindCorrespondency(table, field);
                if (mapping != null) {
                    table.AddMapping(mapping.Item1, mapping.Item2);
                }
            }

            if (table.UnmappedPackFieldNames.Count == 1 && table.UnmappedXmlFieldNames.Count == 1) {
                table.AddMapping(table.UnmappedPackFieldNames[0], table.UnmappedXmlFieldNames[0]);
            }
        }
        
        /*
         * In the given list, find the table with all values equal to the given one from the pack.
         */
        NameMapping FindCorrespondency(MappedTable dataTable, string fieldName) {
            NameMapping result = null;
            List<string> values = dataTable.PackData.Values(fieldName);
            List<string> packTableNames = dataTable.PackData.FieldsContainingValues(values);
            List<string> xmlTableNames = dataTable.XmlData.FieldsContainingValues(values);

            if (packTableNames.Count == 1 && xmlTableNames.Count == 1) {
                result = new NameMapping(packTableNames[0], xmlTableNames[0]);
            } else {
                foreach(string xmlField in xmlTableNames) {
                    if (xmlField.Equals(UnifyName(fieldName))) {
                        result = new NameMapping(fieldName, xmlField);
                        break;
                    }
                }
            }
            return result;
        }

        string UnifyName(string packColumnName) {
            return packColumnName.Replace(' ', '_').ToLower();
        }
        
        /*
         * Retrieve the columnname/valuelist collection from the db file of the given type.
         */
        void ValuesFromPack(MappedTable table, DBFile dbFile) {
            foreach(List<FieldInstance> row in dbFile.Entries) {
                foreach(FieldInstance field in row) {
                    table.PackData.AddFieldValue(field.Name, field.Value);
                }
            }
        }
  
        /*
         * Retrieve the columnname/valuelist collection from the table with the given name.
         */
        void ValuesFromXml(MappedTable fill) {
            XmlDocument tableDocument = new XmlDocument();
            string xmlFile = Path.Combine(xmlDirectory, string.Format("{0}.xml", fill.TableName));
            List<CaFieldInfo> infos = CaFieldInfo.ReadInfo(xmlDirectory, fill.TableName);
            infos.ForEach(i => {
                if (i.Ignored && !fill.IgnoredXmlFieldNames.Contains(i.Name)) {
                    fill.IgnoredXmlFields.Add(i.Name);
                }
            });

            if (File.Exists(xmlFile)) {
                tableDocument.Load(xmlFile);
                foreach(XmlNode node in tableDocument.ChildNodes[1]) {
                    if (node.Name.Equals(fill.TableName)) {
                        Dictionary<string, string> keyValues = new Dictionary<string, string>();
                        infos.ForEach(i => { 
                            keyValues[i.Name] = "";
                        });
                        foreach(XmlNode valueNode in node.ChildNodes) {
                            keyValues[valueNode.Name] = valueNode.InnerText;
                        }
                        foreach (string key in keyValues.Keys) {
                            fill.XmlData.AddFieldValue(key, keyValues[key]);
                        }
                    } else if ("edit_uuid".Equals(node.Name)) {
                        fill.Guid = node.InnerText;
                    } else {
                        // skip header
                        continue;
                    }
                }
            }
        }

        /*
         * Query the user for the unresolved columns of the given table.
         * Return false if user requested quit.
         */
        public bool ManuallyResolveTableMapping(string tableName) {

            MappedTable table = mappedTables[tableName];
            Console.WriteLine("\nTable {0}", table.TableName);
            List<NameMapping> mappings = new List<NameMapping>();
            List<string> candidates = new List<string>(table.UnmappedXmlFieldNames);
            foreach (string query in table.UnmappedPackFieldNames) {
                if (candidates.Count == 0) {
                    continue;
                }
                Console.WriteLine("Enter corresponding field for \"{0}\":", query);
                for (int i = 0; i < candidates.Count; i++) {
                    Console.WriteLine("{0}: {1}", i, candidates[i]);
                }
                int response = -1;
                while (response != int.MinValue && (response < 0 || response > candidates.Count - 1)) {
                    string val = Console.ReadLine();
                    if ("q".Equals(val)) {
                        return false;
                    } else if ("n".Equals(val)) {
                        response = int.MinValue;
                        break;
                    }
                    int.TryParse(val, out response);
                }
                if (response == int.MinValue) {
                    continue;
                }
                string mapped = candidates[response];
                candidates.Remove(mapped);
                mappings.Add(new NameMapping(query, mapped));
            }
            return true;
        }
    }

    class CaFieldInfo {
        public CaFieldInfo(string name, string type) {
            if (name == null || type == null) {
                throw new InvalidDataException();
            }
            Name = name; 
            fieldType = type;
        }

        public string Name { get; private set; }

        string fieldType;

        public bool Ignored {
            get {
                return fieldType.Equals("memo");
            }
        }

        FieldReference reference;
        public FieldReference Reference { get; set; }

        public static List<CaFieldInfo> ReadInfo(string xmlDirectory, string tableName) {
            List<CaFieldInfo> result = new List<CaFieldInfo>();
            try {
                string filename = Path.Combine(xmlDirectory, string.Format("TWaD_{0}.xml", tableName));
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filename);
                foreach (XmlNode node in xmlDoc.ChildNodes) {
                    if (node.Name.Equals("root")) {
                        foreach (XmlNode fieldNode in node.ChildNodes) {
                            if (!fieldNode.Name.Equals("field")) {
                                continue;
                            }
                            string name = null, type = null;
                            string refTable = null, refField = null;
                            foreach (XmlNode itemNode in fieldNode.ChildNodes) {
                                if (itemNode.Name.Equals("name")) {
                                    name = itemNode.InnerText;
                                } else if (itemNode.Name.Equals("field_type")) {
                                    type = itemNode.InnerText;
                                } else if (itemNode.Name.Equals("column_source_table")) {
                                    refTable = itemNode.InnerText;
                                } else if (itemNode.Name.Equals("column_source_column")) {
                                    refField = itemNode.InnerText;
                                }
                            }
                            CaFieldInfo info = new CaFieldInfo(name, type);
                            if (refTable != null && refField != null) {
                                info.Reference = new FieldReference(refTable, refField);
                            }
                            result.Add(info);
                        }
                    }
                }
            } catch { }
            return result;
        }

        public static CaFieldInfo FindInList(List<CaFieldInfo> list, string name) {
            CaFieldInfo result = null;
            foreach (CaFieldInfo info in list) {
                if (info.Name.Equals(name)) {
                    result = info;
                    break;
                }
            }
            return result;
        }
    }
}
