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
        TreeEventHandler treeEventHandler;
        public static string FILENAME = "testfiles.txt";

        #region Properties
        public EsfNode RootNode {
            get {
                if (esfNodeTree.Nodes.Count == 0) {
                    return null;
                }
                return (esfNodeTree.Nodes[0].FirstNode as EsfTreeNode).Tag as EsfNode;
            }
            set {
                esfNodeTree.Nodes.Clear();
                EsfTreeNode rootNode = new EsfTreeNode(value as NamedNode);
                rootNode.ShowCode = showNodeTypeToolStripMenuItem.Checked;
                esfNodeTree.Nodes.Add(rootNode);
                rootNode.Fill();
            }
        }

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
                RootNode = value.RootNode;
                saveAsToolStripMenuItem.Enabled = file != null;
                saveToolStripMenuItem.Enabled = file != null;
                showNodeTypeToolStripMenuItem.Enabled = file != null;
            }
        }
        #endregion

        public EditSF() {
            InitializeComponent();

            updater = new ProgressUpdater(progressBar);

            nodeValueGridView.Rows.Clear();

            treeEventHandler = new TreeEventHandler(nodeValueGridView);
            esfNodeTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(treeEventHandler.FillNode);
            esfNodeTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(treeEventHandler.NodeSelected);

            nodeValueGridView.CellValidating += new DataGridViewCellValidatingEventHandler(validateCell);
            nodeValueGridView.CellEndEdit += new DataGridViewCellEventHandler(cellEdited);
        }

        private void validateCell(object sender, DataGridViewCellValidatingEventArgs args) {
            EsfNode valueNode = nodeValueGridView.Rows[args.RowIndex].Tag as EsfNode;
            if (valueNode != null) {
                string newValue = args.FormattedValue.ToString();
                try {
                    if (args.ColumnIndex == 0 && newValue != valueNode.ToString()) {
                        valueNode.FromString(newValue);
                    }
                } catch {
                    Debug.WriteLine("Invalid value {0}", newValue);
                    args.Cancel = true;
                }
            } else {
                nodeValueGridView.Rows[args.RowIndex].ErrorText = "Cannot edit this value";
                // args.Cancel = true;
            }
        }
        private void cellEdited(object sender, DataGridViewCellEventArgs args) {
            nodeValueGridView.Rows[args.RowIndex].ErrorText = String.Empty;
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
                using (Stream stream = File.OpenRead(openFilename)) {
                    fileToolStripMenuItem.Enabled = false;
                    optionsToolStripMenuItem.Enabled = false;
                    EsfCodec codec = EsfCodecUtil.GetCodec(stream);
                    updater.StartLoading(openFilename, codec);
                    statusLabel.Text = string.Format("Loading {0}", openFilename);
                    LogFileWriter logger = null;
                    if (writeLogFileToolStripMenuItem.Checked) {
                        logger = new LogFileWriter(openFilename + ".xml");
                        //codec.NodeReadFinished += logger.WriteEntry;
                        codec.Log += logger.WriteLogEntry;
                    }
                    EditedFile = new EsfFile(stream, codec);
                    updater.LoadingFinished();
                    FileName = openFilename;
                    if (logger != null) {
                        logger.Close();
                        codec.NodeReadFinished -= logger.WriteEntry;
                    }
                }
            } catch {
                statusLabel.Text = oldStatus;
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
            } catch (Exception e) {
                MessageBox.Show(string.Format("Could not save {0}: {1}", filename, e));
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void showNodeTypeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (EditedFile != null) {
                (esfNodeTree.Nodes[0] as EsfTreeNode).ShowCode = showNodeTypeToolStripMenuItem.Checked;
                nodeValueGridView.Columns["Code"].Visible = showNodeTypeToolStripMenuItem.Checked;
            }
        }
    }

    public class LogFileWriter {
        private TextWriter writer;
        public LogFileWriter(string logFileName) {
            writer = File.CreateText(logFileName);
        }
        public void WriteEntry(EsfNode node, long position) {
            writer.WriteLine("Entry {0} / {1:x} read at {2:x}", node, node.TypeCode, position);
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
            if (ignored is NamedNode) {
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