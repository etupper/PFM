using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Filetypes;

using NameMapping = System.Tuple<string, string>;

namespace PackFileTest.Mapping {
    using NameMapping = System.Tuple<string, string>;

    class MappedDataTable : MappedTable {
        public MappedDataTable(string name) : base(name) { }

        private DataTable packData = new DataTable();
        private DataTable xmlData = new DataTable();

        public override List<string> PackDataFields {
            get { return packData.Fields; }
        }
        public override List<string> XmlDataFields {
            get { return xmlData.Fields; }
        }

        public override Dictionary<string, string> ConstantValues {
            get {
                Dictionary<string, string> result = new Dictionary<string, string>();
                if (UnmappedXmlFieldNames.Count != 0) {
                    // only looks for constant values if we don't have unmapped xml;
                    // values might be coming from one of those
                    return result;
                }
                List<string> unmapped = new List<string>(PackDataFields);
                unmapped.RemoveAll(delegate(string s) { return mappedFields.Keys.Contains(s); });
                foreach (string packFieldName in unmapped) {
                    List<string> values = packData.Values(packFieldName);
                    if (values.Count != 0) {
                        string lastValue = values[0];
                        bool allValuesEqual = true;
                        foreach (string value in values) {
                            allValuesEqual &= value.Equals(lastValue);
                            if (!allValuesEqual) {
                                break;
                            }
                        }
                        if (allValuesEqual) {
                            result.Add(packFieldName, lastValue);
                        }
                    }
                }
                return result;
            }
        }

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
