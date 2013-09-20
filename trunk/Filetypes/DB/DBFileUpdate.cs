using System;
using System.Collections.Generic;
using Common;

namespace Filetypes {
    /*
     * Class updating a db file to the highest version available for the file's type.
     * This is one of the reasons why the schema files have to be separated and released
     * per game, because each game has a different range of versions it can load at all
     * and we can't let a user update a table to a version the game doesn't even support.
     */
    public class DBFileUpdate {
        public delegate string GuidDeterminator(List<string> options);
        
        public GuidDeterminator DetermineGuid { get; set; }
        public string GetGuid(List<string> options) {
            if (DetermineGuid != null) {
                return DetermineGuid(options);
            }
            throw null;
        }
        
        // this could do with an update; since the transition to schema.xml,
        // we also know obsolete fields and can remove them,
        // and we can add fields in the middle instead of assuming they got appended.
        public void UpdatePackedFile(PackedFile packedFile) {
            string key = DBFile.Typename(packedFile.FullPath);
            if (DBTypeMap.Instance.IsSupported(key)) {
                PackedFileDbCodec codec = PackedFileDbCodec.FromFilename(packedFile.FullPath);
                int maxVersion = DBTypeMap.Instance.MaxVersion(key);
                DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
                if (header.Version < maxVersion) {
                    // found a more recent db definition; read data from db file
                    DBFile updatedFile = PackedFileDbCodec.Decode(packedFile);

                    TypeInfo dbFileInfo = updatedFile.CurrentType;
                    string guid;
                    TypeInfo targetInfo = GetTargetTypeInfo (key, maxVersion, out guid);
                    if (targetInfo == null) {
                        throw new Exception(string.Format("Can't decide new structure for {0} version {1}.", key, maxVersion));
                    }

                    // identify FieldInstances missing in db file
                    for (int i = dbFileInfo.Fields.Count; i < targetInfo.Fields.Count; i++) {
                        foreach (List<FieldInstance> entry in updatedFile.Entries) {
                            var field = targetInfo.Fields[i].CreateInstance();
                            entry.Add(field);
                        }
                    }
                    updatedFile.Header.GUID = guid;
                    updatedFile.Header.Version = maxVersion;
                    packedFile.Data = codec.Encode(updatedFile);
                }
            }
        }
        /*
         * Find the type info for the given type and version to update to.
         */
        TypeInfo GetTargetTypeInfo(string key, int maxVersion, out string guid) {
            TypeInfo targetInfo = null;
            List<string> newGuid = GetGuidsForInfo(key, maxVersion);
            guid = null;
            if (newGuid.Count == 0) {
                guid = "";
            } else if (newGuid.Count == 1) {
                guid = newGuid[0];
            } if (newGuid.Count > 1) {
                guid = DetermineGuid(newGuid);
            }
            
            if (guid != null) {
                targetInfo = DBTypeMap.Instance.GetVersionedInfo(key, maxVersion);
            }
            return targetInfo;
        }

        private List<string> GetGuidsForInfo(string type, int version) {
            List<string> result = new List<string>();
            foreach(GuidTypeInfo info in DBTypeMap.Instance.GuidMap.Keys) {
                if (info.Version == version && info.TypeName.Equals(type)) {
                    result.Add(info.Guid);
                }
            }
            return result;
        }
    }
}

