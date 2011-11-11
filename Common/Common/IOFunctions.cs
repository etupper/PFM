namespace Common
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    public class IOFunctions
    {
        public static string GetShogunTotalWarDirectory()
        {
            string str = null;
            if (string.IsNullOrEmpty(str))
            {
                str = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 34330", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 34330", "InstallLocation", "");
            }
            return str;
        }

        public static string GetNapoleonTotalWarDirectory()
        {
            string str = null;
            if (string.IsNullOrEmpty(str))
            {
                str = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 34030", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 34050", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 901162", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 34030", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 34050", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = (string) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 901162", "InstallLocation", "");
            }
            if (string.IsNullOrEmpty(str))
            {
                str = string.Empty;
            }
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
                Filter = "Text TSV|*.tsv|Any File|*.*"
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

