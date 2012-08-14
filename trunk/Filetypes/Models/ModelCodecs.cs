using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Common;

namespace Filetypes {
    public abstract class ModelCodec<M, E> : Codec<ModelFile<M>> 
    where M : EntryContainer<E> {
        
        public ModelFile<M> Decode(Stream stream) {
            ModelFile<M> result = null;
            using (var reader = new BinaryReader(stream)) {
                result = CreateFile();
                try {
                    result.Header = PackedFileDbCodec.readHeader(reader);
                    // LastReadHeader = result.Header;
#if DEBUG
                // Console.WriteLine("Reading {0} models", result.Header.EntryCount);
#endif
                } catch (Exception ex) {
                    throw new InvalidDataException(string.Format("Failed to read header from {0}", stream), ex);
                }
                for (int modelIndex = 0; modelIndex < result.Header.EntryCount; modelIndex++) {
#if DEBUG
                    // Console.WriteLine("Reading model {0}", modelIndex);
#endif
                    M model;
                    try {
                        model = ReadModel(reader);
                    } catch (Exception ex) {
                        throw new InvalidDataException(string.Format("Failed to read model data {0}", modelIndex), ex);
                    }
                    try {
                        List<E> entries = new List<E>(ReadList(reader, ReadEntry, false));
                        model.Entries.AddRange(entries);
                    } catch (Exception ex) {
                        throw new InvalidDataException(string.Format("Failed to entries for model {0}", modelIndex), ex);
                    }
                    
                    result.Models.Add(model);
                }
            }
            return result;
        }
        public void Encode(Stream stream, ModelFile<M> file) {
            file.Header.EntryCount = (uint) file.Models.Count;
            using (var writer = new BinaryWriter(stream)) {
                PackedFileDbCodec.WriteHeader(writer, file.Header);
                file.Models.ForEach(toEncode => {
                    WriteModel(writer, toEncode);
                    writer.Write((uint) toEncode.Entries.Count);
                    for (int i = 0; i < toEncode.Entries.Count; i++) {
                        WriteEntry(writer, toEncode.Entries[i], i);
                    }
                });
            }
        }
        protected abstract ModelFile<M> CreateFile();
        protected abstract M ReadModel(BinaryReader reader);
        protected abstract E ReadEntry(BinaryReader reader);
        
        protected abstract void WriteModel(BinaryWriter writer, M model);
        protected abstract void WriteEntry(BinaryWriter writer, E entry, int index);
        
        protected void WriteCoordinates(BinaryWriter writer, IEnumerable entry) {
            foreach(Coordinates angle in entry) {
                foreach(float f in angle) {
                    writer.Write (f); 
                }
            }
        }
        protected void ReadCoordinates(BinaryReader reader, ModelEntry entry) {
            foreach (Coordinates angle in entry) {
                ReadCoordinate(angle, reader);
            }
        }
        protected virtual void ReadCoordinate(Coordinates coordinates, BinaryReader reader) {
            coordinates.XCoordinate = reader.ReadSingle();
            coordinates.YCoordinate = reader.ReadSingle();
            coordinates.ZCoordinate = reader.ReadSingle();
        }

        public delegate T ItemReader<T>(BinaryReader reader);
        public static List<T> ReadList<T>(BinaryReader reader, ItemReader<T> readItem, bool skipIndex = true) {
            List<T> result = new List<T>();
            int itemCount = reader.ReadInt32();
            for (int i = 0; i < itemCount; i++) {
                try {
                    if (skipIndex) {
                        reader.ReadInt32();
                    }
                    result.Add(readItem(reader));
                } catch (Exception ex) {
                    throw new InvalidDataException(string.Format("Failed to read item {0}", i), ex);
                }
            }
            return result;
        }
    }
        
    
    #region Building Model Codec
    /*
     * Building models codec.
     */
    public class BuildingModelCodec : ModelCodec<BuildingModel, BuildingModelEntry>  {
        private static BuildingModelCodec instance = new BuildingModelCodec();
        public static BuildingModelCodec Instance {
            get {
                return instance;
            }
        }
        protected override ModelFile<BuildingModel> CreateFile() {
            return new BuildingModelFile();
        }
        protected override BuildingModel ReadModel(BinaryReader reader) {
            return new BuildingModel {
                Name = IOFunctions.readCAString(reader),
                TexturePath = IOFunctions.readCAString(reader),
                Unknown = reader.ReadInt32()
            };
        }
        protected override BuildingModelEntry ReadEntry(BinaryReader reader) {
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
        protected override void WriteEntry(BinaryWriter writer, BuildingModelEntry entry, int unused) {
            IOFunctions.writeCAString(writer, entry.Name);
            writer.Write(entry.Unknown);
            WriteCoordinates(writer, entry);
        }
    }
    #endregion
 
    /*
     * Naval models codec.
     */
    public class NavalModelCodec : ModelCodec<NavalModel, ShipPart> {
        private static readonly NavalModelCodec instance = new NavalModelCodec();
        public static NavalModelCodec Instance {
            get {
                return instance;
            }
        }
        
        protected override ModelFile<NavalModel> CreateFile() {
            return new NavalModelFile();
        }
        protected override NavalModel ReadModel(BinaryReader reader) {
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
   
#if DEBUG
            Console.WriteLine("read ship model {0}, {1}, {2}, {3}", 
                              result.ModelId, result.RiggingLogicPath, result.Unknown, result.RigidModelPath);
            Console.Out.Flush();
#endif
            result.NavalCams.AddRange(ReadList(reader, ReadNavalCam, false));
            result.PositionInfos.AddRange(ReadList(reader, ReadPositionEntry));

            return result;
        }
        
        NavalCam ReadNavalCam(BinaryReader reader) {
#if DEBUG
            Console.WriteLine("Reading cams starting at {0}", reader.BaseStream.Position);
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
            Console.WriteLine("Reading position entry at {0}", reader.BaseStream.Position);
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
        
        protected override ShipPart ReadEntry(BinaryReader reader) {
#if DEBUG
            Console.WriteLine("Reading Ship Part at {0}", reader.BaseStream.Position);
#endif
            string name = IOFunctions.readCAString(reader);
            ShipPart part = new ShipPart {
                PartName = name,
                Unknown2 = reader.ReadUInt32()
            };
            Console.WriteLine("Reading ship part {0}", part.PartName);
            part.Entries.AddRange(ReadList(reader, ReadPartEntry));
            
            return part;
        }
        private PartEntry ReadPartEntry(BinaryReader reader) {
#if DEBUG
            Console.WriteLine("Reading Ship Part entry at {0}", reader.BaseStream.Position);
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
        
        protected override void WriteModel(BinaryWriter writer, NavalModel model) {
            IOFunctions.writeCAString(writer, model.ModelId);
            IOFunctions.writeCAString(writer, model.RiggingLogicPath);
            writer.Write (model.Unknown);
            IOFunctions.writeCAString(writer, model.RigidModelPath);
            writer.Write(model.NavalCams.Count);
            model.NavalCams.ForEach(cam => {
                IOFunctions.writeCAString(writer, cam.Name);
                cam.Entries.ForEach(b => {
                    writer.Write(b);
                });
            });
            throw new NotSupportedException();
        }
        protected override void WriteEntry(BinaryWriter writer, ShipPart entry, int index) {
            throw new NotSupportedException();
        }
    }
}

