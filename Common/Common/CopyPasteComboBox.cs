namespace Common
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows.Forms;

    public class CopyPasteComboBox : ToolStripComboBox
    {
        public Keys CopyShortcutKeys = (Keys.Control | Keys.C);
        public Keys PasteShortcutKeys = (Keys.Control | Keys.V);

        public event EventHandler CopyEvent;

        public event EventHandler PasteEvent;

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (((keyData & this.CopyShortcutKeys) == this.CopyShortcutKeys) && (this.CopyEvent != null))
            {
                this.CopyEvent(this, EventArgs.Empty);
            }
            else if (((keyData & this.PasteShortcutKeys) == this.PasteShortcutKeys) && (this.PasteEvent != null))
            {
                this.PasteEvent(this, EventArgs.Empty);
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
            return true;
        }
    }
}

