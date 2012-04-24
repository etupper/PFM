using System;
using System.IO;
using System.Windows.Forms;
using Common;
using Filetypes;

namespace PackFileManager {
    public class GroupformationEditor : PackedFileEditor<GroupformationFile> {
        TabControl tabControl = new TabControl();
        public GroupformationEditor (PackedFile file) : base(new GroupformationCodec()) {
            CurrentPackedFile = file;
            foreach (Groupformation formation in EditedFile.Formations) {
                TabPage tabPage = new TabPage(formation.Name);
                tabControl.TabPages.Add(tabPage);
            }
        }
    }
}

