using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DecodeTool {
    public partial class TypeSelection : UserControl {
        TypeDescription type;
        public TypeDescription Type {
            get {
                return type;
            }
            set {
                type = value;
                if (value != null) {
                    label.Text = value.TypeName;
                }
            }
        }

        public delegate void selection(TypeDescription type);
        public event selection Selected;

        public void ShowPreview(BinaryReader bytes) {
            if (Type != null) {
                preview.Text = Util.decodeSafe(Type, bytes);
            }
        }

        public TypeSelection() {
            InitializeComponent();
        }

        private void button_Click(object sender, EventArgs e) {
            if (Selected != null) {
                Selected(Type);
            }
        }
    }
}
