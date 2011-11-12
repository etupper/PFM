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

        private DBReferenceMap() { }

        public void load(string directory)
        {
            string filename = Path.Combine(directory, DBTypeMap.DB_FILE_TYPE_DIR_NAME, "references.txt");
            references.Clear();
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
            string dbFileName = string.Format(@"db\{0}_tables\{0}", reference);
            foreach (PackedFile file in pack.FileList)
            {
                if (file.Filepath == dbFileName)
                {
                    DBFile dbFile = new DBFile(file, DBTypeMap.Instance[reference].ToArray());
                    SortedSet<string> result = new SortedSet<string>();
                    foreach (List<FieldInstance> entry in dbFile.Entries)
                    {
                        string toAdd = entry[index].Value;
                        if (toAdd != null)
                        {
                            result.Add(toAdd);
                        }
                    }
                    return result;
                }
            }
            return null;
        }

        public SortedSet<string> resolveFromPackFile(string key, int index, PackFile packFile)
        {
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

                string dbFileName = string.Format(@"db\{0}_tables\{0}", toResolve.toMap);
                foreach (PackedFile file in packFile.FileList)
                {
                    if (file.Filepath == dbFileName)
                    {
                        DBFile dbFile = new DBFile(file, DBTypeMap.Instance[toResolve.toMap].ToArray());
                        SortedSet<string> result = new SortedSet<string>();
                        foreach (List<FieldInstance> entry in dbFile.Entries)
                        {
                            string toAdd = entry[toResolve.toIndex].Value;
                            if (toAdd != null)
                            {
                                result.Add(toAdd);
                            }
                        }
                        return result;
                    }
                }
            }
            return null;
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
