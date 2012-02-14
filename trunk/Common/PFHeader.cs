using System;
using System.Collections.Generic;

namespace Common
{
    public class PFHeader {
        private string replacedPackFile = "";
		
		public PFHeader (string id) {
			PackIdentifier = id;
		}
		
		string identifier;
        public string PackIdentifier { 
			get {
				return identifier;
			}
			set {
				switch (value) {
				case "PFH0":
				case "PFH2":
				case "PFH3":
					break;
				default:
					throw new Exception ("Unknown Header Type " + value);
				}
				identifier = value;
			}
		}
        public PackType Type { get; set; }
        public int Version { get; set; }
        public long DataStart { get; set; }
        public UInt32 FileCount { get; set; }

        public string ReplacedPackFileName {
            get { return replacedPackFile; }
            set { replacedPackFile = value; }
        }

        public int Length {
			get {
				int result;
				switch (PackIdentifier) {
				case "PFH0":
					result = 0x18;
					break;
				case "PFH2":
				case "PFH3":
					// PFH2/3 contains a FileTime at 0x1C (I think) in addition to PFH0's header
					result = 0x20;
					break;
				default:
					// if this ever happens, go have a word with MS
					throw new Exception ("Unknown header ID " + PackIdentifier);
				}
				return result;
			}
		}
    }
}

