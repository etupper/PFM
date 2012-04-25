using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Filetypes;

namespace PackFileManager {
    public partial class GroupformationEditorControl : UserControl {
        public delegate string ListItemRenderer<T>(T o);
        public static readonly ListItemRenderer<object> ToStringRenderer = delegate(object o) { return o.ToString(); };
        
        public GroupformationEditorControl() {
            InitializeComponent();

            linesList.DisplayMember = "Display";
            unitPriorityList.DisplayMember = "Display";
        }

        private Groupformation formation;
        public Groupformation EditedFormation {
            get {
                return formation;
            }
            set {
                formation = value;
                Console.WriteLine("setting formation to {0}", formation.Name);
                nameInput.Text = formation.Name;
                purposeComboBox.Text = ((int) formation.Purpose).ToString();
                FillListBox(factionList, formation.Factions);
                FillListBox(linesList, formation.Lines);
                priorityInput.Text = formation.Priority.ToString();
                
                linesList.SelectedIndexChanged += new EventHandler(LineSelected);
                
                linePriorityInput.Validating += new CancelEventHandler(ValidateFloat);
                spacingInput.Validating += new CancelEventHandler(ValidateFloat);
                crescOffsetInput.Validating += new CancelEventHandler(ValidateFloat);
                xInput.Validating += new CancelEventHandler(ValidateFloat);
                yInput.Validating += new CancelEventHandler(ValidateFloat);
                minThresholdInput.Validating += new CancelEventHandler(ValidateInt);
                maxThresholdInput.Validating += new CancelEventHandler(ValidateInt);
            }
        }
  
        #region Input Validation
        void ValidateFloat(object sender, CancelEventArgs args) {
            float fl;
            if (SelectedLine != null && !float.TryParse((sender as Control).Text, out fl)) {
                args.Cancel = true;
                Console.WriteLine("validation for {1} cancelled with {0}", (sender as Control).Text, SelectedLine);
            }
        }
        void ValidateInt(object sender, CancelEventArgs args) {
            int i;
            if (SelectedLine != null && !int.TryParse((sender as Control).Text, out i)) {
                args.Cancel = true;
                Console.WriteLine("validation for {1} cancelled with {0}", (sender as Control).Text, SelectedLine);
            }
        }
        #endregion
        
        #region Setting Edited Line
        void LineSelected(object sender, EventArgs args) {
            // Console.WriteLine("line selected");
            SelectedLine = linesList.SelectedItem as Line;
        }
        
        Line selectedLine;
        public Line SelectedLine {
            get { return selectedLine; }
            set {
                selectedLine = value;
                RelativeLine relativeLine = selectedLine as RelativeLine;
                relativeToInput.Enabled = relativeLine != null;
                relativeToInput.DataBindings.Clear();
                if (relativeLine != null) {
                    Rebind(relativeToInput, "RelativeTo");
                }
                Rebind(linePriorityInput, "Priority");
                Rebind(spacingInput, "Spacing");
                Rebind(crescOffsetInput, "Crescent_Y_Offset");
                Rebind(maxThresholdInput, "MaxThreshold");
                Rebind(minThresholdInput, "MinThreshold");
                Rebind(xInput, "X");
                Rebind(yInput, "Y");
                if (selectedLine is BasicLine) {
                    BasicLine line = selectedLine as BasicLine;
                    FillListBox(unitPriorityList, line.PriorityClassPairs);
                } else if (selectedLine is SpanningLine) {
                    FillListBox (unitPriorityList, (selectedLine as SpanningLine).Blocks);
                }
            }
        }
        
        void Rebind(TextBox box, string bindTo) {
            box.DataBindings.Clear();
            box.Enabled = SelectedLine is BasicLine;
            if (SelectedLine is BasicLine) {
                box.DataBindings.Add("Text", SelectedLine, bindTo);
            } else {
                box.Text = "";
            }
        }
        #endregion
        
        void FillListBox<T>(ListBox listbox, List<T> list) {
            listbox.Items.Clear();
            list.ForEach(val => { listbox.Items.Add(val); });
        }
        
        private void addFactionButton_Click(object sender, EventArgs e) {
            Console.WriteLine("showing preview");
            Form previewForm = new Form {
                Size = new Size(300, 300)
            };
            Panel drawPanel = new Panel {
                Dock = DockStyle.Fill
            };
            drawPanel.Paint += new PaintEventHandler(PaintFormations);
            previewForm.Controls.Add(drawPanel);
            previewForm.ShowDialog();
        }

        private void deleteFactionButton_Click(object sender, EventArgs e) {

        }

        private void addUnitPriorityButton_Click(object sender, EventArgs e) {

        }

        private void deleteUnitPriorityButton_Click(object sender, EventArgs e) {

        }

        private void editUnitPriorityButton_Click(object sender, EventArgs e) {

        }
        
        private void PaintFormations(object sender, PaintEventArgs args) {
            Control c = sender as Control;
            Graphics g = args.Graphics;
            
            Pen pen = new Pen(Color.Blue, 1.0f);
            Pen redPen = new Pen(Color.Red, 1.0f);
            Font f = new Font(FontFamily.GenericSansSerif, 10.0f);
            Brush b = new SolidBrush(Color.Black);
            foreach(Line line in formation.Lines) {
                Rectangle rect = GetRectangle(line);
                rect.X += c.Width/2;
                rect.Y += c.Height/2;
                rect.Inflate(ItemSize/2, ItemSize/2);
                Console.WriteLine("drawing {0}", rect);
                g.DrawRectangle(line is SpanningLine ? redPen : pen, rect);
                g.DrawString(line.Id.ToString(), f, b, new PointF(rect.X, rect.Y));
            }
        }

        const int ItemSize = 1;
  
        // create the extension of the given line
        Rectangle GetRectangle(Line line) {
            Console.WriteLine("retrieving rect for {0}", line.Id);
            Rectangle result = new Rectangle(0, 0, ItemSize, ItemSize);
            if (line is RelativeLine) {
                BasicLine thisLine = line as RelativeLine;
                Line relativeTo = formation.Lines[(int)(line as RelativeLine).RelativeTo];
                Rectangle relationRect = GetRectangle(relativeTo);
                // result.X = (int) (relationRect.X + thisLine.X) + thisLine.X >= 0 ? ItemSize : -ItemSize;
                if (thisLine.X <= 0) {
                    result.X = (int) (relationRect.X + ItemSize + thisLine.X);
                } else {
                    result.X = (int) (relationRect.X - ItemSize + thisLine.X);
                }
                // result.Y = (int) (relationRect.Y + thisLine.Y) + thisLine.Y >= 0 ? ItemSize : -ItemSize;
                if (thisLine.Y <= 0) {
                    result.Y = (int) (relationRect.Y + ItemSize + thisLine.Y);
                } else {
                    result.Y = (int) (relationRect.Y - ItemSize + thisLine.Y);
                }
            } else if (line is SpanningLine) {
                SpanningLine sl = line as SpanningLine;
                formation.Lines.ForEach(l => {
                    if (sl.Blocks.Contains((uint)l.Id)) {
                        result = Rectangle.Union(result, GetRectangle(l));
                    }
                });
                //result = new Rectangle(minX, minY, Math.Abs(maxX - minX), Math.Abs(maxY - minY));
            }
            Console.WriteLine("rect is {0}", result);
            return result;
        }
    }
}
