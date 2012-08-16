using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Common;
using Filetypes;

namespace PackFileTest {
    /**
     * Reads a schema file and integrates it into the currently loaded definitions
     * by renaming all applicable fields to the names contained in the new file.
     * 
     * Applicable fields are those that are contained in tables of the same version.
     */
    public class SchemaIntegrator {
        public bool Verbose {
            get; set;
        }
        
        private Dictionary<string, List<FieldInfo>> references = new Dictionary<string, List<FieldInfo>>();
        
        public SchemaIntegrator() {
            Console.WriteLine("building reference cache");
            foreach(TypeInfo typeInfo in DBTypeMap.Instance) {
                foreach(FieldInfo field in typeInfo.Fields) {
                    if (!string.IsNullOrEmpty(field.ForeignReference)) {
                        List<FieldInfo> addTo;
                        if (!references.TryGetValue(field.ForeignReference, out addTo)) {
                            Console.WriteLine("Reference found: {0}", field.ForeignReference);
                            addTo = new List<FieldInfo>();
                        }
                        addTo.Add(field);
                    }
                }
            }
            Console.WriteLine("ok, done");
        }
        
        private List<string> failedIntegrations = new List<String>();
        public List<string> FailedIntegrations {
            get {
                return failedIntegrations;
            }
        }
        
        public void IntegrateFile(string filename) {
            Console.WriteLine("Integrating schema file {0}", filename);
            using (var stream = File.OpenRead(filename)) {
                XmlImporter importer = new XmlImporter(stream);
                importer.Import();
                
                foreach(string type in importer.Descriptions.Keys) {
                    IntegrateTable(type, importer.Descriptions[type]);
                }
            }
        }
        
        void IntegrateTable(string type, List<FieldInfo> toIntegrate) {
            if (Verbose) {
                Console.WriteLine("*** Integrating table {0}", FormatTable(type, toIntegrate));
            }
            ICollection<int> differentVersions = GetVersions(toIntegrate);
            foreach(int version in differentVersions) {
                List<FieldInfo> integrateFrom = DBTypeMap.FilterForVersion(toIntegrate, version);
                ICollection<List<FieldInfo>> integrateToInfos = InfosForTypename(type, version);
                foreach(List<FieldInfo> integrateTo in integrateToInfos) {
                    try {
                        if (Verbose) {
                            Console.WriteLine(" Integrating to {0}", FormatTable(type, integrateFrom));
                        }
                        IntegrateInto(type, integrateTo, integrateFrom);
                    } catch (Exception ex) {
                        if (!failedIntegrations.Contains(type)) {
                            Console.Error.WriteLine("Cannot integrate:\n{0} (version {2}) into \n{1}",
                                                    FormatTable(type, toIntegrate), 
                                                    FormatTable(type, integrateTo), 
                                                    version, ex.Message);
                            Console.Error.WriteLine();
                            FailedIntegrations.Add(type);
                        }
                    }
                }
            }
        }
        
        static readonly Regex UNKNOWN_RE = new Regex("[Uu]nknown");
        void IntegrateInto (string type, List<FieldInfo> integrateInto, List<FieldInfo> integrateFrom) {
            for(int i = 0; i < integrateFrom.Count; i++) {
                FieldInfo fromInfo = integrateFrom[i];
                if (integrateInto.Count <= i) {
                    Console.WriteLine(" stopping integration: can't find {0}. field in {1}", 
                                      i, FormatTable(type, integrateInto));
                }
                FieldInfo to = integrateInto[i];
                if (to.TypeCode != fromInfo.TypeCode) {
                    throw new InvalidDataException(string.Format("Field {0}: invalid Type (can't integrate {1}, original {2})",
                                                                 i, fromInfo.TypeName, to.TypeName));
                }
                // don't rename to "unknown"
                if (UNKNOWN_RE.IsMatch(fromInfo.Name)) {
                    if (Verbose) {
                        Console.WriteLine("Not renaming {0} to {1}", fromInfo.Name, to.Name);
                    }
                    continue;
                }
                if (!to.Name.Equals(fromInfo.Name)) {
                    if (Verbose) {
                        Console.WriteLine("Renaming {0} to {1}", fromInfo.Name, to.Name);
                    }
                    CorrectReferences(type, to, fromInfo.Name);
                    to.Name = fromInfo.Name;
                }
            }
        }
        
        void CorrectReferences(string type, FieldInfo toInfo, string newName) {
            string referenceString = DBReferenceMap.FormatReference(type, toInfo.Name);
            
            if (references.ContainsKey(referenceString)) {
                string newReference = DBReferenceMap.FormatReference(type, newName);
                foreach(FieldInfo info in references[referenceString]) {
                    info.ForeignReference = newReference;
                    Console.WriteLine("Correcting reference {0}: to {1}", 
                                      referenceString, newReference);
                }
            }
        }
        
        static string FormatTable(string tableName, List<FieldInfo> info) {
            return string.Format("{0}: {1}", tableName, string.Join(",", info));
        }
        
        static ICollection<int> GetVersions(List<FieldInfo> infos) {
            ICollection<int> result = new SortedSet<int>();
            infos.ForEach(i => {
                result.Add(i.StartVersion);
                result.Add(i.LastVersion);
            });
            result.Remove(int.MaxValue);
            return result;
        }
        
        static ICollection<List<FieldInfo>> InfosForTypename(string type, int version) {
            ICollection<List<FieldInfo>> result = new List<List<FieldInfo>>();
            if (DBTypeMap.Instance.TypeMap.ContainsKey(type)) {
                List<FieldInfo> info = DBTypeMap.Instance.TypeMap[type];
                if (GetVersions(info).Contains(version)) {
                    result.Add(DBTypeMap.FilterForVersion(info, version));
                }
            }
            foreach(GuidTypeInfo guidInfo in DBTypeMap.Instance.GuidMap.Keys) {
                if (guidInfo.TypeName.Equals(type) && guidInfo.Version == version) {
                    result.Add(DBTypeMap.Instance.GuidMap[guidInfo]);
                }
            }
            return result;
        }
    }
}

