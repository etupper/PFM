using System;
using System.IO;
using System.Collections.Generic;
using Common;

namespace Filetypes {
    #region Buildings Model
    public class BuildingModelFile : ModelFile<BuildingModel> {}
    
    public class BuildingModel : EntryContainer<BuildingModelEntry> {
        public string Name { get; set; }
        public string TexturePath { get; set; }
        public int Unknown { get; set; }
    }

    public class BuildingModelEntry : ModelEntry {        
        public string Name { get; set; }
        public int Unknown { get; set; }
        
        public override string ToString() {
            return string.Format("[BuildingModelEntry: Name={0}, Unknown={1}]", Name, Unknown);
        }
    }
    #endregion

    #region Building Model Codec
    /*
     * Building models codec.
     */
    public class BuildingModelCodec : ModelCodec<BuildingModel>  {
        private static BuildingModelCodec instance = new BuildingModelCodec();
        public static BuildingModelCodec Instance {
            get {
                return instance;
            }
        }
        protected override ModelFile<BuildingModel> CreateFile() {
            return new BuildingModelFile();
        }

        public override BuildingModel ReadModel(BinaryReader reader) {
            BuildingModel result = new BuildingModel {
                Name = IOFunctions.readCAString(reader),
                TexturePath = IOFunctions.readCAString(reader),
                Unknown = reader.ReadInt32()
            };

            IOFunctions.FillList(result.Entries, ReadBuildingEntry, reader);
            return result;
        }

        BuildingModelEntry ReadBuildingEntry(BinaryReader reader) {
            BuildingModelEntry entry = new BuildingModelEntry {
                Name = IOFunctions.readCAString(reader),
                Unknown = reader.ReadInt32()
            };
            ReadCoordinates(reader, entry);
            return entry;
        }
        protected override void WriteModel(BinaryWriter writer, BuildingModel model) {
            IOFunctions.writeCAString(writer, model.Name);
            IOFunctions.writeCAString(writer, model.TexturePath);
            writer.Write(model.Unknown);
        }
    }
    #endregion
 
}

