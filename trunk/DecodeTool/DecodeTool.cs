using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Common;
using Filetypes;

namespace DecodeTool {
    public partial class DecodeTool : Form {
        byte[] bytes;
        List<FieldInfo> types = new List<FieldInfo>();
        int offset = 0;
        int displayIndex = 0;
        string typeName = "";
        int newFieldVersion = -1;

        #region Attributes
        public byte[] Bytes {
			set {
				bytes = value;
				string formatted = Util.formatHex (bytes);
				
				// my mono on linux won't handle more...
                int maxLength = 3 * 2048;
				if (formatted.Length > maxLength) 
					formatted = formatted.Substring (0, maxLength);
				hexView.Text = formatted;

                try {
                    using (var stream = new MemoryStream(value)) {
                        DBFileHeader header = PackedFileDbCodec.readHeader(stream);
                        newFieldVersion = header.Version;
                        Guid = header.GUID;
                    }
                } catch {
                    newFieldVersion = -1;
                }

                showInPreview();
			}
			get {
				return bytes;
			}
		}
        public string TypeName {
            get { return typeName; }
            set { typeName = value; typeNameLabel.Text = string.Format("Type: {0}", typeName); }
        }
        string guid = "";
        public string Guid {
            get { return guid; }
            set {
                typeNameLabel.Text = string.Format("Type: {0}, version {1} - {2}", TypeName, newFieldVersion, value);
                guid = value;
            }
        }
        public GuidTypeInfo GuidInfo {
            get {
                return new GuidTypeInfo(Guid, TypeName, newFieldVersion);
            }
        }
        int Offset {
            get { return offset; }
            set { 
                offset = value;
                headerLength.Text = value.ToString();
                setSelection(); 
            }
        }
        int KnownByteCount {
            get {
                int index = offset;
                if (Bytes != null) {
                    using (BinaryReader reader = new BinaryReader(new MemoryStream(Bytes))) {
                        reader.BaseStream.Position = offset;
                        types.ForEach(delegate(FieldInfo d) {
                            int temp;
                            Util.decodeSafe(d, reader, out temp); index += temp; 
                        });
                    }
                }
                return index;
            }
        }
        #endregion

        public DecodeTool() {
            InitializeComponent();

            stringType.Factory = Types.StringType;
            stringType.Selected += addType;

            intType.Factory = Types.IntType;
            intType.Selected += addType;

            boolType.Factory = Types.BoolType;
            boolType.Selected += addType;

            singleType.Factory = Types.SingleType;
            singleType.Selected += addType;

            optStringType.Factory = Types.OptStringType;
            optStringType.Selected += addType;
        }

        #region Type Management
        private void addType(FieldInfo type) {
            if (newFieldVersion != -1) {
                type.StartVersion = newFieldVersion;
            }
            int insertAt = typeList.SelectedIndex;
            if (typeList.SelectedIndex != -1) {
                types.Insert(typeList.SelectedIndex, type);
            } else {
                types.Add(type);
            }
            setSelection();
            if (insertAt != -1) { typeList.SelectedIndex = insertAt; }
        }
        private void delete_Click(object sender, EventArgs e) {
            int selectIndex = -1;
            if (typeList.SelectedIndex == -1) {
                if (types.Count > 0) {
                    types.RemoveAt(types.Count - 1);
                }
            } else {
                selectIndex = typeList.SelectedIndex;
                types.RemoveAt(typeList.SelectedIndex);
            }
            setSelection();
            if (selectIndex != -1 && selectIndex < typeList.Items.Count) {
                typeList.SelectedIndex = selectIndex;
            }
        }
        #endregion

