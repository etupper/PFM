namespace PackFileManager
{
    using Common;
    using FreeImageAPI;
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Windows.Forms;

    public class ImageViewerControl : UserControl
    {
        private AtlasFile atlasFile;
        private Image bmp;
        private Button button1;
        private Button button2;
        private Button button3;
        private IContainer components = null;
        private string file;
        private Rectangle[] grid;
        private ImageViewerControl imageViewerControl;
        private ToolTip pbToolTip;
        private PictureBox pictureBox1;
        private SizeF sizeFactor;
        private ToolTipRegion[] toolTipRegions;

        public ImageViewerControl()
        {
            this.InitializeComponent();
            this.imageViewerControl = this;
            this.pictureBox1.Resize += new EventHandler(this.pictureBox1_Resize);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = "Atlas File(*.atlas)|*.atlas"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            this.atlasFile = new AtlasFile();
                            this.atlasFile.ReadAtlasFile(reader);
                            this.CreateGrid();
                        }
                    }
                }
                catch (IOException)
                {
                    MessageBox.Show("wtf?");
                }
            }
            else
            {
                dialog.Dispose();
            }
        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.button1, "Generate a grid overlay from an extracted .atlas file.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.SetImage(this.file);
        }

        private void button2_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.button2, "Remove the grid overlay from a .dds texture.");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog {
                FileName = "AtlasGrid",
                Filter = "PNG File(*.png)|*.png"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.drawBitmap(dialog.FileName);
            }
        }

        private void button3_MouseHover(object sender, EventArgs e)
        {
            new ToolTip().SetToolTip(this.button3, "Save the .atlas grid overlay to a PNG file.");
        }

        public void CloseImageViewerControl()
        {
            this.pictureBox1.Dispose();
            base.Dispose();
        }

        public void CreateGrid()
        {
            this.atlasFile.setPixelUnits((float) this.bmp.Height);
            this.grid = new Rectangle[this.atlasFile.numEntries];
            this.toolTipRegions = new ToolTipRegion[this.atlasFile.numEntries];
            for (int i = 0; i < this.grid.Length; i++)
            {
                AtlasObject aO = this.atlasFile.Entries[i];
                this.toolTipRegions[i] = new ToolTipRegion(aO);
                this.grid[i] = new Rectangle((int) aO.PX1, (int) aO.PY1, (int) aO.X3, (int) aO.Y3);
            }
            using (Graphics graphics = Graphics.FromImage(this.pictureBox1.Image))
            {
                Pen pen = new Pen(Color.Red, 4f);
                graphics.DrawRectangles(pen, this.grid);
            }
            this.pictureBox1.Refresh();
            this.button2.Enabled = true;
            this.button1.Enabled = false;
            this.button3.Enabled = true;
            this.button3.Click += new EventHandler(this.button3_Click);
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

        private void drawBitmap(string filePath)
        {
            using (Bitmap bitmap = new Bitmap(this.bmp.Width, this.bmp.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    Pen pen = new Pen(Color.Red, 3f);
                    graphics.DrawRectangles(Pens.Red, this.grid);
                }
                using (FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    using (MemoryStream stream2 = new MemoryStream())
                    {
                        bitmap.Save(stream2, ImageFormat.Png);
                        stream2.WriteTo(stream);
                    }
                }
            }
        }

        public void DrawImage()
        {
            Point location = this.pictureBox1.Location;
            Point point2 = new Point {
                X = 0x200,
                Y = 0x200
            };
            using (Graphics graphics = Graphics.FromImage(this.pictureBox1.Image))
            {
                Pen pen = new Pen(Color.Red, 3f);
                graphics.DrawRectangle(pen, location.X, location.Y, point2.X, point2.Y);
            }
        }

        private SizeF GetScalingFactor()
        {
            Point location = this.pictureBox1.Location;
            double num = this.pictureBox1.Size.Height * this.pictureBox1.Size.Width;
            double num2 = this.bmp.Height * this.bmp.Width;
            double num3 = Math.Sqrt(num / num2);
            return new SizeF { Height = (float) num3, Width = (float) num3 };
        }

        private void InitializeComponent()
        {
            this.pictureBox1 = new PictureBox();
            this.button1 = new Button();
            this.button2 = new Button();
            this.button3 = new Button();
            ((ISupportInitialize) this.pictureBox1).BeginInit();
            base.SuspendLayout();
            this.pictureBox1.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.pictureBox1.Location = new Point(0, 0x20);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new Size(0x390, 0x27b);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.button1.Location = new Point(14, 3);
            this.button1.Name = "button1";
            this.button1.Size = new Size(0x53, 0x17);
            this.button1.TabIndex = 1;
            this.button1.Text = "Load .atlas";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new EventHandler(this.button1_Click);
            this.button2.Location = new Point(0x67, 3);
            this.button2.Name = "button2";
            this.button2.Size = new Size(0x53, 0x17);
            this.button2.TabIndex = 2;
            this.button2.Text = "Unload .atlas";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new EventHandler(this.button2_Click);
            this.button3.Location = new Point(0xc0, 3);
            this.button3.Name = "button3";
            this.button3.Size = new Size(0x53, 0x17);
            this.button3.TabIndex = 3;
            this.button3.Text = "Export Grid";
            this.button3.UseVisualStyleBackColor = true;
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            base.Controls.Add(this.button3);
            base.Controls.Add(this.button2);
            base.Controls.Add(this.button1);
            base.Controls.Add(this.pictureBox1);
            base.Name = "ImageViewerControl";
            base.Size = new Size(0x393, 0x29b);
            ((ISupportInitialize) this.pictureBox1).EndInit();
            base.ResumeLayout(false);
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
        }

        public void SetImage(string filePath)
        {
            this.file = filePath;
            this.button3.Enabled = false;
            this.button3.MouseHover += new EventHandler(this.button3_MouseHover);
            this.button2.Enabled = false;
            this.button2.MouseHover += new EventHandler(this.button2_MouseHover);
            this.button1.MouseHover += new EventHandler(this.button1_MouseHover);
            if (this.file.EndsWith(".dds"))
            {
                this.button1.Enabled = true;
            }
            else
            {
                this.button1.Enabled = false;
            }
            FreeImageBitmap bitmap = new FreeImageBitmap(filePath);
            bitmap.ConvertType(FREE_IMAGE_TYPE.FIT_BITMAP, true);
            this.bmp = (Bitmap)bitmap;
            this.pictureBox1.Enabled = true;
            this.pictureBox1.Image = this.bmp;
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.GetScalingFactor();
        }
    }
}

