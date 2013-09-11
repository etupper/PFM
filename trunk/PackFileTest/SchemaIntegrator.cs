using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public Game VerifyAgainst {
            get;
            set;
        }
        public bool IntegrateExisting {
            get; set;
        }
        public bool OverwriteExisting {
            get; set;
        }
        
        private Dictionary<string, List<FieldInfo>> references = null;
        private Dictionary<string, PackedFile> packedFiles = new Dictionary<string, PackedFile>();
        
        public SchemaIntegrator() {
            if (!DBTypeMap.Instance.Initialized) {
                DBTypeMap.Instance.initializeFromFile(Path.Combine(Directory.GetCurrentDirectory(), DBTypeMap.MASTER_SCHEMA_FILE_NAME));
            }
        }
        
        private List<string> failedIntegrations = new List<String>();
        public List<string> FailedIntegrations {
            get {
                return failedIntegrations;
            }
        }
        
        private void BuildReferenceCache() {
            if (references != null) {
                return;
            }
            references = new Dictionary<string, List<FieldInfo>>();
            Console.WriteLine("building reference cache");
            foreach(TypeInfo typeInfo in DBTypeMap.Instance) {
                foreach(FieldInfo field in typeInfo.Fields) {
                    if (!string.IsNullOrEmpty(field.ForeignReference)) {
                        List<FieldInfo> addTo;
                        if (!references.TryGetValue(field.ForeignReference, out addTo)) {
#if DEBUG
                            // Console.WriteLine("Reference found: {0}", field.ForeignReference);
#endif
                            addTo = new List<FieldInfo>();
                        }
                        addTo.Add(field);
                    }
                }
            }

            Console.WriteLine("ok, done");
        }
        
        /*
         * Add db files from given pack to the packedFiles dictionary.
         */
        private void LoadDbFiles() {
            if (VerifyAgainst != null) {
                Console.WriteLine("building pack file cache");
                foreach (string file in new PackLoadSequence().GetPacksLoadedFrom(VerifyAgainst.GameDirectory)) {
                    PackFile pack = new PackFileCodec().Open(file);
                    foreach (PackedFile packed in pack) {
                        Console.WriteLine("loading {0}", packed.FullPath);
                        if (packed.FullPath.StartsWith("db")) {
                            string typename = DBFile.typename(packed.FullPath);
                            if (!packedFiles.ContainsKey(typename)) {
                                packedFiles[typename] = packed;
                            }
                        }
                    }
                }
            }
        }
        
        public void IntegrateFile(string filename) {
            LoadDbFiles();
            BuildReferenceCache();
            Console.WriteLine("Integrating schema file {0}", filename);
            using (var stream = File.OpenRead(filename)) {
                XmlImporter importer = new XmlImporter(stream);
                importer.Import();

                if (IntegrateExisting) {
                    foreach (string type in importer.Descriptions.Keys) {
                        IntegrateTable(type, importer.Descriptions[type]);
                    }
                }
                foreach(GuidTypeInfo info in importer.GuidToDescriptions.Keys) {
                    List<FieldInfo> infos;
                    if (!importer.GuidToDescriptions.TryGetValue(info, out infos)) {
                        // nothing to integrate
                        continue;
                    }
                    if (!DBTypeMap.Instance.GuidMap.ContainsKey(info)) {
                        DBTypeMap.Instance.SetByGuid(info.Guid, info.TypeName, info.Version, infos);
                    } else {
                        PackedFile dbPacked;
                        List<FieldInfo> oldInfo = DBTypeMap.Instance.GetInfoByGuid(info.Guid);
                        if (packedFiles.TryGetValue(info.TypeName, out dbPacked)) {
                            if (OverwriteExisting || !CanDecode(dbPacked)) {
                                DBTypeMap.Instance.SetByGuid(info.Guid, info.TypeName, info.Version, infos);
                                if (CanDecode(dbPacked)) {
                                    Console.WriteLine("Using new schema for {0}", info.TypeName);
                                } else {
                                    DBTypeMap.Instance.SetByGuid(info.Guid, info.TypeName, info.Version, oldInfo);
                                }
                            } else if (IntegrateExisting) {
                                IntegrateInto(info.TypeName, oldInfo, infos);
                                DBTypeMap.Instance.SetByGuid(info.Guid, info.TypeName, info.Version, oldInfo);
                            } else {
                                Console.WriteLine("Can already decode {0}", info.TypeName);
                            }
                        }
                    }
                }
            }
        }

        public void AddSchemaFile(string file) {
            using (var stream = File.OpenRead(file)) {
                XmlImporter importer = new XmlImporter(stream);
                importer.Import();
                foreach (GuidTypeInfo info in importer.GuidToDescriptions.Keys) {
                    if (!DBTypeMap.Instance.GuidMap.ContainsKey(info)) {
                        Console.WriteLine("adding {0}", info.Guid);
                        DBTypeMap.Instance.SetByGuid(info.Guid, info.TypeName, info.Version, importer.GuidToDescriptions[info]);
                    } else {
                        List<FieldInfo> existingInfo = DBTypeMap.Instance.GetInfoByGuid(info.Guid);
                        List<FieldInfo> update = importer.GuidToDescriptions[info];
                        ReplaceUnknowns(existingInfo, update);
                    }
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

        static void ReplaceUnknowns(List<FieldInfo> replaceIn, List<FieldInfo> replaceFrom) {
            if (replaceIn.Count != replaceFrom.Count) {
                return;
            }
            for (int i = 0; i < replaceIn.Count; i++) {
                FieldInfo fromInfo = replaceFrom[i];
                if (!UNKNOWN_RE.IsMatch(fromInfo.Name)) {
                    continue;
                }
                FieldInfo toInfo = replaceIn[i];
                
                if (!fromInfo.TypeName.Equals(toInfo.TypeName)) {
                    return;
                }
                toInfo.Name = fromInfo.Name.ToLower();
            }
        }
        

        static readonly Regex UNKNOWN_RE = new Regex("[Uu]nknown[0-9]*");
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
        
        public void AddCaData(string caDir) {
            CaXmlDbFileCodec codec = new CaXmlDbFileCodec(caDir);
            LoadDbFiles();

            foreach(string dbType in packedFiles.Keys) {
                
                if (dbType.Contains("advice_levels")) {
                    Console.WriteLine("one second...");
                }
                
                DBFileHeader header = PackedFileDbCodec.readHeader(packedFiles[dbType]);
                if (header.GUID != null && header.EntryCount > 0) {
                    TypeInfo caInfo = codec.TypeInfoByTableName(dbType);
                    List<FieldInfo> existingInfo = DBTypeMap.Instance.GetInfoByGuid(header.GUID);
                    if (caInfo != null && existingInfo != null) {
                        for (int fieldIndex = 0; fieldIndex < existingInfo.Count; fieldIndex++) {
                            FieldInfo info = existingInfo[fieldIndex];
                            if (CheckAllFields || UNKNOWN_RE.IsMatch(info.Name)) {
                                try {
                                    DBFile packData = PackedFileDbCodec.Decode(packedFiles[dbType]);
                                    string caXmlFile = string.Format("{0}.xml", dbType.Replace("_tables", ""));
                                    caXmlFile = Path.Combine(caDir, caXmlFile);
                                    DBFile caData = codec.Decode(File.OpenRead(caXmlFile));
                                    List<string> fieldValues = GetFieldValues(packData.Entries, fieldIndex);
                                    List<string> fieldNames = GetMatchingFieldNames(caData, fieldValues);
                                    if (fieldNames.Count != 1) {
                                        Console.WriteLine("no unique fields matching data from {0}:{1}", dbType, info.Name);
                                    } else if (!info.Name.Equals(fieldNames[0])) {
                                        Console.WriteLine("resolved {0}:{1} to {2}", dbType, info.Name, fieldNames[0]);
                                        info.Name = fieldNames[0];
                                    }
                                } catch (Exception e) {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                        addCaReferences (caInfo, existingInfo);
                    }
                }
            }
        }
        
        // when integrating CA tables, check all values for all fields?
        // that takes really long
        public bool CheckAllFields { get; set; }
        
        List<string> GetFieldValues(List<List<FieldInstance>> fieldList, int fieldIndex) {
            List<string> fieldData = new List<string>();
            foreach(List<FieldInstance> fields in fieldList) {
                fieldData.Add(fields[fieldIndex].Value);
            }
            return fieldData;
        }
        List<string> GetMatchingFieldNames(DBFile file, List<string> toMatch) {
            List<string> result = new List<string>();
            for (int i = 0; i < file.CurrentType.Fields.Count; i++) {
                List<string> values = GetFieldValues(file.Entries, i);
                if (Enumerable.SequenceEqual<string>(values, toMatch)) {
                    result.Add(file.CurrentType.Fields[i].Name);
                }
            }
            return result;
        }

        void addCaReferences(TypeInfo caInfo, List<FieldInfo> existingInfo) {
            foreach (FieldInfo caField in caInfo.Fields) {
                if (caField.FieldReference != null) {
                    // we found a reference, add it to the one we have
                    foreach (FieldInfo ourField in existingInfo) {
                        if (ourField.Name.Equals(caField.Name)) {
                            if (packedFiles.ContainsKey(caField.ReferencedTable)) {
                                // found the corresponding field
                                ourField.FieldReference = caField.FieldReference;
                                break;
                            } else if (ourField.FieldReference != null) {
                                ourField.FieldReference = null;
                            }
                            break;
                        }
                    }
                }
            }
        }
        
        void CorrectReferences(string type, FieldInfo toInfo, string newName) {
            string referenceString = FieldReference.FormatReference(type, toInfo.Name);
            
            if (references.ContainsKey(referenceString)) {
                foreach(FieldInfo info in references[referenceString]) {
                    info.ReferencedField = newName;
                    Console.WriteLine("Correcting reference {0}: to {1}", 
                                      referenceString, info.ForeignReference);
                }
            }
        }
        
        static string FormatTable(string tableName, List<FieldInfo> info) {
            return string.Format("{0}: {1}", tableName, string.Join(",", info));
        }

        public static bool CanDecode(PackedFile dbFile) {
            bool valid = false;
            try {
                DBFileHeader header = PackedFileDbCodec.readHeader(dbFile);
                DBFile decoded = PackedFileDbCodec.Decode(dbFile);
                valid = (decoded.Entries.Count == header.EntryCount);
                return valid;
            } catch (Exception) {
            }
            return valid;
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
  
        /*
         * This doesn't really belong here...
         * changes all strings in an existing table definition to string_asci.
         */
        public void ConvertAllStringsToAscii(string packFile) {
            PackFile file = new PackFileCodec().Open(packFile);
            foreach (PackedFile packed in file) {
                if (packed.FullPath.StartsWith("db")) {
                    string typename = DBFile.typename(packed.FullPath);
                    DBFileHeader header = PackedFileDbCodec.readHeader(packed);
                    if (!string.IsNullOrEmpty(header.GUID)) {
                        // List<FieldInfo> infos = DBTypeMap.Instance.GetInfoByGuid(header.GUID);
                        if (!CanDecode(packed)) {
                            // we don't have an entry for this yet; try out the ones we have
                            List<TypeInfo> allInfos = DBTypeMap.Instance.GetAllInfos(typename);
                            if (allInfos.Count > 0) {
                                TryDecode(packed, header, allInfos);
                            } else {
                                Console.WriteLine("no info at all for {0}", typename);
                            }
                        } else {
                            Console.WriteLine("already have info for {0}", header.GUID);
                        }
                    }
                }
            }
        }
        
        void TryDecode(PackedFile dbFile, DBFileHeader header, List<TypeInfo> infos) {
            foreach (TypeInfo info in infos) {
                // register converted to type map
                List<FieldInfo> converted = ConvertToAscii(info.Fields);
                DBTypeMap.Instance.SetByGuid(header.GUID, DBFile.typename(dbFile.FullPath), header.Version, converted);
                bool valid = SchemaIntegrator.CanDecode(dbFile);
                if (!valid) {
                    DBTypeMap.Instance.SetByGuid(header.GUID, DBFile.typename(dbFile.FullPath), header.Version, null);
                } else {
                    // found it! :)
                    Console.WriteLine("adding converted info for guid {0}", header.GUID);
                    break;
                }
            }
        }

        List<FieldInfo> ConvertToAscii(List<FieldInfo> old) {
            List<FieldInfo> newInfos = new List<FieldInfo>(old.Count);
            foreach (FieldInfo info in old) {
                string typeName = info.TypeName.EndsWith("string") ? string.Format("{0}_ascii", info.TypeName) : info.TypeName;
                FieldInfo newInfo = Types.FromTypeName(typeName);
                newInfo.Name = info.Name;
                newInfo.FieldReference = info.FieldReference;
                if (!string.IsNullOrEmpty(newInfo.ForeignReference)) {
                    newInfo.ForeignReference = info.ForeignReference;
                    //newInfo.ReferencedField = info.ReferencedField;
                }
                newInfo.PrimaryKey = info.PrimaryKey;
                newInfos.Add(newInfo);   
            }
            return newInfos;
        }
    }
}

