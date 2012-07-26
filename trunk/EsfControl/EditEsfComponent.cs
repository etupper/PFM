using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using EsfLibrary;

namespace EsfControl {
    public partial class EditEsfComponent : UserControl {
        TreeEventHandler treeEventHandler;

        EsfTreeNode rootNode;
        public EsfNode RootNode {
            get {
                return rootNode != null ? rootNode.Tag as EsfNode : null;
            }
            set {
                esfNodeTree.Nodes.Clear();
                if (value != null) {
                    rootNode = new EsfTreeNode(value as ParentNode);
                    rootNode.ShowCode = ShowCode;
                    esfNodeTree.Nodes.Add(rootNode);
                    rootNode.Fill();
                    nodeValueGridView.Rows.Clear();
                    value.Modified = false;
                }
            }
        }

        bool showCode;
        public bool ShowCode {
            get { return showCode; }
            set {
                showCode = value;
                if (esfNodeTree.Nodes.Count > 0) {
                    (esfNodeTree.Nodes[0] as EsfTreeNode).ShowCode = value;
                    nodeValueGridView.Columns["Code"].Visible = value;
                }
            }
        }
        public EditEsfComponent() {
            InitializeComponent();
            nodeValueGridView.Rows.Clear();

            treeEventHandler = new TreeEventHandler(nodeValueGridView);
            esfNodeTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(treeEventHandler.FillNode);
            esfNodeTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(treeEventHandler.NodeSelected);

            nodeValueGridView.CellValidating += new DataGridViewCellValidatingEventHandler(validateCell);
            nodeValueGridView.CellEndEdit += new DataGridViewCellEventHandler(cellEdited);

            MouseHandler mouseHandler = new MouseHandler();
            esfNodeTree.MouseUp += new MouseEventHandler(mouseHandler.ShowContextMenu);
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
                    Console.WriteLine("Invalid value {0}", newValue);
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
    }

    public class MouseHandler {
        public delegate void NodeAction(EsfNode node);

        public void ShowContextMenu(object sender, System.Windows.Forms.MouseEventArgs e) {
            TreeView treeView = sender as TreeView;
            if (e.Button == MouseButtons.Right && treeView != null) {
                // Point where the mouse is clicked.
                Point p = new Point(e.X, e.Y);
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                // Get the node that the user has clicked.
                TreeNode node = treeView.GetNodeAt(p);
                ParentNode selectedNode = (node != null) ? node.Tag as ParentNode : null;
                if (selectedNode != null && (node.Tag as EsfNode).Parent is RecordArrayNode) {
                    treeView.SelectedNode = node;

                    ToolStripItem toolItem = CreateMenuItem("Duplicate", selectedNode, CopyNode);
                    contextMenu.Items.Add(toolItem);
                    toolItem = CreateMenuItem("Delete", selectedNode, DeleteNode);
                    contextMenu.Items.Add(toolItem);
                }
                
                if (contextMenu.Items.Count != 0) {
                    contextMenu.Show(treeView, p);
                }
            }
        }
        
        private ToolStripMenuItem CreateMenuItem(String label, EsfNode node, NodeAction action) {
            ToolStripMenuItem item = new ToolStripMenuItem(label);
            item.Click += new EventHandler(delegate(object s, EventArgs args) { action(node); });
            return item;
        }
        
        private void CopyNode(EsfNode node) {
            ParentNode toCopy = node as ParentNode;
            ParentNode copy;
            copy = toCopy.CreateCopy() as ParentNode;
            if (copy != null) {
                ParentNode parent = toCopy.Parent as ParentNode;
                if (parent != null) {
                    List<EsfNode> nodes = new List<EsfNode>((toCopy.Parent as RecordArrayNode).Value);
                    int insertAt = nodes.Count;
                    insertAt = parent.Children.IndexOf(toCopy) + 1;
                    nodes.Insert(insertAt, copy);
                    (toCopy.Parent as RecordArrayNode).Value = nodes;
                    copy.Modified = true;
                    copy.AllNodes.ForEach(n => n.Modified = false);
                }
            }
        }

