using System;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using AutoUpdater;
using Common;
using PackFileManager.Properties;

namespace PackFileManager
{
    class Util {
        static readonly string sourceForgeFormat =
            "http://sourceforge.net/projects/packfilemanager/files/{0}/download";
            // "http://downloads.sourceforge.net/project/packfilemanager/{0}?r=&ts={1}&use_mirror=master";

        public static string CreateSourceforgeUrl(string file) {
            return string.Format(sourceForgeFormat, file, DateTime.Now.Ticks);
        }
        public static string PfmDirectory {
            get {
                return Path.GetDirectoryName(Application.ExecutablePath);
            }
        }
    }
    
	class DBFileTypesUpdater {
        static string VERSION_FILE = "xmlversion";
        
        ILatestVersionRetriever versions;
        
        public DBFileTypesUpdater() {
            // versions = new TwcVersionRetriever();
            versions = new SourceforgeVersionRetriever();
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
        
        public void UpdateSchema() {
            Updater.DownloadFile(versions.SchemaUrl, Util.PfmDirectory, SchemaZipname);
            Updater.Unzip(Path.Combine(Util.PfmDirectory, SchemaZipname));
        }
        
        public void UpdatePfm(string openPack = null) {
            Process myProcess = Process.GetCurrentProcess();
            string currentPackPath = openPack == null ? "" : string.Format(" \"{0}\"", openPack);
            string arguments = string.Format("{0} \"{1}\" \"{2}\"{3}", myProcess.Id, versions.PfmUrl, Application.ExecutablePath, currentPackPath);
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
    
    /*
     * Retrieve latest versions from any source.
     */
    interface ILatestVersionRetriever {
        string LatestPfmVersion { get; }
        string LatestSchemaVersion { get; }
        
        string SchemaUrl { get; }
        string PfmUrl { get; }
    }
 
    /*
     * Retrieves versions from single text file on sourceforge and creates links
     * from known base path + version information.
     */
    class SourceforgeVersionRetriever : ILatestVersionRetriever {
        const string pfmTag = "packfilemanager";
        const string schemaTag = "xmlversion";
        
        public string LatestPfmVersion { get; private set; }
        public string LatestSchemaVersion { get; private set; }

        #region Download URLs
        public string SchemaUrl {
            get {
                return Util.CreateSourceforgeUrl(string.Format("Schemata/schema_{0}.zip", LatestSchemaVersion));
            }
        }
        public string PfmUrl {
            get {
                return Util.CreateSourceforgeUrl(string.Format("Release/Pack%20File%20Manager%20{0}.zip", LatestPfmVersion));
            }
        }
        #endregion
        
        static readonly char[] SEPARATOR = { ':' };

        static Regex schema_file_re = new Regex("schema_([0-9]*).zip");
        static Regex pfm_file_re = new Regex("Pack File Manager (.*).zip");
        public SourceforgeVersionRetriever() {
            Console.WriteLine("looking up sf");
            FindOnPage findSchema = new FindOnPage {
                Url = "https://sourceforge.net/projects/packfilemanager/files/Schemata/",
                ToFind = schema_file_re
            };
            FindOnPage findPfmVersion = new FindOnPage {
                Url = "https://sourceforge.net/projects/packfilemanager/files/Release/",
                ToFind = pfm_file_re
            };
            Thread[] findThreads = new Thread[] {
                new Thread(findSchema.Search),
                new Thread(findPfmVersion.Search)
            };
            foreach (Thread t in findThreads) {
                t.Start();
            }
            foreach (Thread t in findThreads) {
                t.Join();
            }
            LatestSchemaVersion = findSchema.Result;
            LatestPfmVersion = findPfmVersion.Result;
            if (LatestPfmVersion == null || LatestSchemaVersion == null) {
                throw new InvalidDataException(string.Format("Could not determine latest versions: got {0}, {1}", 
                                                             LatestPfmVersion, LatestSchemaVersion));
            }
#if DEBUG
            Console.WriteLine("Current versions: pfm {0}, schema {1}", LatestPfmVersion, LatestSchemaVersion);
#endif
        }
    }
  
    /*
     * Retrieves versions from TWC PFM thread; PFM dl link is created from
     * known SF base path + version information, schema link retrieved from
     * forum thread href.
     */
    class TwcVersionRetriever : ILatestVersionRetriever {
        public string LatestPfmVersion { get; private set; }
        public string LatestSchemaVersion { get; private set; }
  
        #region Download URLs
        // retrieved from twc thread link while parsing
        string schemaUrl;
        public string SchemaUrl {
            get {
                return schemaUrl;
            }
            private set {
                // expects the "attachmentid=..." string for the query parameters, will create URL itself
                schemaUrl = string.Format("http://www.twcenter.net/forums/attachment.php?{0}", value);
            }
        }
        public string PfmUrl {
            get {
                return Util.CreateSourceforgeUrl(string.Format("Release/Pack%20File%20Manager%20{0}.zip", LatestPfmVersion));
            }
        }
        #endregion
  
        static Regex FileTypeRegex = new Regex("<a href=\".*(attachmentid=[^\"]*)\"[^>]*>schema_([0-9]*).zip</a>.*</td>");
        static Regex SwVersionRegex = new Regex(@"Update.*Pack File Manager ([0-9]*\.[0-9]*(\.[0-9]*)?)");
        static Regex StopReadRegex = new Regex("/ attachments");

        public TwcVersionRetriever() {
            LatestPfmVersion = LatestSchemaVersion = "0";
            
            string url = string.Format("http://www.twcenter.net/forums/showthread.php?t=494248");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var stream = new StreamReader(response.GetResponseStream())) {
                string line = stream.ReadLine();
                while (line != null) {
                    if (FileTypeRegex.IsMatch(line)) {
                        Match match = FileTypeRegex.Match(line);
                        string schemaVersion = match.Groups[2].Value;
                        if (BuildVersionComparator.Instance.Compare(schemaVersion, LatestSchemaVersion) > 0) {
                            LatestSchemaVersion = schemaVersion;
                            schemaUrl = match.Groups[1].Value;
                        }
                    } else if (SwVersionRegex.IsMatch(line)) {
                        string swVersion = SwVersionRegex.Match(line).Groups[1].Value;
                        if (BuildVersionComparator.Instance.Compare(swVersion, LatestPfmVersion) > 0) {
                            LatestPfmVersion = swVersion;
                        }
                    } else if (StopReadRegex.IsMatch(line)) {
                        // PFM info is in the post itself, schema info in the attachment links...
                        // no need to go on
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
            Console.WriteLine("Latest PFM: {0}, Latest Schema: {1}", LatestPfmVersion, LatestSchemaVersion);
#endif
        }
    }

    class FindOnPage {
        public Regex ToFind { get; set; }
        public string Url { get; set; }
        public string Result { get; private set; }

        public void Search() {
            if (Url == null || ToFind == null) {
                return;
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var stream = new StreamReader(response.GetResponseStream())) {
                string line = stream.ReadLine();
                while (line != null) {
                    if (ToFind.IsMatch(line)) {
                        Match match = ToFind.Match(line);
                        Result = match.Groups[1].Value;
                        break;
                    }
                    line = stream.ReadLine();
                }
            }
        }
    }
}
