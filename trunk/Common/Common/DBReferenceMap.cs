using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common
{
	public class DBReferenceMap
	{
        static Regex REFERENCE_LINE = new Regex("([^:]*):([^ ]*) - ([^:]*):(.*)");
        
        public static readonly DBReferenceMap Instance = new DBReferenceMap();

        public Dictionary<string, List<TableReference>> references = new Dictionary<string, List<TableReference>>();
        Dictionary<KeyValuePair<string,int>, SortedSet<string>> valueCache = new Dictionary<KeyValuePair<string,int>,SortedSet<string>>();
        PackFile lastPack = null;

        private DBReferenceMap() { }

        PackFile LastPack
        {
            get { return lastPack; }
            set
            {
                if ((value != null && lastPack != null) && 
                    (value.Filepath != lastPack.Filepath))
                {
                    // clear cache when using another pack file
                    valueCache.Clear();
                }
                lastPack = value;
            }
        }

        public void load(string directory)
        {
            string filename = Path.Combine(directory, DBTypeMap.DB_FILE_TYPE_DIR_NAME, "references.txt");
            references.Clear();
            LastPack = null;
            foreach (string line in File.ReadAllLines(filename))
            {
                try
                {
                    if (line.StartsWith("#"))
                        continue;
                    Match m = REFERENCE_LINE.Match(line);
                    TableReference reference = new TableReference { 
                        fromMap = m.Groups[1].Value, fromIndex = int.Parse(m.Groups[2].Value),
                        toMap = m.Groups[3].Value, toIndex = int.Parse(m.Groups[4].Value)
                    };
                    List<TableReference> referenceList = null;
                    if (!references.TryGetValue(reference.fromMap, out referenceList))
                    {
                        referenceList = new List<TableReference>();
                        references.Add(reference.fromMap, referenceList);
                    }
                    referenceList.Add(reference);
                }
                catch (Exception x) {
                    Console.WriteLine("{0}", x.Message);
                }
            }
        }
        public void validateReferences(string directory, PackFile pack)
        {
            LastPack = pack;
            // verify dependencies
            foreach (string fromMap in references.Keys)
            {
                foreach (TableReference reference in references[fromMap])
                {
                    if (reference.fromMap == "ancillary_to_effects")
                    {
                        Console.WriteLine("ok");
                    }
                    SortedSet<string> values = collectValues(reference.fromIndex, reference.fromMap, pack);
                    SortedSet<string> allowed = collectValues(reference.toIndex, reference.toMap, pack);
                    if (values != null && allowed != null)
                    {
                        foreach (string val in values)
                        {
                            if (val != "" && !allowed.Contains(val))
                            {
                                Console.WriteLine("value '{0}' in {1}:{2} does not fulfil reference {3}:{4}",
                                    val, reference.fromMap, reference.fromIndex, reference.toMap, reference.toIndex);
                            }
                        }
                    }
                }
            }
        }
        SortedSet<string> collectValues(int index, string reference, PackFile pack)
        {
            if (pack != lastPack)
            {
                lastPack = pack;
            }
            string dbFileName = string.Format(@"db\{0}_tables\{0}", reference);

            SortedSet<string> result = new SortedSet<string>();
            if (valueCache.TryGetValue(new KeyValuePair<string, int>(dbFileName, index), out result))
            {
                return result;
            }
            foreach (PackedFile file in pack.FileList)
            {
                if (file.Filepath == dbFileName)
                {
                    result = new SortedSet<string>();
                    DBFile dbFile = new DBFile(file, DBTypeMap.Instance[reference].ToArray());
                    foreach (List<FieldInstance> entry in dbFile.Entries)
                    {
                        string toAdd = entry[index].Value;
                        if (toAdd != null)
                        {
                            result.Add(toAdd);
                        }
                    }
                }
            }
            valueCache.Add(new KeyValuePair<string, int>(dbFileName, index), result);
            return result;
        }

        public SortedSet<string> resolveFromPackFile(string key, int index, PackFile packFile)
        {
            LastPack = packFile;
            SortedSet<string> result = null;
            if (references.ContainsKey(key))
            {
                TableReference toResolve = null;
                foreach (TableReference tr in references[key])
                {
                    if (tr.fromIndex == index)
                    {
                        toResolve = tr;
                        break;
                    }
                }
                if (toResolve == null) 
                    return null;

                result = collectValues(toResolve.toIndex, toResolve.toMap, packFile);
            }
            return result;
        }
	}

    public class TableReference
    {
//        public string Name { get; set; }
        public string fromMap { get; set; }
        public int fromIndex { get; set; }
        public string toMap { get; set; }
        public int toIndex { get; set; }
    }
}
