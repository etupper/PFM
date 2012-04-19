using Common;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PackFileManager.Properties;

namespace PackFileManager {
    public class FileExtractor {
        private ToolStripStatusLabel packStatusLabel;
        private ToolStripProgressBar packActionProgressBar;

        public IExtractionPreprocessor Preprocessor {
            get;
            set;
        }

        public FileExtractor(ToolStripStatusLabel l, ToolStripProgressBar b) {
            packStatusLabel = l;
            packActionProgressBar = b;
            Preprocessor = new IdentityPreprocessor();
        }

        public void extractFiles(List<PackedFile> packedFiles) {
            string exportDirectory = null;
            if (Settings.Default.CurrentMod == "") {
                FolderBrowserDialog extractFolderBrowserDialog = new FolderBrowserDialog {
                    Description = "Extract to what folder?",
                    SelectedPath = Settings.Default.LastPackDirectory
                };
                exportDirectory =
                    extractFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                    ? extractFolderBrowserDialog.SelectedPath : "";

            } else {
                exportDirectory = ModManager.Instance.CurrentModDirectory;
            }
            if (!string.IsNullOrEmpty(exportDirectory)) {
                FileAlreadyExistsDialog.Action action = FileAlreadyExistsDialog.Action.Ask;
                FileAlreadyExistsDialog.Action defaultAction = FileAlreadyExistsDialog.Action.Ask;
                packStatusLabel.Text = string.Format("Extracting file (0 of {0} files extracted, 0 skipped)", packedFiles.Count);
                packActionProgressBar.Visible = true;
                packActionProgressBar.Minimum = 0;
                packActionProgressBar.Maximum = packedFiles.Count;
                packActionProgressBar.Step = 1;
                packActionProgressBar.Value = 0;
                int num = 0;
                int num2 = 0;
                foreach (PackedFile file in packedFiles) {
                    string path = Path.Combine(exportDirectory, Preprocessor.GetFileName(file.FullPath));
                    if (File.Exists(path)) {
                        string str3;
                        if (defaultAction == FileAlreadyExistsDialog.Action.Ask) {
                            FileAlreadyExistsDialog dialog = new FileAlreadyExistsDialog(path);
                            dialog.ShowDialog(null);
                            action = dialog.ChosenAction;
                            defaultAction = dialog.NextAction;
                        } else {
                            action = defaultAction;
                        }
                        switch (action) {
                            case FileAlreadyExistsDialog.Action.Skip: {
                                    num2++;
                                    packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                                    packActionProgressBar.PerformStep();
                                    Application.DoEvents();
                                    continue;
                                }
                            case FileAlreadyExistsDialog.Action.RenameExisting:
                                str3 = path + ".bak";
                                while (File.Exists(str3)) {
                                    str3 = str3 + ".bak";
                                }
                                File.Move(path, str3);
                                break;

                            case FileAlreadyExistsDialog.Action.RenameNew:
                                do {
                                    path = path + ".new";
                                }
                                while (File.Exists(path));
                                break;

                            case FileAlreadyExistsDialog.Action.Cancel:
                                packStatusLabel.Text = "Extraction cancelled.";
                                packActionProgressBar.Visible = false;
                                return;
                        }
                    } else {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                    Application.DoEvents();
                    File.WriteAllBytes(path, Preprocessor.Process(file));
                    num++;
                    packStatusLabel.Text = string.Format("({1} of {2} files extracted, {3} skipped): extracting {0}", new object[] { file.FullPath, num, packedFiles.Count, num2 });
                    packActionProgressBar.PerformStep();
                    Application.DoEvents();
                }
            }
        }
    }
    public interface IExtractionPreprocessor {
        string GetFileName(string path);
        byte[] Process(PackedFile file);
    }
    public class IdentityPreprocessor : IExtractionPreprocessor {
        public string GetFileName(string path) { return path; }
        public byte[] Process(PackedFile file) { return file.Data; }
    }

    public class TsvConversionPreprocessor : IExtractionPreprocessor {
        public string GetFileName(string path) {
            return string.Format("{0}.csv", path);
        }
        public byte[] Process(PackedFile file) {
            byte[] result = file.Data;
            using (MemoryStream stream = new MemoryStream()) {
                try {
                    DBFile dbFile = PackedFileDbCodec.Decode(file);
                    TextDbCodec.Instance.Encode(stream, dbFile);
                    result = stream.ToArray();
                } catch (DBFileNotSupportedException) {
                    MessageBox.Show(string.Format("Could not export to tsv: {0}\nTSV File will contain raw db data.", file.FullPath));
                }
            }
            return result;
        }
    }
}
