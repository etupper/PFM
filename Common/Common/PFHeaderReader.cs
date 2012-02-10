using System;
using System.IO;
using System.Text;

namespace Common
{
	public abstract class PFHeaderReader
	{
		protected PFHeader header;
		public PFHeader Header {
			get {
				return header;
			}
		}

		public PFHeaderReader (string pfhHeaderVersion)
		{
			header = new PFHeader { PackIdentifier = pfhHeaderVersion };
		}
		public virtual void readFromStream(BinaryReader reader) {
			// skip the header ID (just to be on the safe side)
			reader.BaseStream.Seek (0x04L, SeekOrigin.Begin);
			readPackType (reader);
			header.Version = reader.ReadInt32 ();
			int replacedPackFilenameLength = reader.ReadInt32 ();
			reader.BaseStream.Seek (0x10L, SeekOrigin.Begin);
			header.FileCount = reader.ReadUInt32 ();
			UInt32 indexSize = reader.ReadUInt32 ();
			header.DataStart = headerLength () + indexSize;

			reader.BaseStream.Seek (headerLength (), SeekOrigin.Begin);
			if (header.Version == 1) {
				// read pack file reference
				header.ReplacedPackFileName = 
					new string (ASCIIEncoding.ASCII.GetChars (reader.ReadBytes (replacedPackFilenameLength - 1)));
				// skip the null byte
				reader.ReadByte ();
				header.DataStart += replacedPackFilenameLength;
			}
		}
		protected abstract long headerLength();
		
		protected void readPackType(BinaryReader reader) {
            int packType = reader.ReadInt32();
            if (packType > 4)
            {
                throw new InvalidDataException("unknown pack type");
            }
			header.Type = (PackType) packType;
		}
	}
	
	public class PFH0HeaderReader : PFHeaderReader {
		public PFH0HeaderReader() : base ("PFH0") {}
		protected override long headerLength() { return 0x18; }
	}
	
	public class PFH2HeaderReader : PFHeaderReader {
		// PFH2/3 contains a FileTime at 0x1C (I think) in addition to PFH0's header
		public PFH2HeaderReader(string headerVersion) : base(headerVersion) {}
		protected override long headerLength() { return 0x20; }
	}
}

