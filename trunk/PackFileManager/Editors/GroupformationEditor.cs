using System;
using System.IO;
using System.Windows.Forms;
using Common;
using Filetypes;

namespace PackFileManager {
    public class GroupformationEditor : PackedFileEditor<GroupformationFile> {
        TabControl tabControl = new TabControl {
            Name = "tabControl",
            Multiline = true
        };
        
        public override GroupformationFile EditedFile {
            get {
                return base.EditedFile;
            }
            set {
                base.EditedFile = value;
                Console.WriteLine("opening group formations");
                base.SuspendLayout();
                this.Dock = DockStyle.Fill;
                // Console.WriteLine("edited file now {0}", EditedFile);
                foreach (Groupformation formation in EditedFile.Formations) {
                    // Console.WriteLine("adding tab {0}", formation.Name);
                    TabPage tabPage = new TabPage(formation.Name) {
                        Dock = DockStyle.Fill,
                        AutoScroll = true
                    };
                    GroupformationEditorControl editor = new GroupformationEditorControl {
                        Dock = DockStyle.Fill,
                        EditedFormation = formation
                    };
                    tabPage.Controls.Add(editor);
                    tabControl.TabPages.Add(tabPage);
                }
                tabControl.Dock = DockStyle.Fill;
                Controls.Add(tabControl);
                base.ResumeLayout(true);
            }
        }
        
        public GroupformationEditor () : base(new GroupformationCodec()) {
        }
    }
}

