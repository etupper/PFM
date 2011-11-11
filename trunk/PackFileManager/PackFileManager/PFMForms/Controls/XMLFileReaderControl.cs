namespace PackFileManager.PFMForms.Controls
{
    using Common;
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;

    public class XMLFileReaderControl : UserControl
    {
        private IContainer components = null;

        public XMLFileReaderControl()
        {
            this.InitializeComponent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                Utilities.DisposeHandlers(this);
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            base.AutoScaleMode = AutoScaleMode.Font;
        }
    }
}

