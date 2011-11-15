using System;
using System.Collections.Generic;

namespace Common
{
	public class PFHeader
	{
		private string replacedPackFile = "";
		
		public string PackIdentifier { get; set; }
		public PackType Type { get; set; }
		public int Version { get; set; }
		public long DataStart { get; set; }
		public UInt32 FileCount { get; set; }
		
		public string ReplacedPackFileName { 
			get { return replacedPackFile; }
			set { replacedPackFile = value; }
		}
	}
}

