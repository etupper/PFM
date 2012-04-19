using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EsfLibrary;

namespace EditSF {
    public partial class EditSF : Form {
        ProgressUpdater updater;
        public static string FILENAME = "testfiles.txt";

        #region Properties
        string filename = null;
        public string FileName {
            get {
                return filename;
            }
            set {
                Text = string.Format("EditSF - {0}", Path.GetFileName(value));
                statusLabel.Text = value;
                filename = value;
            }
        }
        EsfFile file;
        EsfFile EditedFile {
            get {
                return file;
            }
            set {
                file = value;
                editEsfComponent.RootNode = value.RootNode;
                editEsfComponent.RootNode.Modified = false;
                saveAsToolStripMenuItem.Enabled = file != null;
                saveToolStripMenuItem.Enabled = file != null;
                showNodeTypeToolStripMenuItem.Enabled = file != null;
            }
        }
        #endregion

        public EditSF() {
            InitializeComponent();

            updater = new ProgressUpdater(progressBar);

        }

        private void promptOpenFile() {
            OpenFileDialog dialog = new OpenFileDialog {
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                try {
                    OpenFile(dialog.FileName);
                } catch (Exception e) {
                    MessageBox.Show(string.Format("Could not open {0}: {1}", dialog.FileName, e));
                    updater.LoadingFinished();
                }
            }
        }
        private void OpenFile(string openFilename) {
            string oldStatus = statusLabel.Text;
            try {
                fileToolStripMenuItem.Enabled = false;
                optionsToolStripMenuItem.Enabled = false;
                // EsfCodec codec = EsfCodecUtil.GetCodec(stream);
                // updater.StartLoading(openFilename, codec);
                statusLabel.Text = string.Format("Loading {0}", openFilename);
                LogFileWriter logger = null;
                if (writeLogFileToolStripMenuItem.Checked) {
                    logger = new LogFileWriter(openFilename + ".xml");
                    //codec.NodeReadFinished += logger.WriteEntry;
                    //codec.Log += logger.WriteLogEntry;
                }
                EditedFile = EsfCodecUtil.LoadEsfFile(openFilename);
                //updater.LoadingFinished();
                FileName = openFilename;
                if (logger != null) {
                    logger.Close();
                    //codec.NodeReadFinished -= logger.WriteEntry;
                    //codec.Log -= logger.WriteLogEntry;
                }
            } catch (Exception exception) {
                statusLabel.Text = oldStatus;
                Console.WriteLine(exception);
            } finally {
                fileToolStripMenuItem.Enabled = true;
                optionsToolStripMenuItem.Enabled = true;
            }
        }

        private void promptSaveFile() {
            SaveFileDialog dialog = new SaveFileDialog {
                RestoreDirectory = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Save(dialog.FileName);
                FileName = dialog.FileName;
            }
        }

        #region Menu handlers
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            promptOpenFile();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            promptSaveFile();
        }
        private void saveToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (filename != null) {
                Save(filename);
            }
        }
        private void runTestsToolStripMenuItem_Click(object sender, EventArgs eventArgs) {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                dialog.Dispose();
                string logFileName = Path.Combine(dialog.SelectedPath, "EditSF_test.txt");
                FileTester tester = new FileTester();
                using (TextWriter logWriter = File.CreateText(logFileName)) {
                    foreach (string file in Directory.EnumerateFiles(dialog.SelectedPath)) {
                        if (file.EndsWith("EditSF_test.txt")) {
                            continue;
                        }
                        string testResult = tester.RunTest(file, progressBar, statusLabel);
                        logWriter.WriteLine(testResult);
                        logWriter.Flush();
                    }
                }
                MessageBox.Show(string.Format("Test successes {0}/{1}", tester.TestSuccesses, tester.TestsRun),
                                "Tests finished");
            }
        }
        private void runSingleTestToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog openDialog = new OpenFileDialog {
                RestoreDirectory = true
            };
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                string output = new FileTester().RunTest(openDialog.FileName, progressBar, statusLabel);
                MessageBox.Show(output, "Test Finished");
            }
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            MessageBox.Show(String.Format("EditSF {0}\nCreated by daniu", Application.ProductVersion), "About EditSF");
        }
        #endregion

        private void Save(string filename) {
            try {
                EsfCodecUtil.WriteEsfFile(filename, EditedFile);
                editEsfComponent.RootNode.Modified = false;
            } catch (Exception e) {
                MessageBox.Show(string.Format("Could not save {0}: {1}", filename, e));
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void showNodeTypeToolStripMenuItem_Click(object sender, EventArgs e) {
            editEsfComponent.ShowCode = true;
        }
    }

    public class LogFileWriter {
        private TextWriter writer;
        public LogFileWriter(string logFileName) {
            writer = File.CreateText(logFileName);
        }
        public void WriteEntry(EsfNode node, long position) {
            //ParentNode
            if (node is RecordNode) {
            }
            //writer.WriteLine("Entry {0} / {1:x} read at {2:x}", node, node.TypeCode, position);
        }
        public void WriteLogEntry(string entry) {
            writer.WriteLine(entry);
        }
        public void Close() {
            writer.Close();
        }
    }

    public class ProgressUpdater {
        private ToolStripProgressBar progress;
        private EsfCodec currentCodec;
        public ProgressUpdater(ToolStripProgressBar bar) {
            progress = bar;
        }
        public void StartLoading(string file, EsfCodec codec) {
            progress.Maximum = (int)new FileInfo(file).Length;
            currentCodec = codec;
            currentCodec.NodeReadFinished += Update;
        }
        public void LoadingFinished() {
            try {
                progress.Value = 0;
                currentCodec.NodeReadFinished -= Update;
            } catch { }
        }
        void Update(EsfNode ignored, long position) {
            if (ignored is ParentNode) {
                try {
                    if ((int)position <= progress.Maximum) {
                        progress.Value = (int)position;
                    }
                    Application.DoEvents();
                } catch {
                    progress.Value = 0;
                    currentCodec.NodeReadFinished -= Update;
                }
            }
        }
    }
}