using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NameMapping = System.Tuple<string, string>;

namespace PackFileTest.Mapping {
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
                packData.Fields.ForEach(f => { if (!mappedFields.ContainsKey(f)) { result.Add(f); } });
                return result;
            }
        }
        public List<string> UnmappedXmlFieldNames {
            get {
                List<string> result = new List<string>();
                xmlData.Fields.ForEach(f => { if (!mappedFields.ContainsValue(f)) { result.Add(f); } });
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
            foreach (string field in values.Keys) {
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
