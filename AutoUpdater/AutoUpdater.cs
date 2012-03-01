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
    class Updater {
        static readonly string sourceForgeFormat =
            "http://downloads.sourceforge.net/project/packfilemanager/Release/" +
            "Pack%20File%20Manager%20{0}.zip?r=&ts={1}&use_mirror=master";

        static readonly string targetNameFormat = "PackFileManager{0}.zip";

        public static void Main(string[] args) {
            // the id of the process to wait for termination of
            int processId;

            // the target version identifier
            // determines source URL and target zip filename
            string version;
            
            // name of the executable to run after the other process has finished
            string startFileName;
            
            // collect eventual arguments
            List<string> arguments = new List<string> ();
   
            try {
                processId = int.Parse (args [0]);
                version = args [1];
                startFileName = args [2];
                for (int i = 3; i < args.Length; i++) {
                    arguments.Add (args [i]);
                }
            } catch {
                Console.WriteLine ("usage: <pid> <version> <executable>");
                return;
            }
   
            // the url to download the update from
            string dlUrl = string.Format (sourceForgeFormat, version, DateTime.Now.Ticks);

            // the name of the zip file to store in
            string zipfileName = string.Format (targetNameFormat, version);

            Process proc = null;
            try {
                proc = Process.GetProcessById (processId);
                proc.EnableRaisingEvents = true;
            } catch (Exception x) {
                Console.WriteLine ("Failed to wait for process '{0}': {1}", processId, x.Message);
            }

            // download file from URL; this also gives the other process some time to shutdown
            // to avoid superfluous waiting
            Console.WriteLine ("Downloading from {0}", dlUrl);
            using (FileStream outfile = File.Open (zipfileName, FileMode.Create)) {
                var request = (HttpWebRequest)WebRequest.Create (dlUrl);
                var response = (HttpWebResponse)request.GetResponse ();
                response.GetResponseStream ().CopyTo (outfile);
            }
            
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
            ZipInputStream zipStream = new ZipInputStream (File.OpenRead (zipfileName));
            ZipEntry entry = zipStream.GetNextEntry ();
            while (entry != null) {
                FileStream outStream = File.OpenWrite (entry.Name);
                zipStream.CopyTo (outStream);
                entry = zipStream.GetNextEntry ();
                outStream.Close ();
            }
            string asParameters = "";
            arguments.ForEach (a => asParameters += " " + a);
            Console.WriteLine ("starting {0} {1}", startFileName, asParameters.Trim ());
            
            Console.WriteLine ("Okay, finished. Restarting.");
            Process.Start (startFileName, asParameters.Trim ());
        }
    }
}
