using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;

namespace AutoUpdater {
    /*
     * Provides small executable that will download a zip file, wait for another process to finish, 
     * then extract the zip file and start a given executable.
     * 
     * It is meant to provide an automatic update of a program which can't do it for itself
     * because it can't write files it still uses while running.
     */
    public class Updater {
        public static void Main(string[] args) {
            // the id of the process to wait for termination of
            int processId;

            // the target version identifier
            // determines source URL and target zip filename
            string downloadUrl;
            
            // name of the executable to run after the other process has finished (full path)
            string startFileName;
            
            // collect eventual arguments
            List<string> arguments = new List<string> ();
            
            Console.WriteLine("Autoupdate started...");
   
            try {
                processId = int.Parse (args [0]);
                downloadUrl = args [1];
                startFileName = args [2];
                for (int i = 3; i < args.Length; i++) {
                    arguments.Add (args [i]);
                }
            } catch {
                Console.WriteLine ("usage: <pid> <downloadUrl> <executable> [<exec_parameters>]");
                return;
            }
#if DEBUG
            Console.WriteLine("startFileName: {0}", startFileName);
#endif
   
            Process proc = null;
            try {
                proc = Process.GetProcessById (processId);
                proc.EnableRaisingEvents = true;
            } catch (Exception x) {
                Console.WriteLine ("Failed to wait for process '{0}': {1}", processId, x.Message);
            }

            // download file from URL; this also gives the other process some time to shutdown
            // to avoid superfluous waiting
            string targetDir = Path.GetDirectoryName(startFileName);
            string filename = Path.GetFileName(downloadUrl).Replace("%20", " ");
            if (filename.Contains("?")) {
                filename = filename.Remove(filename.IndexOf('?'));
            }
            DownloadFile(downloadUrl, targetDir, filename);
            
            try {
                if (proc != null && !proc.HasExited) {
                    Console.WriteLine ("waiting for process to exit...");
                    proc.WaitForExit ();
                }
            } catch (Exception x) {
                Console.WriteLine ("Failed to wait for process {0}: {1}", processId, x.Message);
            }
            
            Console.WriteLine ("Installing...");

            // unzip all entries
            Unzip(filename);

            string asParameters = string.Join(" ", arguments);
            Console.WriteLine ("starting {0} {1}", startFileName, asParameters.Trim ());
            
            Console.WriteLine ("Okay, finished. Restarting.");
            Process.Start (startFileName, asParameters.Trim ());
        }
  
        // downloads the given url to the given target directory, with the optional target filename.
        // if target filename is null, the url's filename is used 
        // (which might include encoded characters and http-query information).
        public static void DownloadFile(string url, string targetDir, string targetFile = null) {
            if (targetFile == null) {
                targetFile = Path.GetFileName(url);
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var dlStream = response.GetResponseStream()) {
                string targetPath = Path.Combine(targetDir, targetFile);
                using (var targetFileStream = File.Create(targetPath)) {
                    Console.WriteLine("Downloading from {0} to {1}", url, targetPath);
                    dlStream.CopyTo(targetFileStream);
                }
            }
        }
        // unzip all entries of the given file to the directory it resides in.
        public static void Unzip(string zipFile) {
            string targetDir = Path.GetDirectoryName(zipFile);
            using (var zipStream = new ZipInputStream(File.OpenRead(zipFile))) {
            ZipEntry entry = zipStream.GetNextEntry();
                while (entry != null) {
                    using (FileStream outStream = File.OpenWrite(Path.Combine(targetDir, entry.Name))) {
                        zipStream.CopyTo(outStream);
                    }
                    entry = zipStream.GetNextEntry();
                }
            }
        }
    }
}
