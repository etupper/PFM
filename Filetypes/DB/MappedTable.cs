using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Filetypes {
    using NameMapping = System.Tuple<string, string>;

    public abstract class MappedTable {
        private string tableName;
        public MappedTable(string name) {
            tableName = name;
        }
        public string TableName {
            get { return tableName; }
        }

        public string Guid { get; set; }
        public abstract List<string> PackDataFields { get; }
        public abstract List<string> XmlDataFields { get; }
        public abstract Dictionary<string, string> ConstantValues { get; }

        #region Mapping
        protected Dictionary<string, string> mappedFields = new Dictionary<string, string>();
        public bool IsFullyMapped {
            get {
                 return mappedFields.Count + ConstantValues.Count == PackDataFields.Count;
            }
        }
        public virtual void AddMapping(string packField, string xmlField) {
            mappedFields[packField] = xmlField;
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
        public string GetMappedXml(string packedFieldName) {
            return mappedFields.ContainsKey(packedFieldName) ? mappedFields[packedFieldName] : null;
        }

        public List<string> UnmappedPackFieldNames {
            get {
                List<string> result = new List<string>();
                List<string> constantPackFields = new List<string>(ConstantValues.Keys);
                PackDataFields.ForEach(f => { if (!mappedFields.ContainsKey(f) && !ConstantValues.ContainsKey(f)) { result.Add(f); } });
                return result;
            }
        }
        public List<string> UnmappedXmlFieldNames {
            get {
                List<string> result = new List<string>();
                foreach (string f in XmlDataFields) {
                    if (!mappedFields.ContainsValue(f)) {
                        result.Add(f);
                    }
                }
                return result;
            }
        }
        #endregion

        #region References
        private Dictionary<string, FieldReference> references = new Dictionary<string, FieldReference>();
        public Dictionary<string, FieldReference> References {
            get { return references; }
        }
        public FieldReference GetReference(string field) {
            FieldReference result = null;
            if (references.ContainsKey(field)) {
                result = references[field];
            }
            return result;
        }
        #endregion

        public virtual string GetConstantValue(string packedFieldName) {
            string result = null;
            if (ConstantValues.ContainsKey(packedFieldName)) {
                result = ConstantValues[packedFieldName];
            }
            return result;
        }
    }

    class MappedInfoTable : MappedTable {
        public MappedInfoTable(string name) : base(name) { }

        public override void AddMapping(string packField, string xmlField) {
            base.AddMapping(packField, xmlField);
            packDataFields.Add(packField);
            xmlDataFields.Add(xmlField);
        }

        private List<string> packDataFields = new List<string>();
        public override List<string> PackDataFields {
            get {
                return packDataFields;
                //List<string> result = new List<string>(PackDataFields);
                //result.AddRange(mappedFields.Keys);
                //return result; 
            }
        }
        private List<string> xmlDataFields = new List<string>();
        public override List<string> XmlDataFields {
            get {
                return xmlDataFields;
                //List<string> result = new List<string>(XmlDataFields);
                //result.AddRange(mappedFields.Values);
                //return result;
            }
        }

        protected Dictionary<string, string> constantValues = new Dictionary<string, string>();
        public override Dictionary<string, string> ConstantValues {
            get { return constantValues; }
        }
        public void AddConstantValue(string field, string value) {
            packDataFields.Add(field);
            constantValues.Add(field, value);
        }
    }
}
