using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace MMS {
    public partial class InputBox : Form {
        public InputBox() {
            InitializeComponent();

            valueField.KeyDown += delegate(object o, KeyEventArgs args) {
                if (args.KeyCode == Keys.Return || args.KeyCode == Keys.Enter) {
                    CloseWithOk();
                    args.Handled = true;
                } else if (args.KeyCode == Keys.Escape) {
                    CloseWithCancel();
                }
            };
        }

        public string InputValue {
            get {
                return valueField.Text;
            }
            set {
                valueField.Text = value;
            }
        }

        private void CloseWithOk(object sender = null, EventArgs e = null) {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void CloseWithCancel(object sender = null, EventArgs e = null) {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }
    }
}
