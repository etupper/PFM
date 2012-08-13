using System;
using System.Collections.Generic;

using Angles = System.Collections.Generic.List<float>;

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
    
    public class ModelContainer<E> where E : ModelEntry {
        List<E> entries = new List<E>();
        public List<E> Entries {
            get {
                return entries;
            }
        }
    }

    public abstract class ModelEntry {
        public ModelEntry() {
            for (int i = 0; i < 3; i++) {
                allAngles.Add(AllNull());
            }
        }
        List<Angles> allAngles = new List<Angles>();
        public List<Angles> Angles {
            get {
                return allAngles;
            }
        }
        public static Angles AllNull() {
            Angles result = new List<float>();
            for(int i = 0; i < 3; i++) {
                result.Add(0f);
            }
            return result;
        }
    }

    #region Buildings
    public class BuildingModelFile : ModelFile<BuildingModel> {}
    
    public class BuildingModel : ModelContainer<BuildingModelEntry> {
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
 
    
    #region
    public class NavalModelFile : ModelFile<NavalModel> { }
    
    public class NavalModel : ModelContainer<NavalEntry> {
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
    }
    public class NavalCam {
        public string Name { get; set; }
        List<uint> data = new List<uint>();
        public List<uint> Data {
            get {
                return data;
            }
        }
        public NavalCam() {
            for(int i = 0; i < 16; i++) {
                data.Add(0);
            }
        }
    }
    public class NavalEntry : ModelEntry {
        public int Unknown1 {
            get; set;
        }
        public int Unknown2 {
            get; set;
        }
        public int Unknown3 {
            get; set;
        }
    }
    #endregion
}

