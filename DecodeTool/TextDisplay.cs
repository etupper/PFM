using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DecodeTool {
    public partial class TextDisplay : Form {
        public TextDisplay(string toDisplay) {
            InitializeComponent();
            text.Text = toDisplay;
        }

        private void close_Click(object sender, EventArgs e) {
            Dispose();
        }
    }
}
