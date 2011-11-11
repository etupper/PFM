namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal static class Log
    {
        public static void WriteLog(List<string> text)
        {
            using (Stream stream = new FileStream(@"C:\Log.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (TextWriter writer = new StreamWriter(stream))
                {
                    foreach (string str in text)
                    {
                        writer.WriteLine(str);
                    }
                }
            }
        }
    }
}

