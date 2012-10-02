using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using Common;
using Filetypes;

using ValueList = System.Collections.Generic.List<string>;
using Table = System.Tuple<string, System.Collections.Generic.List<string>>;
using NameMapping = System.Tuple<string, string>;

namespace PackFileTest {
    /*
     * Finds the field correspondencies between the db file schema and the ones used by CA.
     * Does this by comparing all values within a column, so it depends on
     * the pack file containing the exact same patch level as the xml files.
     * Also, might fail on columns with very few values or where there are several columns
     * in a table which have the exact same values.
     * 
     * The output consists of a line each of
     * tablename:packFieldName-xmlFieldName
     * where tablename is the db table, packFieldName is the name from PFM's schema file,
     * and xmlFieldName is the name used by CA.
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
                    string tableName = DBFile.typename(contained.FullPath);
                    try {
                        PackedFileDbCodec codec = PackedFileDbCodec.GetCodec(contained);
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
                } else if (table.IsCompatible) {
                    TableNameCorrespondencyManager.Instance.IncompatibleTables.Add(table.TableName, table.MappingAsTuples);
                } else {
                    TableNameCorrespondencyManager.Instance.PartialTableMapping.Add(table.TableName, table.MappingAsTuples);
                }
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
        }
        
        /*
         * In the given list, find the table with all values equal to the given one from the pack.
         */
        NameMapping FindCorrespondency(MappedTable dataTable, string fieldName) {
            NameMapping result = null;
            List<string> values = dataTable.PackData.Values(fieldName);
            List<string> packTableNames = dataTable.PackData.FieldsContainingValues(values);
            List<string> xmlTableNames = dataTable.PackData.FieldsContainingValues(values);

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

        /*
         * Query the user for the unresolved columns of the given table.
         * Return false if user requested quit.
         */
        public bool ManuallyResolveTableMapping(string tableName) {
                        
            MappedTable table = mappedTables[tableName];
            Console.WriteLine("\nTable {0}", table.TableName);
            List<NameMapping> mappings = new List<NameMapping>();
            List<string> candidates = new List<string>(table.UnmappedXmlFieldNames);
            foreach(string query in table.UnmappedPackFieldNames) {
                if (candidates.Count == 0) {
                    continue;
                }
                Console.WriteLine("Enter corresponding field for \"{0}\":", query);
                for (int i = 0; i < candidates.Count; i++) {
                    Console.WriteLine("{0}: {1}", i, candidates[i]);
                }
                int response = -1;
                while (response != int.MinValue && (response < 0 || response > candidates.Count-1)) {
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
            if (File.Exists(xmlFile)) {
                tableDocument.Load(xmlFile);
                foreach(XmlNode node in tableDocument.ChildNodes[1]) {
                    if (node.Name.Equals(fill.TableName)) {
                        foreach(XmlNode valueNode in node.ChildNodes) {
                            fill.XmlData.AddFieldValue(valueNode.Name, valueNode.InnerText);
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
    }

    
    class MappedTable {
        private string tableName;
        private Dictionary<string, string> mappedFields = new Dictionary<string, string>();
        private DataTable packData = new DataTable();
        private DataTable xmlData = new DataTable();
        
        public MappedTable(string name) {
            tableName = name;
        }
        
        public string TableName {
            get { return tableName; }
        }
        public string Guid { get; set; }
        public bool IsCompatible {
            get { 
                return packData.Fields.Count == xmlData.Fields.Count; 
            }
        }
        public bool IsFullyMapped {
            get {
                return IsCompatible && mappedFields.Count == packData.Fields.Count;
            }
        }
        
        public List<NameMapping> MappingAsTuples {
            get {
                List<NameMapping> result = new List<NameMapping>(mappedFields.Count);
                foreach (string field in mappedFields.Keys) {
                    result.Add(new NameMapping(field, mappedFields[field]));
                }
                return result;
            }
        }
  
        #region Field Name Retrieval
        public List<string> UnmappedPackFieldNames {
            get {
                List<string> result = new List<string>();
                packData.Fields.ForEach(f => { if (!mappedFields.ContainsKey(f)) { result.Add(f); }});
                return result;
            }
        }
        public List<string> UnmappedXmlFieldNames {
            get {
                List<string> result = new List<string>();
                xmlData.Fields.ForEach(f => { if (!mappedFields.ContainsKey(f)) { result.Add(f); }});
                return result;
            }
        }
        #endregion
  
        public DataTable PackData {
            get {
                return packData;
            }
        }
        public DataTable XmlData {
            get {
                return xmlData;
            }
        }
        
        public void AddMapping(string packField, string xmlField) {
            mappedFields[packField] = xmlField;
        }
    }
    
    class DataTable {
        Dictionary<string, List<string>> values = new Dictionary<string, List<string>>();
        
        public List<string> Fields {
            get {
                return new List<string>(values.Keys);
            }
        }
            
        public List<string> Values(string field) {
            return values[field];
        }

        public List<string> FieldsContainingValues(List<string> toMatch) {
            List<string> fields = new List<string>();
            foreach(string field in values.Keys) {
                if (SameValues(toMatch, values[field])) {
                    fields.Add(field);
                }
            }
            return fields;
        }
        
        public void AddFieldValue(string fieldName, string value) {
            List<string> list;
            if (values.ContainsKey(fieldName)) {
                list = values[fieldName];
            } else {
                list = new List<string>();
                values[fieldName] = list;
            }
            list.Add(value);
        }

        /*
         * Helper method to compare values from the given list.
         * Performs some conversions (rounds CA floats to 2 digits and transforms
         * binary's bools to ints (1 and 0 respectively).
         */
        public static bool SameValues(List<string> values1, List<string> values2) {
            bool result = values1.Count == values2.Count;
            if (result) {
                for (int i = 0; i < values1.Count; i++) {
                    result = values1[i].Equals(values2[i]);
                    if (!result) {
                        // maybe floats? Those are rounded differently
                        try {
                            double value1;
                            double value2;
                            bool bValue1;
                            int iValue2;
                            string v2 = values2[i].Replace(".", ",");
                            bool parsed = double.TryParse(values1[i], out value1);
                            parsed &= double.TryParse(v2, out value2);
                            if (parsed) {
                                value1 = Math.Round(value1, 2);
                                value2 = Math.Round(value2, 2);
                                result = value1 == value2;
                            } else if (bool.TryParse(values1[i], out bValue1) &&
                                       int.TryParse(values2[i], out iValue2)) {
                                int iValue1 = bValue1 ? 1 : 0;
                                result = iValue1 == iValue2;
                            }
                        } catch { }
                        if (!result) {
                            break;
                        }
                    }
                }
            }
            return result;
        }
    }
}
