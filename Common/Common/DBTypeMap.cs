using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Common
{
	public class DBTypeMap
	{
        static Regex DB_FILE_TYPE_PATTERN = new Regex("DBFileTypes_([0-9]*).txt");
        public static readonly string DB_FILE_TYPE_DIR_NAME = "DBFileTypes";
        public static readonly string DB_TYPE_USER_DIR_NAME = "DBFileTypes_user";

        private SortedDictionary<string, List<TypeInfo>> typeMap;

        public static readonly DBTypeMap Instance = new DBTypeMap();

        private DBTypeMap()
        {
            // prevent instantiation
        }

        public void initializeTypeMap(string basePath) {
            typeMap = loadDbFileTypes(basePath);
        }
        public void loadFromXsd(string xsdFile)
        {
            typeMap = new XsdParser(xsdFile).loadXsd();
        }

        public void saveToFile(string directory)
        {
            // never overwrite downloaded files, use another subdirectory
            string dir = Path.Combine(directory, DB_TYPE_USER_DIR_NAME);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            try
            {
                Dictionary<int, List<TypeInfo>> versionToInfos = new Dictionary<int, List<TypeInfo>>();

                // collect all typeinfo lists, sorted by version number
                foreach (string key in typeMap.Keys)
                {
                    List<TypeInfo> infos = typeMap[key];
                    for (int i = 0; i < infos.Count; i++)
                    {
                        if (infos[i] != null)
                        {
                            List<TypeInfo> addTo = retrieveOrAdd<int>(versionToInfos, i);
                            addTo.Add(infos[i]);
                        }
                    }
                }
                Comparer<TypeInfo> comparer = new TypeInfoComparer();
                foreach (int version in versionToInfos.Keys)
                {
                    StreamWriter writer = new StreamWriter(Path.Combine(dir, "DBFileTypes_" + version + ".txt"));
                    List<TypeInfo> infos = versionToInfos[version];
                    infos.Sort(comparer);
                    foreach (TypeInfo info in infos)
                    {
                        // header: table name, tab, first encoded field
                        writer.Write(string.Format("{0}\t{1}", info.name, encodeField(info.fields[0])));
                        for (int i = 1; i < info.fields.Count; i++)
                        {
                            // semicolon at eol is marker that there are more fields to come
                            writer.WriteLine(";");
                            writer.Write(string.Format("{0}", encodeField(info.fields[i])));
                        }
                        // make file more readable by separating entries
                        for (int i = 0; i < 3; i++) writer.WriteLine();
                    }
                    writer.Close();
                }
            }
            catch (Exception x)
            {
                Directory.Move(dir, Path.Combine(directory, DB_TYPE_USER_DIR_NAME + ".save"));
                throw x;
            }
        }
        string encodeField(FieldInfo info) {
            string result = string.Format("{0},{1}", info.name, info.type);
            switch(info.modifier) {
                case FieldInfo.Modifier.NextFieldRepeats:
                    result += ",*";
                    break;
                case FieldInfo.Modifier.NextFieldIsConditional:
                    result += ",1";
                    break;
            }
            return result;
        }
        private static List<TypeInfo> retrieveOrAdd<T>(IDictionary<T, List<TypeInfo>> dict, T key)
        {
            List<TypeInfo> list;
            if (!dict.TryGetValue(key, out list))
            {
                list = new List<TypeInfo>();
                dict.Add(key, list);
            }
            return list;
        }

        private static SortedDictionary<string, List<TypeInfo>> loadDbFileTypes(string basePath)
        {
            SortedDictionary<string, List<TypeInfo>> result = new SortedDictionary<string, List<TypeInfo>>();
            int headerVersion = 0;

            // prefer loading from user directory
            string path = Path.Combine(basePath, DB_TYPE_USER_DIR_NAME);
            if (!Directory.Exists(path))
            {
                path = Path.Combine(basePath, DB_FILE_TYPE_DIR_NAME);
            }

            // find the highest header version
            string[] matchingFiles = Directory.GetFiles(path, "DBFileTypes_*");
            foreach (string file in matchingFiles)
            {
                if (DB_FILE_TYPE_PATTERN.IsMatch(file))
                {
                    int version = -1;
                    if (int.TryParse(DB_FILE_TYPE_PATTERN.Match(file).Groups[1].Value, out version))
                    {
                        SortedDictionary<string, TypeInfo> fromFile = getTypeMapFromFile(file);
                        foreach (string key in fromFile.Keys)
                        {
                            List<TypeInfo> addTo = retrieveOrAdd<string>(result, key);
                            if (addTo.Count < version+1)
                            {
                                for (int i = addTo.Count; i < version+1; i++)
                                {
                                    addTo.Add(null);
                                }
                            }
                            addTo[version] = fromFile[key];
                        }
                    }
                    headerVersion = Math.Max(headerVersion, version);
                }
            }
            return result;
        }

        public int HighestHeaderVersion
        {
            get
            {
                return typeMap.Count;
            }
        }

        public List<string> DBFileTypes
        {
            get
            {
                List<string> result = new List<string>(typeMap.Keys);
                result.Sort();
                return result;
            }
        }

        public List<TypeInfo> this[string key]
        {
            get
            {
                List<TypeInfo> result = null;
                typeMap.TryGetValue(key, out result);
                return result;
            }
        }

        private static SortedDictionary<string, TypeInfo> getTypeMapFromFile(string filepath)
        {
            string[] lines = File.ReadAllLines(filepath);
            return parseTypeMap(lines);
        }

        private static SortedDictionary<string, TypeInfo> parseTypeMap(string[] lines)
        {
            SortedDictionary<string, TypeInfo> dictionary = new SortedDictionary<string, TypeInfo>();
            string str = "";
            foreach (string str2 in lines)
            {
                // ignore empty and comment lines
                if ((str2.Length != 0) && (str2[0] != '#'))
                {
                    // append all strings until line without ; at the end
                    str = str + str2;
                    if ((str2[str2.Length - 1] != ';') && (str2[str2.Length - 1] != '\t'))
                    {
                        string[] strArray = str.Split("\t".ToCharArray());
                        
                        // let the TypeInfo class parse this
                        TypeInfo info = new TypeInfo(strArray[0], strArray[1]);
                        dictionary.Add(strArray[0], info);
                        str = "";
                    }
                }
            }
            return dictionary;
        }

	}

    class TypeInfoComparer : Comparer<TypeInfo>
    {
        public override int Compare(TypeInfo x, TypeInfo y)
        {
            return x.name.CompareTo(y.name);
        }
    }
}
