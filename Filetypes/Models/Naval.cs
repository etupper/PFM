using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace Filetypes {
    #region Naval Model
    public class NavalModelFile : ModelFile<NavalModel> { }
    public class ShipPartFile : ModelFile<ShipPart> { }
    
    public class NavalModel : EntryContainer<ShipPart> {
        public string ModelId { get; set; }
        public string RiggingLogicPath { get; set; }
        public bool Unknown { get; set; }
        public string RigidModelPath { get; set; }
        public float UnknownFloat { get; set; }
        
        private List<NavalCam> cams = new List<NavalCam>();
        public List<NavalCam> NavalCams {
            get {
                return cams;
            }
        }
        
        private List<PartPositionInfo> partPositions = new List<PartPositionInfo>();
        public List<PartPositionInfo> PositionInfos {
            get {
                return partPositions;
            }
        }

        /* Four trailing ints.. probably references */
        public uint Side1 {
            get; set;
        }
        public uint Side2 {
            get; set;
        }
        public uint Side3 {
            get; set;
        }
        public uint Side4 {
            get; set;
        }
    }
    
    public class NavalCam : EntryContainer<uint> {
        public string Name { get; set; }
    }
    
    public class PartPositionInfo : ModelEntry {
        List<uint> unknown = new List<uint>();
        public List<uint> Unknown {
            get {
                return unknown;
            }
        }
    }

    #region Ship Parts
    public class ShipPart : EntryContainer<PartEntry> {
        public string PartName { get; set; }
        public uint Unknown2 { get; set; }

    }
    public class PartEntry : ModelEntry {
        public uint Unknown {
            get; set;
        }

        /* Four trailing ints.. probably references */
        public uint Side1 {
            get; set;
        }
        public uint Side2 {
            get; set;
        }
        public uint Side3 {
            get; set;
        }
        public uint Side4 {
            get; set;
        }
        
        /* The bools seem to be associated to the coordinate blocks */
        public bool Coord1Tag {
            get; set;
        }
        public bool Coord2Tag {
            get; set;
        }
        public bool Coord3Tag {
            get; set;
        }
        public void FlagCoordinate(Coordinates toFlag, bool flagAs) {
            if (toFlag == Coordinates1) {
                Coord1Tag = flagAs;
            } else if (toFlag == Coordinates2) {
                Coord2Tag = flagAs;
            } else if (toFlag == Coordinates3) {
                Coord3Tag = flagAs;
            }
        }
        public bool GetCoordinateFlag(Coordinates getFor) {
            if (getFor == Coordinates1) {
                return Coord1Tag;
            } else if (getFor == Coordinates2) {
                return Coord2Tag;
            } else if (getFor == Coordinates3) {
                return Coord3Tag;
            }
            throw new InvalidOperationException();
        }
    }
    #endregion
    #endregion

    #region Ship Part Codec
    /*
     * The models codec for earlier games; only contains ship parts.
     */
    public class ShipPartCodec : ModelCodec<ShipPart> {
        private static ShipPartCodec instance = new ShipPartCodec();
        public static ShipPartCodec Instance {
            get {
                return instance;
            }
        }
        
        protected override ModelFile<ShipPart> CreateFile() {
            return new ShipPartFile();
        }
        
        public override ShipPart ReadModel(BinaryReader reader) {
#if DEBUG
            Console.WriteLine("Reading Ship Part at {0:x}", reader.BaseStream.Position);
#endif
            string name = IOFunctions.readCAString(reader);
            ShipPart part = new ShipPart {
                PartName = name,
                Unknown2 = reader.ReadUInt32()
            };

            FillList(part.Entries, ReadPartEntry, reader);
            return part;
        }

        public PartEntry ReadPartEntry(BinaryReader reader) {
#if DEBUG
            //Console.WriteLine("Reading Ship Part entry at {0}", reader.BaseStream.Position);
#endif
            PartEntry part = new PartEntry {
                Unknown = reader.ReadUInt32()
            };
            Coordinates[] coords = { part.Coordinates1, part.Coordinates2, part.Coordinates3 };
            foreach(Coordinates coord in coords) {
                ReadCoordinate(coord, reader);
                part.FlagCoordinate(coord, reader.ReadBoolean());
            }
            part.Side1 = reader.ReadUInt32();
            part.Side2 = reader.ReadUInt32();
            part.Side3 = reader.ReadUInt32();
            part.Side4 = reader.ReadUInt32();
            return part;
        }
        
        protected override void WriteModel(BinaryWriter writer, ShipPart model) {
            throw new NotImplementedException ();
        }
    }
    #endregion
    
    #region Naval Model Codec
    /*
     * Naval models codec.
     */
    public class NavalModelCodec : ModelCodec<NavalModel> {
        private static readonly NavalModelCodec instance = new NavalModelCodec();
        public static NavalModelCodec Instance {
            get {
                return instance;
            }
        }
        private ShipPartCodec partReader = new ShipPartCodec();
        
        protected override ModelFile<NavalModel> CreateFile() {
            return new NavalModelFile();
        }
        public override NavalModel ReadModel(BinaryReader reader) {
#if DEBUG
            long readModelStart = reader.BaseStream.Position;
            Console.WriteLine("Model starting at {0:x}", readModelStart);
#endif
            NavalModel result = new NavalModel {
                ModelId = IOFunctions.readCAString(reader),
                RiggingLogicPath = IOFunctions.readCAString(reader),
                Unknown = reader.ReadBoolean(),
                RigidModelPath = IOFunctions.readCAString(reader)
            };
   
            result.NavalCams.AddRange(ReadList(reader, ReadNavalCam, false));
            result.PositionInfos.AddRange(ReadList(reader, ReadPositionEntry));
            FillList(result.Entries, partReader.ReadModel, reader, false);
   
            // result.UnknownFloat = reader.ReadSingle();
//            result.Side1 = reader.ReadUInt32();
//            result.Side2 = reader.ReadUInt32();
//            result.Side3 = reader.ReadUInt32();
//            result.Side4 = reader.ReadUInt32();
            return result;
        }
        
        NavalCam ReadNavalCam(BinaryReader reader) {
#if DEBUG
            // Console.WriteLine("Reading cams starting at {0}", reader.BaseStream.Position);
#endif
            NavalCam cam = new NavalCam {
                Name = IOFunctions.readCAString(reader)
            };
            for (int dataIndex = 0; dataIndex < 16; dataIndex++) {
                cam.Entries.Add(reader.ReadUInt32());
            }
            return cam;
        }

        PartPositionInfo ReadPositionEntry(BinaryReader reader) {
#if DEBUG
            //Console.WriteLine("Reading position entry at {0}", reader.BaseStream.Position);
#endif
            PartPositionInfo entry = new PartPositionInfo();
            ReadCoordinates(reader, entry);
            
            int moreEntries = reader.ReadInt32();
            for (int i = 0; i < moreEntries; i++) {
                entry.Unknown.Add(reader.ReadUInt32());
            }
            Console.Out.Flush();
            return entry;
        }
        
        public ShipPart ReadPartEntry(BinaryReader reader) {
            ShipPart part = partReader.ReadModel(reader);
            part.Entries.AddRange(ReadList(reader, partReader.ReadPartEntry));
            return part;
        }
        
        protected override void WriteModel(BinaryWriter writer, NavalModel model) {
//            IOFunctions.writeCAString(writer, model.ModelId);
//            IOFunctions.writeCAString(writer, model.RiggingLogicPath);
//            writer.Write (model.Unknown);
//            IOFunctions.writeCAString(writer, model.RigidModelPath);
//            writer.Write(model.NavalCams.Count);
//            model.NavalCams.ForEach(cam => {
//                IOFunctions.writeCAString(writer, cam.Name);
//                cam.Entries.ForEach(b => {
//                    writer.Write(b);
//                });
//            });
            throw new NotSupportedException();
        }
    }
    #endregion
}

