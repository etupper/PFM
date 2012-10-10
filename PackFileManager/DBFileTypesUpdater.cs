using System;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using AutoUpdater;
using Common;
using PackFileManager.Properties;

namespace PackFileManager
{
    class Util {
        static readonly string sourceForgeFormat =
            "http://downloads.sourceforge.net/project/packfilemanager/{0}?r=&ts={1}&use_mirror=master";

        public static string CreateSourceforgeUrl(string file) {
            return string.Format(sourceForgeFormat, file, DateTime.Now.Ticks);
        }
    }
    
	class DBFileTypesUpdater {
        static string VERSION_FILE = "xmlversion";
        
        LatestVersionRetriever versions;
        
        public DBFileTypesUpdater() {
            versions = new LatestVersionRetriever();
        }
        
        #region Query Update Neccessary
        public bool NeedsSchemaUpdate {
            get {
                int currentVersion, latestVersion;
                int.TryParse(CurrentSchemaVersion, out currentVersion);
                int.TryParse(versions.LatestSchemaVersion, out latestVersion);
                bool result = latestVersion > currentVersion;
                if (!result) {
                    // check if we have all game schema files; if not, force update
                    string appBaseDir = Path.GetDirectoryName(Application.ExecutablePath);
                    foreach(Game g in Game.Games) {
                        result |= !File.Exists(Path.Combine(appBaseDir, g.SchemaFilename));
                    }
                }
                return result;
            }
        }
        
        public bool NeedsPfmUpdate {
            get {
                return BuildVersionComparator.Instance.Compare(versions.LatestPfmVersion, CurrentPfmVersion) > 0;
            }
        }
        #endregion

        #region Current Versions
        static string CurrentPfmVersion {
            get {
                return Application.ProductVersion;
            }
        }
        public string LatestPfmVersion {
            get {
                return versions.LatestPfmVersion;
            }
        }
        static string CurrentSchemaVersion {
            get {
                string currentSchemaVersion = File.Exists(VERSION_FILE) ? File.ReadAllText(VERSION_FILE).Trim() : "0";
                return currentSchemaVersion;
            }
        }
        #endregion

        #region Download URLs
        string SchemaUrl {
            get {
                return Util.CreateSourceforgeUrl(string.Format("Schemata/{0}", SchemaZipname));
            }
        }
        string PfmUrl {
            get {
                return Util.CreateSourceforgeUrl(string.Format("Release/Pack%20File%20Manager%20{0}.zip", LatestPfmVersion));
            }
        }
        #endregion
  
        #region Zip File Names
        string PackFileZipname {
            get {
                return string.Format("Pack File Manager {0}.zip", LatestPfmVersion);
            }
        }
        string SchemaZipname {
            get {
                return string.Format("schema_{0}.zip", versions.LatestSchemaVersion);
            }
        }
        #endregion
        
        static string PfmDirectory {
            get {
                return Path.GetDirectoryName(Application.ExecutablePath);
            }
        }
        
        public void UpdateSchema() {
            Updater.DownloadFile(SchemaUrl, PfmDirectory, SchemaZipname);
            Updater.Unzip(Path.Combine(PfmDirectory, SchemaZipname));
        }
        
        public void UpdatePfm(string openPack = null) {
            Process myProcess = Process.GetCurrentProcess();
            string currentPackPath = openPack == null ? "" : string.Format(" \"{0}\"", openPack);
            string arguments = string.Format("{0} \"{1}\" \"{2}\"{3}", myProcess.Id, PfmUrl, Application.ExecutablePath, currentPackPath);
#if DEBUG
            Console.WriteLine("Updating with AutoUpdater.exe {0}", arguments);
#endif

            if (myProcess.CloseMainWindow()) {
                // re-open file if one is open already
                Process.Start("AutoUpdater.exe", arguments);
                myProcess.Close();
            }
        }
	}

    class LatestVersionRetriever {
        public string LatestPfmVersion { get; private set; }
        public string LatestSchemaVersion { get; private set; }
        
        const string pfmTag = "packfilemanager";
        const string schemaTag = "xmlversion";
        
        static readonly char[] SEPARATOR = { ':' };
        
        public LatestVersionRetriever() {
            string url = Util.CreateSourceforgeUrl("latestVersionInfo.txt");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var stream = new StreamReader(response.GetResponseStream())) {
                string line = stream.ReadLine();
                while (line != null) {
                    string[] split = line.Split(SEPARATOR);
                    switch(split[0]) {
                    case pfmTag:
                        LatestPfmVersion = split[1];
                        break;
                    case schemaTag:
                        LatestSchemaVersion = split[1];
                        break;
                    }
                    line = stream.ReadLine();
                }
            }
            if (LatestPfmVersion == null || LatestSchemaVersion == null) {
                throw new InvalidDataException(string.Format("Could not determine latest versions: got {0}, {1}", 
                                                             LatestPfmVersion, LatestSchemaVersion));
            }
#if DEBUG
            Console.WriteLine("Current versions: pfm {0}, schema {1}", LatestPfmVersion, LatestSchemaVersion);
#endif
        }
    }

    // compare build numbers
    public class BuildVersionComparator : Comparer<string>
    {
        public static readonly Comparer<string> Instance = new BuildVersionComparator();
        
        public override int Compare(string v1, string v2) {
            int result = 0;
            string[] v1Split = v1.Split('.');
            string[] v2Split = v2.Split('.');
            for (int i = 0; i < Math.Min(v1Split.Length, v2Split.Length); i++) {
                int v1Version = 0, v2Version = 0;
                int.TryParse(v1Split[i], out v1Version);
                int.TryParse(v2Split[i], out v2Version);
                result = v1Version - v2Version;
                if (result != 0) {
                    return result;
                }
            }
            if (result == 0) {
                // different version lengths (eg 1.7.2 and 2.0)
                result = v1Split.Length != v2Split.Length ? 1 : 0;
                // longer one is larger (2.0.1 > 2.0)
                result *= v1Split.Length > v2Split.Length ? 1 : -1;
            }

            // result > 0: v1 is larger
            return result;
        }
    }
}
