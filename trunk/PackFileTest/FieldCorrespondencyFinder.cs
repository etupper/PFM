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

        Dictionary<string, DBFile> patchFileValues = new Dictionary<string, DBFile>();
        
        Dictionary<string, List<NameMapping>> fullyMapped = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> FullyMapped {
            get {
                return fullyMapped;
            }
        }
        Dictionary<string, List<NameMapping>> partiallyMapped = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> PartiallyMapped {
            get {
                return partiallyMapped;
            }
        }
        Dictionary<string, List<NameMapping>> incompatibleTables = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> IncompatibleTables  {
            get {
                return incompatibleTables;
            }
        }

        Dictionary<string, string> tableToGuid = new Dictionary<string, string>();
        public Dictionary<string, string> TableToGuid {
            get { return tableToGuid; }
        }

        public FieldCorrespondencyFinder (string packFile, string xmlDir) {
            DBTypeMap.Instance.InitializeTypeMap(Directory.GetCurrentDirectory());
            // initialize patchFileValues from pack file
            PackFile pack = new PackFileCodec().Open(packFile);
            foreach(PackedFile contained in pack.Files) {
                if (contained.FullPath.StartsWith("db")) {
                    try {
                        PackedFileDbCodec codec = PackedFileDbCodec.GetCodec(contained);
                        DBFile dbFile = codec.Decode(contained.Data);
                        patchFileValues[contained.Name] = dbFile;
                    } catch (Exception e) {
#if DEBUG
                        Console.Error.WriteLine(e.Message);
#endif
                    }
                }
            }
            xmlDirectory = xmlDir;
        }
        
        public void FindAllCorrespondencies() {
            foreach(string tableName in patchFileValues.Keys) {
                FindCorrespondencies(tableName);
            }
        }

        /*
         * Find the corresponding fields for all columns in the given table.
         */
        void FindCorrespondencies(string tableName) {
            List<TableColumn> dbValues = ValuesFromPack(tableName);
            List<NameMapping> mapping = new List<NameMapping>();
            Dictionary<string, List<NameMapping>> addToMemberMap = fullyMapped;
            if (dbValues != null) {
                List<TableColumn> valuesFromXml = ValuesFromXml(tableName);
                if (dbValues.Count != valuesFromXml.Count) {
                    addToMemberMap = incompatibleTables;
                }
                foreach (TableColumn valuesFromPack in ValuesFromPack(tableName)) {

                    // make sure we have enough source data to compare
                    if (valuesFromPack == null || valuesFromPack.Values.Count == 0) {
                        //if (dbValues.Count == 1 && valuesFromXml.Count == 1) {
                        //    NameMapping mapping = new NameMapping(dbValues[0].
                        //}
                        Console.Error.WriteLine("No data available for {0}", tableName);
                        continue;
                    }
                    
                    // find the same values in the xml
                    NameMapping corresponding = FindCorrespondency(valuesFromPack, valuesFromXml);
                    if (corresponding != null) {
                        Console.WriteLine("{0}:{1}-{2}", tableName, corresponding.Item1, corresponding.Item2);
                        mapping.Add(corresponding);
                    } else if (dbValues.Count == 1 && valuesFromXml.Count == 1) {
                        mapping.Add(new NameMapping(dbValues[0].TableColumnNames[0], valuesFromXml[0].TableColumnNames[0]));
                    } else {
                        Console.Error.WriteLine("No correspondence found for {0}:{1}", tableName, string.Join(",", valuesFromPack.TableColumnNames));
                        addToMemberMap = partiallyMapped;
                    }
                }
                addToMemberMap[tableName] = mapping;
            } else {
                Console.Error.WriteLine("No {0} entry in pack", tableName);
            }
        }
        
        /*
         * In the given list, find the table with all values equal to the given one from the pack.
         */
        NameMapping FindCorrespondency(TableColumn packTable, List<TableColumn> xml) {
            NameMapping result = null;
            if (packTable.TableColumnNames.Count == 1) {
                string packTableName = packTable.TableColumnNames[0];
                if (xml.Count == 1) {
                    result = new NameMapping(packTableName, xml[0].TableColumnNames[0]);
                } else {
                    foreach (TableColumn xmlTable in xml) {
                        if (xmlTable.SameValues(packTable.Values)) {
                            // found our match
                            if (xmlTable.TableColumnNames.Count == 1) {
                                // only one candidate; this is it
                                result = new NameMapping(packTableName, xmlTable.TableColumnNames[0]);
                            } else {
                                string unifiedName = UnifyName(packTableName);
                                foreach (string xmlColumnName in xmlTable.TableColumnNames) {
                                    if (unifiedName.Equals(xmlColumnName)) {
                                        result = new NameMapping(packTableName, xmlColumnName);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
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
        List<TableColumn> ValuesFromPack(string tableName) {
            List<TableColumn> result = new List<TableColumn>();
            DBFile dbFile = null;
            if (patchFileValues.TryGetValue(tableName, out dbFile)) {
                for (int columnIndex = 0; columnIndex < dbFile.CurrentType.Fields.Count; columnIndex++) {
                    FieldInfo info = dbFile.CurrentType.Fields[columnIndex];
                    List<string> values = new List<string>();
                    for (int row = 0; row < dbFile.Entries.Count; row++) {
                        values.Add(dbFile[row, columnIndex].Value);
                    }
                    TableColumn addTo = null;
                    foreach (TableColumn column in result) {
                        if (column.SameValues(values)) {
                            addTo = column;
                        }
                    }
                    if (addTo == null) {
                        addTo = new TableColumn(values);
                        result.Add(addTo);
                    }
                    addTo.TableColumnNames.Add(info.Name);
                }
            }
            return result;
        }
  
        /*
         * Retrieve the columnname/valuelist collection from the table with the given name.
         */
        List<TableColumn> ValuesFromXml(string tableName) {
            List<TableColumn> result = new List<TableColumn>();
            XmlDocument tableDocument = new XmlDocument();
            string xmlFile = Path.Combine(xmlDirectory, string.Format("{0}.xml", tableName));
            if (File.Exists(xmlFile)) {
                tableDocument.Load(xmlFile);
                foreach(XmlNode node in tableDocument.ChildNodes[1]) {
                    if (node.Name.Equals(tableName)) {
                        foreach(XmlNode valueNode in node.ChildNodes) {
                            AddToTable(valueNode.Name, valueNode.InnerText, result);
                        }
                    } else if ("edit_uuid".Equals(node.Name) && !tableToGuid.ContainsKey(tableName)) {
                        tableToGuid[tableName] = node.InnerText;
                    } else {
                        // skip header
                        continue;
                    }
                }
            }
            return result;
        }
  
        /* 
         * Helper method to add the given column name to the associated list if there is any,
         * or create one if there isn't.
         */
        void AddToTable(string columnName, string value, List<TableColumn> tables) {
            TableColumn addTo = null;
            foreach (TableColumn table in tables) {
                if (table.TableColumnNames.Contains(columnName)) {
                    addTo = table;
                    break;
                }
            }
            if (addTo == null) {
                addTo = new TableColumn(new List<string>());
                addTo.TableColumnNames.Add(columnName);
                tables.Add(addTo);
            }
            addTo.Values.Add(value);
        }
    }

    public class TableColumn {
        public TableColumn(List<string> values) {
            tableColumnValues = values;
        }
        List<string> tableColumnValues;
        public List<string> Values {
            get { return tableColumnValues; }
        }

        List<string> tableColumnNames = new List<string>();
        public List<string> TableColumnNames {
            get { return tableColumnNames; }
        }

        /*
         * Helper method to compare values from the given list.
         * Performs some conversions (rounds CA floats to 2 digits and transforms
         * binary's bools to ints (1 and 0 respectively).
         */
        public bool SameValues(List<string> values1) {
            bool result = values1.Count == tableColumnValues.Count;
            if (result) {
                for (int i = 0; i < values1.Count; i++) {
                    result = values1[i].Equals(tableColumnValues[i]);
                    if (!result) {
                        // maybe floats? Those are rounded differently
                        try {
                            double value1;
                            double value2;
                            bool bValue1;
                            int iValue2;
                            string v2 = tableColumnValues[i].Replace(".", ",");
                            bool parsed = double.TryParse(values1[i], out value1);
                            parsed &= double.TryParse(v2, out value2);
                            if (parsed) {
                                value1 = Math.Round(value1, 2);
                                value2 = Math.Round(value2, 2);
                                result = value1 == value2;
                            } else if (bool.TryParse(values1[i], out bValue1) &&
                                       int.TryParse(tableColumnValues[i], out iValue2)) {
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
