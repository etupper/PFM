using System;
using System.Windows.Forms;
using EsfLibrary;

namespace EditSF {
    public class EsfTreeNode : TreeNode {
        private bool showCode;
        public bool ShowCode {
            get { return showCode; }
            set {
                NamedNode node = Tag as NamedNode;
                Text = value ? string.Format("{0} - {1}", node.Name, node.TypeCode) : node.Name;
                showCode = value;
                foreach (TreeNode child in Nodes) {
                    (child as EsfTreeNode).ShowCode = value;
                }
            }
        }
        public EsfTreeNode(NamedNode node, bool showC = false) {
            Tag = node;
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
