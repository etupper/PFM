using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
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
                    rootNode = new EsfTreeNode(value as NamedNode);
                    rootNode.ShowCode = ShowCode;
                    esfNodeTree.Nodes.Add(rootNode);
                    rootNode.Fill();
                    nodeValueGridView.Rows.Clear();
                    value.Modified = false;
                }
            }
        }

        public bool showCode;
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

    public class EsfTreeNode : TreeNode {
        private bool showCode;
        public bool ShowCode {
            get { return showCode; }
            set {
                NamedNode node = Tag as NamedNode;
                if (node != null) {
                    Text = value ? string.Format("{0} - {1}", node.Name, node.TypeCode) : node.Name;
                    showCode = value;
                    foreach (TreeNode child in Nodes) {
                        (child as EsfTreeNode).ShowCode = value;
                    }
                }
            }
        }
        public EsfTreeNode(NamedNode node, bool showC = false) {
            Tag = node;
            Text = node.Name;
            node.ModifiedEvent += delegate(EsfNode n) { ForeColor = n.Modified ? Color.Red : Color.Black; };
            ShowCode = showC;
        }
        public void Fill() {
            if (Nodes.Count == 0) {
                foreach (NamedNode child in (Tag as NamedNode).Children) {
                    Nodes.Add(new EsfTreeNode(child, ShowCode));
                }
            }
        }
    }

    public class TreeEventHandler {
        DataGridView nodeValueGridView;
        public TreeEventHandler(DataGridView view) {
            nodeValueGridView = view;
        }
        public void FillNode(object sender, TreeViewCancelEventArgs args) {
            foreach (TreeNode child in args.Node.Nodes) {
                EsfTreeNode esfNode = child as EsfTreeNode;
                if (esfNode != null) {
                    esfNode.Fill();
                }
            }
        }
        public void NodeSelected(object sender, TreeViewEventArgs args) {
            NamedNode node = args.Node.Tag as NamedNode;
            try {
                nodeValueGridView.Rows.Clear();
                foreach (EsfNode value in node.Values) {
                    int index = nodeValueGridView.Rows.Add(value.ToString(), value.SystemType.ToString(), value.TypeCode.ToString());
                    nodeValueGridView.Rows[index].Tag = value;
                }
            } catch {
            }
        }
    }
}
