using System;
using System.Collections;
using System.Collections.Generic;

namespace Filetypes {
    public abstract class ModelFile<T> {
        public DBFileHeader Header { get; set; }
        List<T> containedModels = new List<T>();
        public List<T> Models {
            get {
                return containedModels;
            }
        }
    }
    
    public abstract class EntryContainer<T> {
        List<T> entries = new List<T>();
        public List<T> Entries {
            get {
                return entries;
            }
        }
    }
    
    public class Coordinates : IEnumerable {
        public float XCoordinate {
            get;
            set;
        }
        public float YCoordinate {
            get;
            set;
        }
        public float ZCoordinate {
            get;
            set;
        }
        public IEnumerator GetEnumerator() {
            float[] angles = new float[] { XCoordinate, YCoordinate, ZCoordinate };
            return angles.GetEnumerator();
        }
    }

    public abstract class ModelEntry : IEnumerable {
        Coordinates angles1 = new Coordinates();
        public Coordinates Coordinates1 {
            get {
                return angles1;
            }
        }
        Coordinates angles2 = new Coordinates();
        public Coordinates Coordinates2 {
            get {
                return angles2;
            }
        }
        Coordinates angles3 = new Coordinates();
        public Coordinates Coordinates3 {
            get {
                return angles3;
            }
        }
        public IEnumerator GetEnumerator() {
            Coordinates[] angles = new Coordinates[] { angles1, angles2, angles3 };
            return angles.GetEnumerator();
        }
    }

    #region Buildings
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


    #region Naval
    public class NavalModelFile : ModelFile<NavalModel> { }
    
    public class NavalModel : EntryContainer<ShipPart> {
        public string ModelId { get; set; }
        public string RiggingLogicPath { get; set; }
        public bool Unknown { get; set; }
        public string RigidModelPath { get; set; }
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

    public class ShipPart : EntryContainer<PartEntry> {
        public string PartName { get; set; }
        public uint Unknown2 { get; set; }

    }
    public class PartEntry : ModelEntry {
        public uint Unknown {
            get; set;
        }
        
        public bool Coord1Tag {
            get; set;
        }
        public bool Coord2Tag {
            get; set;
        }
        public bool Coord3Tag {
            get; set;
        }
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
}

