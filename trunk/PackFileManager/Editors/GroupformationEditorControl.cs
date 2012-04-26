using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using Filetypes;

namespace PackFileManager {
    public delegate T Parser<T>(string parse);
    
    interface ILineEditor {
        void SetLine(BasicLine line);
    }
    public interface IModifiable {
        void SetModified(bool val);
    }

    public partial class GroupformationEditorControl : UserControl, IModifiable {
        static Form PreviewWindow = new Form {
            Size = new Size(300, 300)
        };
        
        public bool Modified { get; set; }
        
        public delegate string ListItemRenderer<T>(T o);
        public static readonly ListItemRenderer<object> ToStringRenderer = delegate(object o) { return o.ToString(); };
        
        List<ILineEditor> bindings = new List<ILineEditor>();
  
        delegate void PropertySetter(TextBox box);
        public GroupformationEditorControl() {
            InitializeComponent();

            linesList.DisplayMember = "Display";
            unitPriorityList.DisplayMember = "Display";

            // create all the text bindings
            bindings.Add(new TextBinding<uint>(relativeToInput, uint.Parse, "RelativeTo", this));
            bindings.Add(new TextBinding<float>(linePriorityInput, float.Parse, "Priority", this));
            bindings.Add(new TextBinding<float>(spacingInput, float.Parse, "Spacing", this));
            bindings.Add(new TextBinding<float>(crescOffsetInput, float.Parse, "Crescent_Y_Offset", this));
            bindings.Add(new TextBinding<float>(xInput, float.Parse, "X", this));
            bindings.Add(new TextBinding<float>(yInput, float.Parse, "Y", this));
            bindings.Add(new TextBinding<int>(minThresholdInput, int.Parse, "MinThreshold", this));
            bindings.Add(new TextBinding<int>(maxThresholdInput, int.Parse, "MaxThreshold", this));
        }
        public void SetModified(bool val) {
            Modified = val;
            formationPreview.Formation = EditedFormation;
            formationPreview.Invalidate();
        }
        
        private Groupformation formation;
        public Groupformation EditedFormation {
            get {
                return formation;
            }
            set {
                formation = value;
                nameInput.Text = formation.Name;
                purposeComboBox.Text = ((int)formation.Purpose).ToString();
                FillListBox(factionList, formation.Factions);
                FillListBox(linesList, formation.Lines);
                priorityInput.Text = formation.Priority.ToString();

                linesList.SelectedIndexChanged += new EventHandler(LineSelected);
                
                formationPreview.Formation = formation;
            }
        }

        #region Setting Edited Line
        void LineSelected(object sender, EventArgs args) {
            SelectedLine = linesList.SelectedItem as Line;
        }
        
        Line selectedLine;
        public Line SelectedLine {
            get { return selectedLine; }
            set {
                BasicLine relativeLine = value as BasicLine;
                foreach(ILineEditor editor in bindings) {
                    editor.SetLine(relativeLine);
                }

                selectedLine = value;
                if (selectedLine is BasicLine) {
                    BasicLine line = selectedLine as BasicLine;
                    FillListBox(unitPriorityList, line.PriorityClassPairs);
                } else if (selectedLine is SpanningLine) {
                    FillListBox(unitPriorityList, (selectedLine as SpanningLine).Blocks);
                }
                formationPreview.SelectedLine = value;
            }
        }
        #endregion

        void FillListBox<T>(ListBox listbox, List<T> list) {
            listbox.Items.Clear();
            list.ForEach(val => { listbox.Items.Add(val); });
        }

        private void addFactionButton_Click(object sender, EventArgs e) {

        }

        private void deleteFactionButton_Click(object sender, EventArgs e) {

        }

        private void addUnitPriorityButton_Click(object sender, EventArgs e) {

        }

        private void deleteUnitPriorityButton_Click(object sender, EventArgs e) {

        }

        private void editUnitPriorityButton_Click(object sender, EventArgs e) {

        }

        private void preview_Click(object sender, EventArgs e) {
            Console.WriteLine("showing preview");
            Form previewForm = new Form {
                Size = new Size(300, 300)
            };
            FormationPreview drawPanel = new FormationPreview {
                Dock = DockStyle.Fill,
                Formation = EditedFormation
            };
            previewForm.Controls.Add(drawPanel);
            previewForm.ShowDialog();
        }
    }

    /*
     * Class implementing the binding of text box to a Relative Line's property.
     * Need to do this manually because the mono implementation doesn't seem to respect the
     * DataBindings.Clear() and will still set values to past objects.
     */
    public class TextBinding<T> : ILineEditor {
        Parser<T> Parse;
        TextBox TextBox;
        PropertyInfo Info;
        BasicLine line;
        IModifiable NotificationTarget;
        
        // create an instance, bound to the given text box, using the given parser
        // to set the given property.
        public TextBinding(TextBox box, Parser<T> parser, string propertyName, IModifiable notify) {
            Parse = parser;
            Info = typeof(RelativeLine).GetProperty(propertyName);
            if (Info == null) {
                throw new ArgumentException(string.Format("property {0} not valid", propertyName));
            }
            TextBox = box;
            box.LostFocus += delegate(object sender, EventArgs args) { SetValue(); };
            box.Validating += Validator;
            NotificationTarget = notify;
        }

        // the line for which to edit the property
        public BasicLine Line { 
            get {
                return line;
            }
            set {
                SetValue ();
                line = value;
                try {
                if (line != null) {
                    object val = Info.GetValue(line, null);
                    TextBox.Text = val.ToString();
                    TextBox.Enabled = true;
                } else {
                    TextBox.Text = "";
                    TextBox.Enabled = false;
                }
                } catch {
                    TextBox.Text = "";
                    TextBox.Enabled = false;
                }
            }
        }
        
        // implement ILineEditor
        public void SetLine(BasicLine l) {
            Line = l;
        }
        
        // helper method, called from Line property and LostFocus event
        void SetValue() {
            if (line as BasicLine != null && TextBox.Enabled) {
                try {
                    T newValue = Parse(TextBox.Text);
                    T oldValue = (T) Info.GetValue(line, null);
                    
                    if (Comparer<T>.Default.Compare(oldValue, newValue) != 0) {
                        Info.SetValue(line, Parse(TextBox.Text), null);
                        if (NotificationTarget != null) {
                            NotificationTarget.SetModified(true);
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine(string.Format("setting value {0} to {1} failed: {2}", Info.Name, TextBox.Text, e));
                }
            }
        }
        
        // validates the box by trying to parse its text with the parser
        // cancels upon exception
        void Validator(object sender, CancelEventArgs args) {
            try {
                if (line != null && TextBox.Enabled) {
                    Parse((sender as TextBox).Text);
                }
            } catch {
                Console.WriteLine("Failed validation of {0} for {1}", Info.Name, TextBox.Text);
                args.Cancel = true;
            }
        }
    }
    
}
