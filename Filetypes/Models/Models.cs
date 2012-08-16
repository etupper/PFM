using System;
using System.Collections;
using System.Collections.Generic;

namespace Filetypes {
    #region Base Classes
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
    #endregion
    
    #region Coordinates
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
        public float this[int index] {
            get {
                float[] angles = new float[] { XCoordinate, YCoordinate, ZCoordinate };
                return angles[index];
            }
            set {
                float[] angles = new float[] { XCoordinate, YCoordinate, ZCoordinate };
                angles[index] = value;
            }
        }
        public IEnumerator GetEnumerator() {
            float[] angles = new float[] { XCoordinate, YCoordinate, ZCoordinate };
            return angles.GetEnumerator();
        }
    }
    public class TaggedCoordinate<T> : Coordinates {
        T Tag {
            get; set;
        }
    }
    #endregion
}

