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
        
        Dictionary<string, List<NameMapping>> correspondingFields = new Dictionary<string, List<NameMapping>>();
        public Dictionary<string, List<NameMapping>> CorrespondingFields {
            get {
                return correspondingFields;
            }
        }

        public FieldCorrespondencyFinder (string xmlDir, string packFile) {
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
            List<Table> dbValues = ValuesFromPack(tableName);
            if (dbValues != null) {
                List<Table> valuesFromXml = ValuesFromXml(tableName);
                foreach(Table valuesFromPack in ValuesFromPack(tableName)) {

                    // make sure we have enough source data to compare
                    if (valuesFromPack == null || valuesFromPack.Item2.Count == 0) {
                        Console.Error.WriteLine("No data available for {0}", tableName);
                        continue;
                    }
                    
                    // find the same values in the xml
                    NameMapping corresponding = FindCorrespondency(valuesFromPack, valuesFromXml);
                    if (corresponding != null) {
                        Console.WriteLine("{0}:{1}-{2}", tableName, corresponding.Item1, corresponding.Item2);
                        List<NameMapping> addTo = new List<NameMapping>();
                        if (!correspondingFields.TryGetValue(tableName, out addTo)) {
                            addTo = new List<NameMapping>();
                            correspondingFields[tableName] = addTo;
                        }
                        addTo.Add(corresponding);
                    } else {
                        Console.Error.WriteLine("No correspondence found for {0}:{1}", tableName, valuesFromPack.Item1);
                    }
                }
            } else {
                Console.Error.WriteLine("No {0} entry in pack", tableName);
            }
        }
        
        /*
         * In the given list, find the table with all values equal to the given one from the pack.
         */
        NameMapping FindCorrespondency(Table packTable, List<Table> xml) {
            NameMapping result = null;
            string packTableName = packTable.Item1;
            foreach(Table xmlTable in xml) {
                string xmlTableName = xmlTable.Item1;
                if (SameValues(packTable.Item2, xmlTable.Item2)) {
                    result = new Tuple<string, string>(packTableName, xmlTableName);
                    break;
                }
            }
            return result;
        }
        
        /*
         * Helper method to compare values from the given list.
         * Performs some conversions (rounds CA floats to 2 digits and transforms
         * binary's bools to ints (1 and 0 respectively).
         */
        bool SameValues(List<string> values1, List<string> values2) {
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
                            if (double.TryParse(values1[i], out value1) && 
                                double.TryParse(values2[i], out value2)) {
                                value1 = Math.Round(value1, 2);
                                value2 = Math.Round(value2, 2);
                                result = value1 == value2;
                            } else if (bool.TryParse(values1[i], out bValue1) &&
                                       int.TryParse(values2[i], out iValue2)) {
                                int iValue1 = bValue1 ? 1 : 0;
                                result = iValue1 == iValue2;
                            }
                        } catch {}
                        if (!result) {
                            break;
                        }
                    }
                }
            }
            return result;
        }
  
        /*
         * Retrieve the columnname/valuelist collection from the db file of the given type.
         */
        List<Table> ValuesFromPack(string tableName) {
            List<Table> result = new List<Table>();
            DBFile dbFile = null;
            if (patchFileValues.TryGetValue(tableName, out dbFile)) {
                for (int columnIndex = 0; columnIndex < dbFile.CurrentType.Fields.Count; columnIndex++) {
                    FieldInfo info = dbFile.CurrentType.Fields[columnIndex];
                    List<string> values = new List<string>();
                    for (int row = 0; row < dbFile.Entries.Count; row++) {
                        values.Add(dbFile[row, columnIndex].Value);
                    }
                    Table column = new Tuple<string, List<string>>(info.Name, values);
                    result.Add(column);
                }
            }
            return result;
        }
  
        /*
         * Retrieve the columnname/valuelist collection from the table with the given name.
         */
        List<Table> ValuesFromXml(string tableName) {
            List<Table> result = new List<Table>();
            XmlDocument tableDocument = new XmlDocument();
            string xmlFile = Path.Combine(xmlDirectory, string.Format("{0}.xml", tableName));
            if (File.Exists(xmlFile)) {
                tableDocument.Load(xmlFile);
                foreach(XmlNode node in tableDocument.ChildNodes[1]) {
                    if (node.Name.Equals(tableName)) {
                        foreach(XmlNode valueNode in node.ChildNodes) {
                            AddToTable(valueNode.Name, valueNode.InnerText, result);
                        }
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
        void AddToTable(string columnName, string value, List<Table> tables) {
            Tuple<string, List<string>> addTo = null;
            foreach(Tuple<string, List<string>> table in tables) {
                if (table.Item1.Equals(columnName)) {
                    addTo = table;
                    break;
                }
            }
            if (addTo == null) {
                addTo = new Tuple<string, List<string>>(columnName, new List<string>());
                tables.Add(addTo);
            }
            addTo.Item2.Add(value);
        }
    }
}
