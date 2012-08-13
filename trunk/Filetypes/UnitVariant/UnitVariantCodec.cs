using System;
using System.Collections.Generic;
using System.IO;
using Common;

namespace Filetypes {
	public class UnitVariantCodec : Codec<UnitVariantFile> {
		public static readonly UnitVariantCodec Instance = new UnitVariantCodec();
		
		public static byte[] Encode(UnitVariantFile file) {
			using (MemoryStream stream = new MemoryStream()) {
				Instance.Encode (stream, file);
				return stream.ToArray ();
			}
		}
		
		// read from file
		public UnitVariantFile Decode(Stream stream) {
			UnitVariantFile file = new UnitVariantFile ();
			using (BinaryReader reader = new BinaryReader (stream)) {
				byte[] buffer = reader.ReadBytes (4);
				if ((((buffer [0] != 0x56) || (buffer [1] != 0x52)) || (buffer [2] != 0x4e)) || (buffer [3] != 0x54)) {
					throw new FileLoadException ("Illegal unit_variant file: Does not start with 'VRNT'");
				}
				file.Version = reader.ReadUInt32 ();
				int entries = (int)reader.ReadUInt32 ();
				file.Unknown1 = reader.ReadUInt32 ();
				byte[] buffer3 = reader.ReadBytes (4);
				file.B1 = buffer3 [0];
				file.B2 = buffer3 [1];
				file.B3 = buffer3 [2];
				file.B4 = buffer3 [3];
				file.Unknown2 = BitConverter.ToUInt32 (buffer3, 0);
				if (file.Version == 2) {
					file.Unknown3 = reader.ReadInt32 ();
				}
				file.UnitVariantObjects = new List<UnitVariantObject> (entries);
				for (int i = 0; i < entries; i++) {
					UnitVariantObject item = readObject (reader);
					file.UnitVariantObjects.Add (item);
				}
				for (int j = 0; j < file.UnitVariantObjects.Count; j++) {
					for (int k = 0; k < file.UnitVariantObjects[j].StoredEntryCount; k++) {
						MeshTextureObject mto = ReadMTO (reader);
						file.UnitVariantObjects [j].MeshTextureList.Add (mto);
					}
				}
			}
			return file;
		}

		private static UnitVariantObject readObject(BinaryReader reader) {
			UnitVariantObject item = new UnitVariantObject {
                ModelPart = IOFunctions.readStringContainer (reader),
                Index = reader.ReadUInt32 (),
                Num2 = reader.ReadUInt32 (),
				StoredEntryCount = reader.ReadUInt32 (),
				MeshStartIndex = reader.ReadUInt32 ()
			};
			return item;
		}
		
		private static MeshTextureObject ReadMTO(BinaryReader reader) {
			MeshTextureObject obj3 = new MeshTextureObject {
			                  Mesh = IOFunctions.readStringContainer (reader),
			                  Texture = IOFunctions.readStringContainer (reader),
			                  Bool1 = reader.ReadBoolean (),
			                  Bool2 = reader.ReadBoolean ()
			              };
			return obj3;
		}
		
		// write to stream
		public void Encode(Stream stream, UnitVariantFile file) {
			using (BinaryWriter writer = new BinaryWriter(stream)) {
				writer.Write ("VRNT".ToCharArray (0, 4));
				writer.Write (file.Version);
				writer.Write ((uint)file.UnitVariantObjects.Count);
				writer.Write (file.Unknown1);
				writer.Write (file.Unknown2);
				if (file.Version == 2) {
					writer.Write (file.Unknown3);
				}
				int mtoStartIndex = 0;
				foreach (UnitVariantObject uvo in file.UnitVariantObjects) {
					IOFunctions.writeStringContainer (writer, uvo.ModelPart);
					if (uvo.Index == 0) {
						mtoStartIndex = 0;
					}
					writer.Write (uvo.Index);
					writer.Write (uvo.Num2);  // always 0 afaict
					writer.Write ((uint)uvo.EntryCount);
					writer.Write (mtoStartIndex);
					mtoStartIndex += (int) uvo.EntryCount;
				}
				for (int j = 0; j < file.NumEntries; j++) {
					if (file.UnitVariantObjects [j].EntryCount != 0) {
						for (int k = 0; k < file.UnitVariantObjects[j].EntryCount; k++) {
							IOFunctions.writeStringContainer (writer, file.UnitVariantObjects [j].MeshTextureList [k].Mesh);
							IOFunctions.writeStringContainer (writer, file.UnitVariantObjects [j].MeshTextureList [k].Texture);
							writer.Write (file.UnitVariantObjects [j].MeshTextureList [k].Bool1);
							writer.Write (file.UnitVariantObjects [j].MeshTextureList [k].Bool2);
						}
					}
				}
				writer.Flush ();
			}
		}
	}
}

