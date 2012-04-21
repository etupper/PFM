using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using Common;
using PackFileManager.Properties;

namespace PackFileManager
{
	class DBFileTypesUpdater
	{
        static Regex FileTypeRegex = new Regex("<a href=\".*(attachmentid=[^\"]*)\"[^>]*>schema_([0-9]*).zip</a>.*</td>");
        static Regex SwVersionRegex = new Regex(@"Update.*Pack File Manager ([0-9]*\.[0-9]*(\.[0-9]*)?)");
        static Comparer<string> comparator = new BuildVersionComparator();
        static string VERSION_FILE = "xmlversion";

        public static bool checkVersion(string targetDir, ref string swVersion)
        {
            // read the delivery announcement thread page into a string
            bool result = false;
            string url = string.Format("http://www.twcenter.net/forums/showthread.php?p={0}", Settings.Default.TwcThreadId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader stream = new StreamReader(response.GetResponseStream());
            string wholePage = stream.ReadToEnd();
            stream.Close();

            // find latest software version on page
            foreach(Match m in SwVersionRegex.Matches(wholePage)) {
                string matchVersion = SwVersionRegex.Match(wholePage).Groups[1].Value;
                if (comparator.Compare(matchVersion, swVersion) > 0)
                {
                    swVersion = matchVersion;
                }
            }

            // parse page content for attachment names
            int highestVersion = -1;
            string highestUrl = String.Empty;
            foreach (Match m in FileTypeRegex.Matches(wholePage))
            {
                // find name containing highest version and the associated url
                int version;
                if (int.TryParse(m.Groups[2].Value, out version) && (version > highestVersion)) {
                    highestVersion = version;
                    highestUrl = m.Groups[1].Value;
                }
            }
            if (highestVersion == -1)
            {
                return false;
            }

            // check if we have already the same version as we found on the page
            bool needsUpdate = true;
            if (Directory.Exists(targetDir) && File.Exists(VERSION_FILE))
            {
                int currentVersion = -1;
                StreamReader currentVersionReader = new StreamReader(VERSION_FILE);
                if (int.TryParse(currentVersionReader.ReadLine(), out currentVersion))
                {
                    needsUpdate = (currentVersion < highestVersion) || !File.Exists(Path.Combine(targetDir, "schema.xml"));
                }
                currentVersionReader.Close();
            }
            else
            {
                Directory.CreateDirectory(targetDir);
            }

            if (needsUpdate)
            {
                // download most current zipfile
                string dlUrl = string.Format("http://www.twcenter.net/forums/attachment.php?{0}", highestUrl);
                string zipfile = string.Format("schema_{0}.zip", highestVersion);

                FileStream outfile = File.OpenWrite(zipfile);
                request = (HttpWebRequest)WebRequest.Create(dlUrl);
                response = (HttpWebResponse)request.GetResponse();
                response.GetResponseStream().CopyTo(outfile);
                outfile.Close();

                // unzip all entries to the dbfiletypes directory
                ZipInputStream zipStream = new ZipInputStream(File.OpenRead(zipfile));
                ZipEntry entry = zipStream.GetNextEntry();
                while (entry != null)
                {
                    FileStream outStream = File.OpenWrite(Path.Combine(targetDir, entry.Name));
                    zipStream.CopyTo(outStream);
                    entry = zipStream.GetNextEntry();
                    outStream.Close();
                }
                
                // update version file for later update queries
                StreamWriter versionWriter = new StreamWriter(VERSION_FILE);
                versionWriter.WriteLine(highestVersion);
                versionWriter.Close();
                result = true;
            }
            return result;
        }
	}

    // compare build numbers
    public class BuildVersionComparator : Comparer<string>
    {
        public override int Compare(string v1, string v2)
        {
            int result = 0;
            string[] v1Split = v1.Split('.');
            string[] v2Split = v2.Split('.');
            for (int i = 0; i < Math.Min(v1Split.Length, v2Split.Length); i++)
            {
                int v1Version = 0, v2Version = 0;
                int.TryParse(v1Split[i], out v1Version);
                int.TryParse(v2Split[i], out v2Version);
                result = v1Version - v2Version;
                if (result != 0)
                {
                    return result;
                }
            }
            if (result == 0)
            {
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
