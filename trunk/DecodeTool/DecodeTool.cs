using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Common;

namespace DecodeTool {
    public partial class DecodeTool : Form {
        Regex typeRe = new Regex("DBFileTypes_([0-9]*).txt");

        byte[] bytes;
        List<String> fieldNames = new List<string>();
        List<TypeDescription> types = new List<TypeDescription>();
        int offset = 0;
        int displayIndex = 0;
        string typeName = "";

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
			}
			get {
				return bytes;
			}
		}
        string TypeName {
            get { return typeName; }
            set { typeName = value; typeNameLabel.Text = string.Format("Type: {0}", typeName); }
        }
        int Offset {
            get { return offset; }
            set { offset = value; setSelection(); }
        }
        int KnownByteCount {
            get {
                int index = offset;
                if (Bytes != null) {
                    using (BinaryReader reader = new BinaryReader(new MemoryStream(Bytes))) {
                        reader.BaseStream.Position = offset;
                        types.ForEach(delegate(TypeDescription d) {
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

            stringType.Type = Types.StringType;
            stringType.Selected += addType;

            intType.Type = Types.IntType;
            intType.Selected += addType;

            boolType.Type = Types.BoolType;
            boolType.Selected += addType;

            singleType.Type = Types.SingleType;
            singleType.Selected += addType;

            optStringType.Type = Types.OptStringType;
            optStringType.Selected += addType;
        }

        #region Type Management
        private void addType(TypeDescription type) {
            if (typeList.SelectedIndex != -1) {
                types.Insert(typeList.SelectedIndex, type);
                fieldNames.Insert(typeList.SelectedIndex, "unknown");
            } else {
                types.Add(type);
                fieldNames.Add("unknown");
            }
            setSelection();
        }
        private void delete_Click(object sender, EventArgs e) {
            int selectIndex = -1;
            if (typeList.SelectedIndex == -1) {
                if (types.Count > 0) {
                    fieldNames.RemoveAt(types.Count - 1);
                    types.RemoveAt(types.Count - 1);
                }
            } else {
                selectIndex = typeList.SelectedIndex;
                fieldNames.RemoveAt(typeList.SelectedIndex);
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
				types.ForEach (delegate(TypeDescription d) {
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
								types.ForEach (delegate(TypeDescription d) { 
									s = d.Decode (reader); 
									tempBytes += (uint)d.GetLength (s); 
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
                types.ForEach(delegate(TypeDescription d) {
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
				fieldNames.Clear ();
				offset = 0;
				Bytes = File.ReadAllBytes (dlg.FileName);
				string dir = Path.GetDirectoryName (dlg.FileName);
				TypeName = Path.GetFileName (dir).Replace ("_tables", "").Trim ();
				string parent = Path.GetDirectoryName (dir);
				int maxVersion = -1;
				foreach (string typeFile in Directory.EnumerateFiles(parent, "DBFileType*")) {
					Match m = typeRe.Match (typeFile);
					int version = int.Parse (m.Groups [1].Value);
					if (version > maxVersion) {
						SortedDictionary<string, TypeInfo> infos = DBTypeMap.getTypeMapFromFile (typeFile);
						if (infos.ContainsKey (TypeName)) {
							TypeInfo info = infos [TypeName];
							fieldNames.Clear ();
							types = Util.Convert (info, ref fieldNames);
						}
					}
				}
				setSelection ();
			}
		}

        private void loadToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                SortedDictionary<string, TypeInfo> infos = DBTypeMap.getTypeMapFromFile(dlg.FileName);
                if (infos.ContainsKey(TypeName)) {
                    TypeInfo info = infos[TypeName];
                    fieldNames.Clear();
                    types = Util.Convert(info, ref fieldNames);
                    setSelection();
                }
            }
        }
        #endregion

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
                        types.ForEach(delegate(TypeDescription d) {
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
            } else {
                headerLength.Text = offset.ToString();
            }
        }

        private void showTypes_Click(object sender, EventArgs e) {
			StringBuilder builder = new StringBuilder ();
			builder.AppendLine ().AppendLine ();
			builder.AppendLine (string.Format ("{0}\t{1};", TypeName, Util.ToString (fieldNames [0], types [0])));
			for (int i = 1; i < types.Count; i++) {
				string toAppend = Util.ToString (fieldNames [i], types [i]) + (i == types.Count - 1 ? "" : ";");
				builder.AppendLine (toAppend);
			}
			string text = builder.ToString ();
			File.WriteAllText ("temp.txt", text);
			Console.WriteLine (text);
			TextDisplay d = new TextDisplay (text);
			d.ShowDialog ();
		}

        #region Extended Type Management
        private void more1ToolStripMenuItem_Click(object sender, EventArgs e) {
            if (typeList.SelectedIndex != -1) {
                int selected = typeList.SelectedIndex;
                if (types[typeList.SelectedIndex] == Types.BoolType) {
                    types[typeList.SelectedIndex] = Types.OptStringType;
                    setSelection();
                } else if (types[typeList.SelectedIndex] == Types.OptStringType) {
                    types[typeList.SelectedIndex] = Types.BoolType;
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
                skipToCurrentEntry(reader);
                int start = (int) reader.BaseStream.Position;
                int index = (sender as ListBox).SelectedIndex;
                for (int i = 0; i < index; i++) {
                    string temp = types[i].Decode(reader);
                }
                long pos = reader.BaseStream.Position;
                showPreview(reader, pos);
                color((int) pos, 1, Color.DarkRed);
            }
        }
    }
}