        private void DeleteNode(EsfNode node) {
            ParentNode toCopy = node as ParentNode;
            ParentNode parent = toCopy.Parent as ParentNode;
            if (parent != null) {
                List<EsfNode> nodes = new List<EsfNode>((toCopy.Parent as RecordArrayNode).Value);
                nodes.Remove(node);
                (toCopy.Parent as RecordArrayNode).Value = nodes;
            }
        }
    }
    
    public class EsfTreeNode : TreeNode {
        private bool showCode;
        public bool ShowCode {
            get { return showCode; }
            set {
                ParentNode node = Tag as ParentNode;
                if (node != null) {
                    string baseName = (node as INamedNode).GetName();
                    Text = value ? string.Format("{0} - {1}", baseName, node.TypeCode) : baseName;
                    showCode = value;
                    foreach (TreeNode child in Nodes) {
                        (child as EsfTreeNode).ShowCode = value;
                    }
                }
            }
        }
        public EsfTreeNode(ParentNode node, bool showC = false) {
            Tag = node;
            Text = (node as INamedNode).GetName();
            node.ModifiedEvent += NodeChange;
            ForeColor = node.Modified ? Color.Red : Color.Black;
            ShowCode = showC;
        }
        public void Fill() {
            if (Nodes.Count == 0) {
                ParentNode parentNode = (Tag as ParentNode);
                foreach (ParentNode child in parentNode.Children) {
                    Nodes.Add(new EsfTreeNode(child, ShowCode));
                }
            }
        }
        public void NodeChange(EsfNode n) {
            ForeColor = n.Modified ? Color.Red : Color.Black;
            ParentNode node = (Tag as ParentNode);
            if (node != null && node.Children.Count != this.Nodes.Count) {
                Nodes.Clear();
                Fill();
                if (IsExpanded) {
                    foreach (TreeNode child in Nodes) {
                        (child as EsfTreeNode).Fill();
                    }
                }
            }
        }
    }

    public class TreeEventHandler {
        private List<ModificationColorizer> registeredEvents = new List<ModificationColorizer>();
        
        DataGridView nodeValueGridView;
        public TreeEventHandler(DataGridView view) {
            nodeValueGridView = view;
        }
        /*
         * Fill the event's target tree node's children with their children
         * (to show the [+] if they contain child nodes).
         */
        public void FillNode(object sender, TreeViewCancelEventArgs args) {
            foreach (TreeNode child in args.Node.Nodes) {
                EsfTreeNode esfNode = child as EsfTreeNode;
                if (esfNode != null) {
                    esfNode.Fill();
                }
            }
        }

        /*
         * Render the data cell view, preparing the red color for modified entries.
         */
        public void NodeSelected(object sender, TreeViewEventArgs args) {
            ParentNode node = args.Node.Tag as ParentNode;
            try {
                nodeValueGridView.Rows.Clear();
                registeredEvents.ForEach(handler => { (handler.row.Tag as EsfNode).ModifiedEvent -= handler.ChangeColor; });
                registeredEvents.Clear();
                foreach (EsfNode value in node.Values) {
                    int index = nodeValueGridView.Rows.Add(value.ToString(), value.SystemType.ToString(), value.TypeCode.ToString());
                    DataGridViewRow newRow = nodeValueGridView.Rows [index];
                    ModificationColorizer colorizer = new ModificationColorizer(newRow);
                    registeredEvents.Add(colorizer);
                    foreach (DataGridViewCell cell in newRow.Cells) {
                        cell.Style.ForeColor = value.Modified ? Color.Red : Color.Black;
                    }
                    value.ModifiedEvent += colorizer.ChangeColor;
                    
                    newRow.Tag = value;
                }
            } catch {
            }
        }
    }
    
    public class ModificationColorizer {
        public DataGridViewRow row;
        public ModificationColorizer(DataGridViewRow r) {
            row = r;
        }
        public void ChangeColor(EsfNode node) {
            foreach (DataGridViewCell cell in row.Cells) {
                cell.Style.ForeColor = node.Modified ? Color.Red : Color.Black;
            }
        }
    }
}
