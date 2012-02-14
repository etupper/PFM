namespace Common
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public class ErrorBox : Form
    {
        private IContainer components = null;
        private RichTextBox errorTextBox;

        private ErrorBox(Exception ex)
        {
            this.InitializeComponent();
            this.Text = ex.GetType().Name;
            this.errorTextBox.Text = string.Format("{0}\r\n\r\nStack trace:\r\n{1}", ex.Message, ex.StackTrace);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.errorTextBox = new RichTextBox();
            base.SuspendLayout();
            this.errorTextBox.Dock = DockStyle.Fill;
            this.errorTextBox.Location = new Point(0, 0);
            this.errorTextBox.Name = "errorTextBox";
            this.errorTextBox.ReadOnly = true;
            this.errorTextBox.Size = new Size(0x248, 0x234);
            this.errorTextBox.TabIndex = 0;
            this.errorTextBox.Text = "";
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.ClientSize = new Size(0x248, 0x234);
            base.Controls.Add(this.errorTextBox);
            base.MaximizeBox = false;
            base.MinimizeBox = false;
            base.Name = "ErrorDialog";
            base.ShowIcon = false;
            base.ShowInTaskbar = false;
            base.StartPosition = FormStartPosition.CenterParent;
            this.Text = "ErrorDialog";
            base.TopMost = true;
            base.ResumeLayout(false);
        }

        public static void ShowDialog(Exception ex)
        {
            new ErrorBox(ex).ShowDialog();
        }
    }
}

