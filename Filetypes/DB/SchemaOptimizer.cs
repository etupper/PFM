using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace Filetypes {
    
    /*
     * Will go through all packs in a given directory, 
     * and remove all table/version definitions from the DBTypeMap
     * that are not used in any db file in any pack.
     */
    public class SchemaOptimizer {

        // the directory to iterate pack files of
        public string PackDirectory { get; set; }

        // the filename to save the result in
        public string SchemaFilename { get; set; }

        SortedDictionary<string, List<FieldInfo>> typeMap = new SortedDictionary<string, List<FieldInfo>>();
        SortedDictionary<GuidTypeInfo, List<FieldInfo>> guidMap = new SortedDictionary<GuidTypeInfo, List<FieldInfo>>();

        public SchemaOptimizer () {
            PackDirectory = "";
            SchemaFilename = "schema_optimized.xml";
        }
        
        public int RemovedEntries {
            get {
                int typeCount = DBTypeMap.Instance.TypeMap.Count - typeMap.Count;
                int guidCount = DBTypeMap.Instance.GuidMap.Count - guidMap.Count;
                return typeCount + guidCount;
            }
        }
        Dictionary<string, int> minVersion = new Dictionary<string, int>();
        Dictionary<string, int> maxVersion = new Dictionary<string, int>();

        public void FilterExistingPacks() {
            if (Directory.Exists(PackDirectory)) {
                DateTime start = DateTime.Now;
                Console.WriteLine("Retrieving from {0}, storing to {1}", PackDirectory, SchemaFilename);

                typeMap.Clear();
                guidMap.Clear();
                minVersion.Clear();
                maxVersion.Clear();

                List<GuidTypeInfo> allUsed = new List<GuidTypeInfo>();
                Dictionary<GuidTypeInfo, PackedFile> infos = new Dictionary<GuidTypeInfo, PackedFile>();
                foreach (string path in Directory.EnumerateFiles(PackDirectory, "*.pack")) {
                    PackFile pack = new PackFileCodec().Open (path);
                    GetUsedTypes(pack, infos);

                    // add all infos we don't have yet
                    foreach(GuidTypeInfo info in infos.Keys) {
                        if (!allUsed.Contains(info)) { 
                            allUsed.Add(info); 
                        }
                    }
                }
                
                foreach(GuidTypeInfo info in allUsed) {
                    if (!string.IsNullOrEmpty(info.Guid)) {
                        AddSafe(info, infos[info], guidMap);
                        continue;
                    }
                }
                foreach (string type in minVersion.Keys) {
                    List<FieldInfo> add = new List<FieldInfo>();
                    List<FieldInfo> addFrom;
                    if (DBTypeMap.Instance.TypeMap.TryGetValue(type, out addFrom)) {
#if DEBUG
                        if (type.Equals("agents_tables")) {
                            Console.WriteLine();
                        }
#endif
                        int min = minVersion[type];
                        int max = maxVersion[type];
                        
                        add = FilterList(addFrom, min, max);
                        typeMap[type] = add;
                    }
                }
                
                using (var stream = File.Create(SchemaFilename)) {
                    new XmlExporter(stream) { LogWriting = false}.Export(typeMap, guidMap);
                }

                DateTime end = DateTime.Now;
                Console.WriteLine("optimization took {0}", end.Subtract(start));
            }
        }

        private void AddSafe(GuidTypeInfo key, PackedFile packedFile, SortedDictionary<GuidTypeInfo, List<FieldInfo>> addTo) {
            List<FieldInfo> addValue;
            if (DBTypeMap.Instance.GuidMap.TryGetValue(key, out addValue) && !addTo.ContainsKey(key)) {
                addTo[key] = addValue;
            } else if (DBTypeMap.Instance.TypeMap.ContainsKey(key.TypeName)) {
                addTo[key] = FilterList(DBTypeMap.Instance.TypeMap[key.TypeName], key.Version, key.Version);
                // also add to guid map in DBTypeMap
                DBTypeMap.Instance.GuidMap[key] = addTo[key];
            } else {
                TypeInfo useInfo = null;
                DBFile checkFile = PackedFileDbCodec.Decode(packedFile);
                if (checkFile != null) {
                    useInfo = checkFile.CurrentType;
                    Console.WriteLine("no info for {2} guid {0}, but can use {1}", key.Guid, useInfo, key.TypeName);
                } else {
                    List<TypeInfo> allInfos = DBTypeMap.Instance.GetAllInfos(key.TypeName);
                    Console.WriteLine("no info for {2} guid {0}, using highest of {1}", key.Guid, allInfos.Count, key.TypeName);
                    int highestVersion = -1;
                    if (allInfos.Count > 0) {
                        allInfos.ForEach(i => {
                            if (i.Version > highestVersion) {
                                highestVersion = i.Version;
                                useInfo = i;
                            }
                        });
                    }
                }
                if (useInfo != null) {
                    DBTypeMap.Instance.GuidMap[key] = useInfo.Fields;
                }
            }
        }
        
        private List<FieldInfo> FilterList(List<FieldInfo> infos, int min, int max) {
            List<FieldInfo> result = new List<FieldInfo>();
#if DEBUG
            foreach(FieldInfo field in infos) {
                if (field.StartVersion <= max && field.LastVersion >= min) {
                    result.Add(field);
                }
            }
#else
        infos.ForEach(field => {
                if (field.StartVersion <= max && field.LastVersion >= min) {
                    result.Add(field);
                }
            });
#endif
            return result;
        }

        private Dictionary<GuidTypeInfo, PackedFile> GetUsedTypes(PackFile pack, Dictionary<GuidTypeInfo, PackedFile> infos) {
            foreach (PackedFile packed in pack.Files) {
                if (packed.FullPath.StartsWith("db")) {
                    AddFromPacked(infos, packed);
                }
            }
            return infos;
        }

        private void AddFromPacked(Dictionary<GuidTypeInfo, PackedFile> infos, PackedFile packed) {
            if (packed.Size != 0) {
                string type = DBFile.typename(packed.FullPath);
                DBFileHeader header = PackedFileDbCodec.readHeader(packed);
                infos[new GuidTypeInfo(header.GUID, type, header.Version)] = packed;
                if (string.IsNullOrEmpty(header.GUID)) {
                    int min = int.MaxValue;
                    minVersion.TryGetValue(type, out min);
                    min = Math.Min(min, header.Version);
                    minVersion[type] = min;
                    int max = 0;
                    maxVersion.TryGetValue(type, out max);
                    max = Math.Max(max, header.Version);
                    maxVersion[type] = max;
                }
            }
        }
    }
}

