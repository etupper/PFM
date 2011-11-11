namespace PackFileManager
{
    using Common;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    public class TextFileEditorControl : UserControl
    {
        private IContainer components;
        private RichTextBox richTextBox1;

        public TextFileEditorControl()
        {
            this.components = null;
            this.InitializeComponent();
        }

        public TextFileEditorControl(PackedFile packedFile)
        {
            this.components = null;
            this.InitializeComponent();
            StreamReader reader = new StreamReader(new MemoryStream(packedFile.Data, false), Encoding.ASCII);
            this.richTextBox1.Show();
            this.richTextBox1.Text = reader.ReadToEnd();
        }

        public void CloseTextFileEditorControl()
        {
            base.Dispose();
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
            this.richTextBox1 = new RichTextBox();
            base.SuspendLayout();
            this.richTextBox1.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.richTextBox1.Location = new Point(0, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new Size(0x4a2, 0x28c);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.Controls.Add(this.richTextBox1);
            base.Name = "TextFileEditorControl";
            base.Size = new Size(0x4a2, 0x28c);
            base.ResumeLayout(false);
        }
    }
}

