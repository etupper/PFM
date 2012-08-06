﻿namespace Common
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    public class IOFunctions
    {
        public static string TSV_FILTER = "TSV Files (*.csv,*.tsv)|*.csv;*.tsv|Text Files (*.txt)|*.txt|All Files|*.*";
        public static string PACKAGE_FILTER = "Package File (*.pack)|*.pack|Any File|*.*";

        public static string GetShogunTotalWarDirectory()
        {
            return GetDirectory(STW_IDS);
        }
        
        public static string GetNapoleonTotalWarDirectory()
        {
            return GetDirectory(NTW_IDS);
        }
        
        private static string WOW_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        private static string WIN_NODE = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
        
        private static string[] STW_IDS = { "34330" };
        private static string[] NTW_IDS = { "34030", "34050", "901162" };
        private static string[] ETW_IDS = { "10500" };

        private static string GetDirectory(string[] candidates) {
            string str = null;
            for (int i = 0; i < candidates.Length && str == null; i++) {
                try {
                    str = GetInstallLocation(WOW_NODE, candidates[i]);
                    if (str == null) {
                        str = GetInstallLocation(WIN_NODE, candidates[i]);
                    }
                } catch {}
            }
            return str;
        }
        
        private static string GetInstallLocation(string node, string id) {
            string str = null;
            try {
                string regKey = string.Format(WOW_NODE, id);
                str = (string) Registry.GetValue(regKey, "InstallLocation", "");
                // check if directory actually exists
                if (!string.IsNullOrEmpty(str) && !Directory.Exists(str)) {
                    str = null;
                }
            } catch {}
            return str;
        }

        public static string readCAString(BinaryReader reader)
        {
            int num = reader.ReadInt16();
            return new string(Encoding.Unicode.GetChars(reader.ReadBytes(num * 2)));
        }

        public static string readStringContainer(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(0x200);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; bytes[i] != 0; i += 2)
            {
                builder.Append(Encoding.Unicode.GetChars(bytes, i, 2));
            }
            return builder.ToString();
        }

        public static string readZeroTerminatedAscii(BinaryReader reader) {
            StringBuilder builder2 = new StringBuilder();
            char ch2 = reader.ReadChar();
            while (ch2 != '\0') {
                builder2.Append(ch2);
                ch2 = reader.ReadChar();
            }
            return builder2.ToString();
        }

        public static void writeCAString(BinaryWriter writer, string value)
        {
            writer.Write((ushort) value.Length);
            writer.Write(Encoding.Unicode.GetBytes(value));
        }

        public static void writeStringContainer(BinaryWriter writer, string value)
        {
            byte[] array = new byte[0x200];
            Encoding.Unicode.GetBytes(value).CopyTo(array, 0);
            writer.Write(array);
        }

        public static void writeToTSVFile(List<string> strings)
        {
            SaveFileDialog dialog = new SaveFileDialog {
                Filter = TSV_FILTER
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(dialog.FileName))
                {
                    foreach (string str in strings)
                    {
                        writer.WriteLine(str);
                    }
                }
            }
        }
    }
}

