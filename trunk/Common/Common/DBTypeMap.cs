using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

namespace Common
{
	public class DBTypeMap
	{
        static Regex DB_FILE_TYPE_PATTERN = new Regex("DBFileTypes_([0-9]*)");
        public static readonly string DB_FILE_TYPE_DIR_NAME = "DBFileTypes";

        private SortedDictionary<string, TypeInfo>[] typeMap;
        public List<string> dbFileTypes = new List<string>();
        int headerVersion = 12;

        public static readonly DBTypeMap Instance = new DBTypeMap();

        private DBTypeMap()
        {
            // prevent instantiation
        }

        public void initializeTypeMap(string basePath)
        {
            typeMap = new SortedDictionary<string, TypeInfo>[0];
            dbFileTypes = new List<string>();
            headerVersion = 12;

            string filepath = "";

            // find the highest header version
            string[] matchingFiles = Directory.GetFiles(Path.Combine(basePath, DB_FILE_TYPE_DIR_NAME), "DBFileTypes_*");
            foreach (string file in matchingFiles)
            {
                if (DB_FILE_TYPE_PATTERN.IsMatch(file))
                {
                    int version = -1;
                    int.TryParse(DB_FILE_TYPE_PATTERN.Match(file).Groups[1].Value, out version);
                    headerVersion = Math.Max(headerVersion, version);
                }
            }
            this.typeMap = new SortedDictionary<string, TypeInfo>[headerVersion];
            for (int i = 0; i < headerVersion; i++)
            {
                filepath = Path.Combine(basePath, DB_FILE_TYPE_DIR_NAME, string.Format("DBFileTypes_{0}.txt", i));
                if (File.Exists(filepath))
                {
                    this.typeMap[i] = this.getTypeMapFromFile(filepath);
                }
            }
            this.dbFileTypes.Sort();
            for (int i = this.dbFileTypes.Count - 1; i > 0; i--)
            {
                if (this.dbFileTypes[i] == this.dbFileTypes[i - 1])
                    this.dbFileTypes.RemoveAt(i);
            }
        }

        public int HighestHeaderVersion
        {
            get
            {
                return headerVersion;
            }
        }

        public List<string> DBFileTypes
        {
            get
            {
                return dbFileTypes;
            }
        }

        public SortedDictionary<string, TypeInfo> this[int index]
        {
            get
            {
                return typeMap[index];
            }
        }

        private SortedDictionary<string, TypeInfo> getTypeMapFromFile(string filepath)
        {
            string[] lines = File.ReadAllLines(filepath);
            return this.parseTypeMap(lines);
        }

        private SortedDictionary<string, TypeInfo> parseTypeMap(string[] lines)
        {
            SortedDictionary<string, TypeInfo> dictionary = new SortedDictionary<string, TypeInfo>();
            string str = "";
            foreach (string str2 in lines)
            {
                if ((str2.Length != 0) && (str2[0] != '#'))
                {
                    str = str + str2;
                    if ((str2[str2.Length - 1] != ';') && (str2[str2.Length - 1] != '\t'))
                    {
                        string[] strArray = str.Split("\t".ToCharArray());
                        TypeInfo info = new TypeInfo(strArray[0], strArray[1]);
                        dictionary.Add(strArray[0], info);
                        this.dbFileTypes.Add(strArray[0]);
                        str = "";
                    }
                }
            }
            return dictionary;
        }


	}
}
