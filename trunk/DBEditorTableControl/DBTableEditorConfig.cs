using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DBTableControl
{
    public class DBTableEditorConfig
    {
        [XmlElement]
        public bool FreezeKeyColumns { get; set; }

        [XmlElement]
        public bool UseComboBoxes { get; set; }

        [XmlElement]
        public bool ShowAllColumns { get; set; }

        public FakeDictionary<string, List<string>> HiddenColumns { get; set; }

        [XmlElement]
        public string ImportExportDirectory { get; set; }

        public DBTableEditorConfig()
        {
            FreezeKeyColumns = true;
            UseComboBoxes = true;
            ShowAllColumns = false;
            HiddenColumns = new FakeDictionary<string, List<string>>();
            ImportExportDirectory = "";
        }

        public void Load(string file = "Config\\DBTableEditorConfig.xml")
        {
            DBTableEditorConfig loadedconfig = new DBTableEditorConfig();

            if (File.Exists(file))
            {
                XmlSerializer xs = new XmlSerializer(typeof(DBTableEditorConfig));

                using (Stream s = File.Open(file, FileMode.Open))
                {
                    loadedconfig = (DBTableEditorConfig)xs.Deserialize(s);
                }
            }

            FreezeKeyColumns = loadedconfig.FreezeKeyColumns;
            UseComboBoxes = loadedconfig.UseComboBoxes;
            ShowAllColumns = loadedconfig.ShowAllColumns;
            HiddenColumns = loadedconfig.HiddenColumns;
            ImportExportDirectory = loadedconfig.ImportExportDirectory;
        }

        public void Save(string file = "Config\\DBTableEditorConfig.xml")
        {
            XmlSerializer xs = new XmlSerializer(typeof(DBTableEditorConfig));

            if (!Directory.Exists("Config"))
            {
                Directory.CreateDirectory("Config");
            }

            using (Stream s = File.Create(file))
            {
                xs.Serialize(s, this);
            }
        }
    }

    [Serializable]
    public class FakeDictionary<TKey, TValue>
    {
        [XmlElement("Entry")]
        public List<KeyValuePair<TKey, TValue>> Entries { get; set; }

        public FakeDictionary()
        {
            Entries = new List<KeyValuePair<TKey, TValue>>();
        }

        public TValue this[TKey key]
        {
            get
            {
                if (ContainsKey(key))
                {
                    return Entries[Entries.IndexOf(Entries.Single(n => n.Key.Equals(key)))].Value;
                }
                else
                {
                    throw new ArgumentException(String.Format("Dictionary {1} does not contain key: {0}", key, this));
                }
            }

            set
            {
                Entries[Entries.IndexOf(Entries.Single(n => n.Key.Equals(key)))].Value = value;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> newentry)
        {
            if (Entries.Any(n => n.Key.Equals(newentry.Key)))
            {
                throw new ArgumentException(String.Format("Dictionary {1} already contains key: {0}", newentry.Key, this));
            }
            else
            {
                Entries.Add(newentry);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return Entries.Any(n => n.Key.Equals(key));
        }

        public void Clear()
        {
            Entries.Clear();
        }


        public void Sort()
        {
            Entries.Sort();
        }
    }

    [Serializable]
    public class KeyValuePair<TKey, TValue> :IComparable
    {
        [XmlAttribute]
        public TKey Key { get; set; }

        [XmlElement]
        public TValue Value { get; set; }

        public KeyValuePair()
        {

        }

        public KeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public int CompareTo(object tocompare)
        {
            if (tocompare is KeyValuePair<TKey, TValue>)
            {
                return CompareTo((KeyValuePair<TKey, TValue>)tocompare);
            }
            else
            {
                throw new ArgumentException("Object is not of the same type.");
            }
        }

        public int CompareTo(KeyValuePair<TKey, TValue> tocompare)
        {
            return Key.ToString().CompareTo(tocompare.Key.ToString());
        }
    }
}
