using Common;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
namespace PackFileManager {
    /*
     * Base class for any kind of entry (directory of file).
     */
    abstract class PackEntryNode : TreeNode {
        protected bool renamed;
        private bool added;
        public bool Added {
            get { return added; }
            set {
                added = value;
                changeColor(Tag as PackEntry);
            }
        }
        public PackEntryNode(PackEntry entry)
            : base(entry.Name) {
            Tag = entry;
            entry.ModifiedEvent += changeColor;
            entry.RenameEvent += (e, name) => { renamed = true; changeColor(e); };
            changeColor(entry);
        }
        public virtual void changeColor(PackEntry e) {
            ForeColor = Color.Black;
            if (added) {
                ForeColor = Color.Green;
            } else if (renamed) {
                ForeColor = Color.LimeGreen;
            } else if (e.Deleted) {
                ForeColor = Color.LightGray;
            } else if (e.Modified) {
                ForeColor = Color.Red;
            }
        }
        public void reset() {
            renamed = added = false;
            foreach (TreeNode node in Nodes) {
                PackEntryNode packNode = node as PackEntryNode;
                packNode.reset();
            }
        }
    }

    /*
     * A tree node representing an actual data file.
     */
    class PackedFileNode : PackEntryNode {
        public PackedFileNode(PackedFile file)
            : base(file) {
        }
        /*
         * Overridden to adjust to color depending on we have DB type information.
         */
        public override void changeColor(PackEntry file) {
            base.changeColor(file);

            string mouseover;
            PackedFile file2 = file as PackedFile;
            if (!file2.Deleted && file2.FullPath.StartsWith("db")) {
                if (!PackedFileDbCodec.CanDecode(file2, out mouseover)) {
                    if (Parent != null) {
                        Parent.ToolTipText = mouseover;
                        Parent.ForeColor = Color.Red;
                    }
                    ForeColor = Color.Red;
                } else {
                    if (PackedFileDbCodec.readHeader(file2).EntryCount == 0) {
                        if (NodeFont != null) {
                            NodeFont = new Font(NodeFont, FontStyle.Strikeout);
                        }
                    }
                    if (HeaderVersionObsolete(file2)) {
                        if (Parent != null) {
                            Parent.BackColor = Color.Yellow;
                        }
                        BackColor = Color.Yellow;
                    }
                }

                if (Parent != null && Parent.Text != null && !Parent.Text.Contains("version")) {
                    DBFileHeader header = PackedFileDbCodec.readHeader(file2);
                    Parent.Text = string.Format("{0} - version {1}", Parent.Text, header.Version);
                }
            }
        }
        public static bool HeaderVersionObsolete(PackedFile packedFile) {
            DBFileHeader header = PackedFileDbCodec.readHeader(packedFile);
            string type = DBFile.typename(packedFile.FullPath);
            int maxVersion = DBTypeMap.Instance.MaxVersion(type);
            return DBTypeMap.Instance.IsSupported(type) && maxVersion != 0 && (header.Version < maxVersion);
        }
    }

    /*
     * A tree node representing a directory.
     */
    class DirEntryNode : PackEntryNode {
        public DirEntryNode(VirtualDirectory nodeDir)
            : base(nodeDir) {
            foreach (VirtualDirectory dir in nodeDir.Subdirectories) {
                Nodes.Add(new DirEntryNode(dir));
            }
            foreach (PackedFile file in nodeDir.Files) {
                PackEntryNode node = new PackedFileNode(file);
                Nodes.Add(node);
                node.changeColor(file);
            }
            nodeDir.DirectoryAdded += insertNew;
            nodeDir.FileAdded += insertNew;
            nodeDir.FileRemoved += removeEntry;
        }
        private void removeEntry(PackEntry entry) {
            TreeNode remove = null;
            foreach (TreeNode node in Nodes) {
                if (node.Tag == entry) {
                    remove = node;
                    break;
                }
            }
            if (remove != null) {
                Nodes.Remove(remove);
            }
        }
        private void insertNew(PackEntry entry) {
            PackEntryNode node = null;
            if (entry is PackedFile) {
                node = new PackedFileNode(entry as PackedFile);
            } else {
                node = new DirEntryNode(entry as VirtualDirectory);
            }
            node.Added = true;
            int index = 0;
            List<PackEntry> entries = (Tag as VirtualDirectory).Entries;
            for (index = 0; index < entries.Count; index++) {
                if (entries[index] == entry) {
                    break;
                }
            }
            Nodes.Insert(index, node);
            node.changeColor(entry);

            changeColor(Tag as PackEntry);
            PackEntryNode parent = Parent as PackEntryNode;
            while (parent != null) {
                parent.changeColor(parent.Tag as PackEntry);
                parent = parent.Parent as PackEntryNode;
            }

            Expand();
        }
    }
}
