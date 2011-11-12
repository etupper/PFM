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
        static Regex FileTypeRegex = new Regex("<a href=\".*(attachmentid=[^\"]*)\"[^>]*>DBFileTypes_([0-9]*).zip</a>.*</td>");
        static Regex SwVersionRegex = new Regex(@"Pack File Manager ([0-9]*)\.([0-9]*)\.([0-9]*)");
        static string VERSION_FILE = "version";

        public static bool checkVersion(string basePath)
        {
            // read the delivery announcement thread page into a string
            bool result = false;
            string url = string.Format("http://www.twcenter.net/forums/showthread.php?p={0}", Settings.Default.TwcThreadId);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader stream = new StreamReader(response.GetResponseStream());
            string wholePage = stream.ReadToEnd();
            stream.Close();

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
            string targetDir = Path.Combine(basePath, DBTypeMap.DB_FILE_TYPE_DIR_NAME);
            bool needsUpdate = true;
            if (Directory.Exists(targetDir) && File.Exists(VERSION_FILE))
            {
                int currentVersion = -1;
                StreamReader currentVersionReader = new StreamReader(VERSION_FILE);
                if (int.TryParse(currentVersionReader.ReadLine(), out currentVersion))
                {
                    needsUpdate = (currentVersion < highestVersion);
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
                string zipfile = string.Format("DBFileTypes_{0}.zip", highestVersion);

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
}
