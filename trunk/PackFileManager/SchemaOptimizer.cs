using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Filetypes;

namespace PackFileManager {
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
                foreach (string path in Directory.EnumerateFiles(PackDirectory, "*.pack")) {
                    PackFile pack = new PackFileCodec().Open (path);
                    List<GuidTypeInfo> infos = GetUsedTypes(pack);
                    
                    // add all infos we don't have yet
                    infos.ForEach(info => { if (!allUsed.Contains(info)) { allUsed.Add(info); } });
                }
                
                SortedDictionary<string, List<FieldInfo>> masterTypes = DBTypeMap.Instance.TypeMap;
                SortedDictionary<GuidTypeInfo, List<FieldInfo>> masterGuids = DBTypeMap.Instance.GuidMap;

                foreach(GuidTypeInfo info in allUsed) {
                    if (!string.IsNullOrEmpty(info.Guid)) {
                        AddSafe(info, guidMap, masterGuids);
                        continue;
                    }
                }
                foreach (string type in minVersion.Keys) {
                    List<FieldInfo> add = new List<FieldInfo>();
                    List<FieldInfo> addFrom;
                    if (masterTypes.TryGetValue(type, out addFrom)) {
                        if (type.Equals("units")) {
                            Console.WriteLine();
                        }
                        int min = minVersion[type];
                        int max = maxVersion[type];
                        addFrom.ForEach(field => {
                            if (field.StartVersion <= max && field.LastVersion >= min) {
                                add.Add(field);
                            }
                        });
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

        private void AddSafe<T>(T key, SortedDictionary<T, List<FieldInfo>> addTo, SortedDictionary<T, List<FieldInfo>> addFrom) {
            List<FieldInfo> addValue;
            if (addFrom.TryGetValue(key, out addValue) && !addTo.ContainsKey(key)) {
                addTo[key] = addValue;
            }
        }

        private List<GuidTypeInfo> GetUsedTypes(PackFile pack) {
            List<GuidTypeInfo> infos = new List<GuidTypeInfo>();
            foreach (PackedFile packed in pack.Files) {
                if (packed.FullPath.StartsWith("db")) {
                    AddFromPacked(infos, packed);
                }
            }
            return infos;
        }

        private void AddFromPacked(List<GuidTypeInfo> infos, PackedFile packed) {
            if (packed.Size != 0) {
                string type = DBFile.typename(packed.FullPath);
                DBFileHeader header = PackedFileDbCodec.readHeader(packed);
                infos.Add(new GuidTypeInfo(header.GUID, type, header.Version));
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

