using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace Common
{
	public class DBTypeMap
	{
        static Regex DB_FILE_TYPE_PATTERN = new Regex("DBFileTypes_([0-9]*)");
        public static readonly string DB_FILE_TYPE_DIR_NAME = "DBFileTypes";

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

        private static SortedDictionary<string, List<TypeInfo>> loadDbFileTypes(string basePath)
        {
            SortedDictionary<string, List<TypeInfo>> result = new SortedDictionary<string, List<TypeInfo>>();
            int headerVersion = 0;

            // find the highest header version
            string[] matchingFiles = Directory.GetFiles(Path.Combine(basePath, DB_FILE_TYPE_DIR_NAME), "DBFileTypes_*");
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
                            List<TypeInfo> addTo;
                            if (!result.TryGetValue(key, out addTo)) {
                                addTo = new List<TypeInfo>();
                                result.Add(key, addTo);
                            }
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
    public class XsdParser
    {
        public static Regex TABLES_SUFFIX = new Regex("_tables");

        int lastVersion = 0;
        SortedDictionary<string, List<TypeInfo>> allInfos = new SortedDictionary<string, List<TypeInfo>>();
        XmlSchema schema;

        // temporaries during parsing
        string currentDbFileName;
        TypeInfo currentInfo;
        List<TypeInfo> infos;

        public XsdParser(string file)
        {
            FileStream fs;
            XmlSchemaSet set;
            try
            {
                fs = new FileStream(file, FileMode.Open);
                schema = XmlSchema.Read(fs, new ValidationEventHandler(ShowCompileError));
                set = new XmlSchemaSet();
                set.Add(schema);

                set.Compile();

                //loadXsd();
            }
            catch (XmlSchemaException e)
            {
                Console.WriteLine("LineNumber = {0}", e.LineNumber);
                Console.WriteLine("LinePosition = {0}", e.LinePosition);
                Console.WriteLine("Message = {0}", e.Message);
                Console.WriteLine("Source = {0}", e.Source);
            }
        }
        public SortedDictionary<string, List<TypeInfo>> loadXsd()
        {
            DisplayObjects(schema, "");
            return allInfos;
        }
        private void startNewDbFile(XmlSchemaComplexType type)
        {
            // add previously read db file
            if (currentDbFileName != null)
            {
                addCurrentInfo();
                allInfos.Add(currentDbFileName.Replace("_tables", ""), infos);
            }
            lastVersion = 0;
            currentDbFileName = type.Name;
            currentInfo = new TypeInfo(currentDbFileName);
        }

        private void addDbAttribute(XmlSchemaAttribute attribute)
        {
            bool optional = false;
            if (attribute.UnhandledAttributes != null)
            {
                foreach (XmlAttribute unhandled in attribute.UnhandledAttributes)
                {
                    if (unhandled.Name == "msprop:Optional" && unhandled.Value == "true")
                    {
                        optional = true;
                    }
                    if (unhandled.Name == "msProp:VersionStart")
                    {
                        int nextVersion = int.Parse(unhandled.Value);
                        addCurrentInfo();
                        lastVersion = nextVersion;
                    }
                }
            }
            if (optional)
            {
                FieldInfo field = new FieldInfo("->", "Boolean", "1");
                currentInfo.fields.Add(field);
            }
            FieldInfo fieldType = new FieldInfo(attribute.Name, attribute.AttributeSchemaType.TypeCode.ToString());
            currentInfo.fields.Add(fieldType);
        }

        private void DisplayObjects(XmlSchemaObject o, string indent)
        {
            string str = "unknown";
            XmlSchemaObjectCollection children = new XmlSchemaObjectCollection();
            if (o is XmlSchema)
            {
                str = "root";
                children = ((XmlSchema)o).Items;
            }
            else if (o is XmlSchemaComplexType)
            {
                XmlSchemaComplexType type = (XmlSchemaComplexType)o;
                startNewDbFile(type);
                str = type.Name;
                children = type.Attributes;
                infos = new List<TypeInfo>();
            }
            else if (o is XmlSchemaAttribute)
            {
                XmlSchemaAttribute attribute = (XmlSchemaAttribute)o;
                addDbAttribute(attribute);
                str = string.Format("{0} ({1})", attribute.Name, attribute.AttributeSchemaType.TypeCode);
            }
            if (o is XmlSchemaAnnotated && ((XmlSchemaAnnotated)o).UnhandledAttributes != null)
            {
                string attlist = "";
                new List<XmlAttribute>(((XmlSchemaAnnotated)o).UnhandledAttributes).ForEach(uh => attlist += " " + uh);
                str = string.Format("{0} (unhandled: {1})", str, attlist);
            }
            // Console.WriteLine("{0}{1}", indent, str);

            foreach (XmlSchemaObject child in children)
            {
                DisplayObjects(child, indent + "\t");
            }
        }
        private void addCurrentInfo()
        {
            for (int i = infos.Count; i < lastVersion + 1; i++)
            {
                infos.Add(null);
            }
            infos[lastVersion] = currentInfo;
        }
        private static void ShowCompileError(object sender, ValidationEventArgs e)
        {
            Console.WriteLine("Validation Error: {0}", e.Message);
        }
    }
}
