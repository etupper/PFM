namespace PackFileManager
{
    using Common;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    public class TextFileEditorControl : PackedFileEditor<string>
    {
        private IContainer components = null;
        private RichTextBox richTextBox1;

        public TextFileEditorControl() : base(TextCodec.Instance) {
            this.InitializeComponent();

            richTextBox1.TextChanged += (b, e) => DataChanged = true;
        }

        public override bool CanEdit(PackedFile file) {
            string[] extensions = { "txt", "lua", "csv", "fx", "fx_fragment", 
                "h", "battle_script", "xml", 
                "tai", "xml.rigging", "placement", "hlsl"
            };
            bool result = false;
            foreach (string ext in extensions) {
                if (file.FullPath.EndsWith(ext)) {
                    result = true;
                    break;
                }
            }
            return result;
        }
        
        public override string EditedFile {
            get {
                return richTextBox1.Text;
            }
            set {
                richTextBox1.Text = value;
                DataChanged = false;
            }
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
            base.Controls.Add(this.richTextBox1);
            base.Name = "TextFileEditorControl";
            base.Size = new Size(0x4a2, 0x28c);
            base.ResumeLayout(false);
        }
    }
    
    public class TextCodec : Codec<string> {
        public static readonly TextCodec Instance = new TextCodec();
        public string Decode(Stream file) {
            string result = "";
            using (var reader = new StreamReader(file, Encoding.ASCII)) {
                result = reader.ReadToEnd();
            }
            return result;
        }
        public void Encode(Stream stream, string toEncode) {
            using (var writer = new StreamWriter(stream)) {
                writer.Write(toEncode);
            }
        }
    }
}

