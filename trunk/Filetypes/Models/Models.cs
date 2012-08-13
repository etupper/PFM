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
    
    public class ModelContainer<E> where E : ModelEntry {
        List<E> entries = new List<E>();
        public List<E> Entries {
            get {
                return entries;
            }
        }
    }

    public class Angles : IEnumerable {
        public float XAngle {
            get;
            set;
        }
        public float YAngle {
            get;
            set;
        }
        public float ZAngle {
            get;
            set;
        }
        public IEnumerator GetEnumerator() {
            float[] angles = new float[] { XAngle, YAngle, ZAngle };
            return angles.GetEnumerator();
        }
    }

    public abstract class ModelEntry : IEnumerable {
        Angles angles1 = new Angles();
        public Angles Angles1 {
            get {
                return angles1;
            }
        }
        Angles angles2 = new Angles();
        public Angles Angles2 {
            get {
                return angles2;
            }
        }
        Angles angles3 = new Angles();
        public Angles Angles3 {
            get {
                return angles3;
            }
        }
        public IEnumerator GetEnumerator() {
            Angles[] angles = new Angles[] { angles1, angles2, angles3 };
            return angles.GetEnumerator();
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
 
    
    #region Naval
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
        public List<int> unknown = new List<int>();
        public List<int> Unknown {
            get {
                return unknown;
            }
        }
    }
    #endregion
}

