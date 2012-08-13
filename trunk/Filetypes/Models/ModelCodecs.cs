using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace Filetypes {
    public abstract class ModelCodec<M, E> : Codec<ModelFile<M>>
        where E : ModelEntry
    where M : ModelContainer<E> {
        public ModelFile<M> Decode(Stream stream) {
            ModelFile<M> result = null;
            using (var reader = new BinaryReader(stream)) {
                result = CreateFile();
                try {
                    result.Header = PackedFileDbCodec.readHeader(reader);
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
                    uint itemCount = reader.ReadUInt32();
#if DEBUG
                    // Console.WriteLine("Reading {0} entries", itemCount);
#endif
                    for (uint j = 0; j < itemCount; j++) {
                        try {
                            E entry = ReadEntry(reader);
#if DEBUG
                        // Console.WriteLine("Read entry {0}: {1}", j, entry);
#endif
                            model.Entries.Add(entry);
                        } catch (Exception ex) {
                            throw new InvalidDataException(string.Format("Failed to read entry {1} in {2}", j, model), ex);
                        }
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
        
        protected void WriteAngles(BinaryWriter writer, E entry) {
            entry.Angles.ForEach(a => {
                a.ForEach(f => { 
                    writer.Write (f); 
                });
            });
        }
        protected void ReadAngles(BinaryReader reader, E entry) {
            foreach(List<float> angle in entry.Angles) {
                for(int i = 0; i < angle.Count; i++) {
                    angle[i] = reader.ReadSingle();
                }
            }
        }
    }
    
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
            ReadAngles(reader, entry);
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
            WriteAngles(writer, entry);
            entry.Angles.ForEach(a =>  {
                a.ForEach(f => {
                    writer.Write (f);
                });
            });
        }
    }
 
    /*
     * Naval models codec.
     */
    public class NavalModelCodec : ModelCodec<NavalModel, NavalEntry> {
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
            NavalModel result = new NavalModel {
                ModelId = IOFunctions.readCAString(reader),
                RiggingLogicPath = IOFunctions.readCAString(reader),
                Unknown = reader.ReadBoolean(),
                RigidModelPath = IOFunctions.readCAString(reader)
            };
            int camCount = reader.ReadInt32();
            for(int camIndex = 0; camIndex < camCount; camIndex++) {
                NavalCam cam = new NavalCam {
                    Name = IOFunctions.readCAString(reader)
                };
                for (int dataIndex = 0; dataIndex < cam.Data.Count; dataIndex++) {
                    cam.Data[dataIndex] = reader.ReadUInt32();
                }
                result.NavalCams.Add(cam);
            }
            return result;
        }
        protected override NavalEntry ReadEntry(BinaryReader reader) {
            // skip the item number, we don't care... is just its index in the list
            reader.ReadInt32();
            NavalEntry entry = new NavalEntry();
            ReadAngles(reader, entry);
            entry.Unknown1 = reader.ReadInt32();
            entry.Unknown2 = reader.ReadInt32();
            entry.Unknown3 = reader.ReadInt32();
            return entry;
        }
        
        protected override void WriteModel(BinaryWriter writer, NavalModel model) {
            IOFunctions.writeCAString(writer, model.ModelId);
            IOFunctions.writeCAString(writer, model.RiggingLogicPath);
            writer.Write (model.Unknown);
            IOFunctions.writeCAString(writer, model.RigidModelPath);
            writer.Write(model.NavalCams.Count);
            model.NavalCams.ForEach(cam => {
                IOFunctions.writeCAString(writer, cam.Name);
                cam.Data.ForEach(b => {
                    writer.Write(b);
                });
            });
        }
        protected override void WriteEntry(BinaryWriter writer, NavalEntry entry, int index) {
            writer.Write(index);
            WriteAngles(writer, entry);
            writer.Write(entry.Unknown1);
            writer.Write(entry.Unknown2);
            writer.Write(entry.Unknown3);
        }
    }
}

