using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using Filetypes;
using Common;
using EsfLibrary;

namespace PackFileManager {
    public partial class PackedEsfEditor : PackedFileEditor<EsfNode> {
        public PackedEsfEditor() : base(new DelegatingEsfCodec()) {
            InitializeComponent();
        }

        string[] EXTENSIONS = { ".esf" };
        public override bool CanEdit(PackedFile file) {
            return HasExtension(file, EXTENSIONS);
        }

        public override EsfNode EditedFile {
            get {
                return base.EditedFile;
            }
            set {
                if (EditedFile != null) {
                    EditedFile.ModifiedEvent -= SetModified;
                }
                esfComponent.RootNode = value;
                base.EditedFile = value;
                EditedFile.ModifiedEvent += SetModified;
                DataChanged = false;
            }
        }

        private void SetModified(EsfNode n) {
            DataChanged = true;
        }
    }

    class DelegatingEsfCodec : Codec<EsfNode> {
        EsfCodec codecDelegate;
        public EsfNode Decode(Stream stream) {
            codecDelegate = EsfCodecUtil.GetCodec(stream);
            return codecDelegate.Parse(stream);
        }
        public void Encode(Stream encodeTo, EsfNode node) {
            using (var writer = new BinaryWriter(encodeTo)) {
                codecDelegate.EncodeRootNode(writer, node);
            }
        }
    }
}
