namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class LocFile
    {
		public string Name { get; set; }

		public List<LocEntry> Entries {
			get;
			private set;
		}
        public int NumEntries {
			get {
				return Entries.Count;
			}
		}
		
		public LocFile () {
			Entries = new List<LocEntry> ();
		}

		#region CSV export
        public void Export(StreamWriter writer) {
			for (int i = 0; i < NumEntries; i++) {
				writer.WriteLine (this.Entries [i].Tag.Replace ("\t", @"\t").Replace ("\n", @"\n") + 
				                 "\t" + this.Entries [i].Localised.Replace ("\t", @"\t").Replace ("\n", @"\n") + 
				                  "\t" + (this.Entries [i].Tooltip ? "True" : "False"));
			}
		}

        public void Import(StreamReader reader) {			
			Entries.Clear ();
			while (!reader.EndOfStream) {
				string str = reader.ReadLine ();
				if (str.Trim () != "") {
					string[] strArray = str.Split (new char[] { '\t' });
					string tag = strArray [0].Replace (@"\t", "\t").Replace (@"\n", "\n").Trim (new char[] { '"' });
					string localised = strArray [1].Replace (@"\t", "\t").Replace (@"\n", "\n").Trim (new char[] { '"' });
					bool tooltip = strArray [2].ToLower () == "true";
					Entries.Add (new LocEntry (tag, localised, tooltip));
				}
			}
		}
		#endregion
    }

    public class LocEntry {
		public string Localised { get; set; }
		public string Tag { get; set; }
		public bool Tooltip { get; set; }

		public LocEntry (string tag, string localised, bool tooltip) {
			this.Tag = tag;
			this.Localised = localised;
			this.Tooltip = tooltip;
		}
	}
}