        private void setSelection() {
			if (bytes == null) {
				return;
			}

			color (0, bytes.Length, Color.Black);
			color (offset, KnownByteCount - offset, Color.Blue);
			color (0, offset, Color.Red);
			hexView.Select (0, 0);

			using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes))) {
				showPreview(reader, KnownByteCount);

				typeList.Items.Clear ();
				valueList.Items.Clear ();
				skipToCurrentEntry(reader);
				string s;
				int start = (int)reader.BaseStream.Position;
				int end = start;
				types.ForEach (delegate(FieldInfo d) {
					int length;
					s = Util.decodeSafe (d, reader, out length);
					typeList.Items.Add (d.TypeName);
					valueList.Items.Add (s);
					end += length;
				});
				color (start, end - start, Color.Green);
				if (offset > 4) {
					reader.BaseStream.Position = offset - 4;
					int assumedEntryCount = reader.ReadInt32 ();
					string infoString = string.Format ("trying to read {0} entries: ", assumedEntryCount);
					uint totalBytes = 0;
					try {
						for (int i = 0; i < assumedEntryCount; i++) {
							try {
								uint tempBytes = 0;
								types.ForEach (delegate(FieldInfo d) { 
									s = d.Decode (reader); 
									tempBytes += (uint)d.Length (s); 
								});
								totalBytes += tempBytes;
							} catch (Exception x) {
								infoString = string.Format ("{0} {1} at entry {2}", infoString, x.Message, i);
								throw x;
							}
						}
						infoString = string.Format ("{0} read {1} entries, {2}/{3} bytes", 
                            infoString, assumedEntryCount, totalBytes, bytes.Length - offset);
					} catch (Exception) {
					}
					repeatInfo.Text = infoString;
				}
			}
		}

        private void showPreview(BinaryReader reader, long position) {
            reader.BaseStream.Position = position;
            intType.ShowPreview(reader);
            reader.BaseStream.Position = position;
            stringType.ShowPreview(reader);
            reader.BaseStream.Position = position;
            boolType.ShowPreview(reader);
            reader.BaseStream.Position = position;
            singleType.ShowPreview(reader);
            reader.BaseStream.Position = position;
            optStringType.ShowPreview(reader);
        }

        private void skipToCurrentEntry(BinaryReader reader) {
            reader.BaseStream.Position = offset;
            for (int i = 0; i < displayIndex; i++) {
                types.ForEach(delegate(FieldInfo d) {
                    Util.decodeSafe(d, reader);
                });
            }
        }
        private void color(int from, int length, Color c) {
            if (length != 0) {
                hexView.SelectionStart = 3 * from;
                hexView.SelectionLength = (3 * length) - 1;
                hexView.SelectionColor = c;
            }
        }
        
        #region Menu Handler
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog ();
			if (dlg.ShowDialog () == DialogResult.OK) {
				types.Clear ();
				offset = 0;
				Bytes = File.ReadAllBytes (dlg.FileName);
				TypeName = Path.GetFileName(Path.GetDirectoryName (dlg.FileName));
                showInPreview();
			}
		}

        private void loadToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                DBTypeMap.Instance.initializeFromFile(dlg.FileName);
                showInPreview();
            }
        }
        #endregion

        void showInPreview() {
            if (bytes == null) {
                return;
            }
            using (MemoryStream stream = new MemoryStream(bytes)) {
                DBFileHeader header = PackedFileDbCodec.readHeader(stream);
                if (DBTypeMap.Instance.IsSupported(TypeName)) {
                    TypeInfo info = DBTypeMap.Instance.GetVersionedInfo(header.GUID, TypeName, header.Version);
                    types = info.Fields;
                }
                Offset = header.Length;
            }
        }

        #region Browsing
        private void goStart_Click(object sender, EventArgs e) {
            displayIndex = 0;
            setSelection();
        }

        private void back_Click(object sender, EventArgs e) {
            if (displayIndex > 0) {
                displayIndex -= 1;
                setSelection();
            }
        }

        private void forward_Click(object sender, EventArgs e) {
            displayIndex++;
            setSelection();
        }

        private void goProblem_Click(object sender, EventArgs e) {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(Bytes))) {
                reader.BaseStream.Position = offset;
                reader.BaseStream.Position = offset - 4;
                int assumedEntryCount = reader.ReadInt32();
                int i = displayIndex;
                try {
                    for (i = 0; i < assumedEntryCount; i++) {
                        types.ForEach(delegate(FieldInfo d) {
                            d.Decode(reader);
                        });
                    }
                } catch (Exception) {
                    displayIndex = i;
                }
            }
            setSelection();
        }
        #endregion

        private void setHeaderLength_Click(object sender, EventArgs e) {
            int newValue = Offset;
            if (int.TryParse(headerLength.Text, out newValue)) {
                Offset = newValue;
            }
        }

        private void showTypes_Click(object sender, EventArgs e) {
            string text = XmlExporter.TableToString(GuidInfo, types);
			TextDisplay d = new TextDisplay (text);
			d.ShowDialog ();
		}

        #region Extended Type Management
        private void more1ToolStripMenuItem_Click(object sender, EventArgs e) {
            if (typeList.SelectedIndex != -1) {
                int selected = typeList.SelectedIndex;
                if (types[typeList.SelectedIndex].TypeName == "boolean") {
                    types[typeList.SelectedIndex] = Types.FromTypeName("optstring") ;
                    setSelection();
                } else if (types[typeList.SelectedIndex].TypeName == "optstring") {
                    types[typeList.SelectedIndex] = Types.FromTypeName("boolean");
                    setSelection();
                }
                typeList.SelectedIndex = selected;
            }
        }

        private void more2ToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void more3ToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void more4ToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void more5ToolStripMenuItem_Click(object sender, EventArgs e) {

        }
        #endregion

        private void valueList_SelectedIndexChanged(object sender, EventArgs e) {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes))) {
                try {
                    skipToCurrentEntry(reader);
                    int index = (sender as ListBox).SelectedIndex;
                    for (int i = 0; i < index; i++) {
                        types[i].Decode(reader);
                    }
                    long pos = reader.BaseStream.Position;
                    showPreview(reader, pos);
                    color((int) pos, 1, Color.DarkRed);
                } catch {}
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog dlg = new SaveFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                using (FileStream stream = File.OpenWrite(dlg.FileName)) {
                    XmlExporter exporter = new XmlExporter(stream);
                    exporter.Export(DBTypeMap.Instance.TypeMap, DBTypeMap.Instance.GuidMap);
                }
            }
        }

        private void setButton_Click(object sender, EventArgs e) {
            DBTypeMap.Instance.SetByName(TypeName, types);
        }
    }
}
