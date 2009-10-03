using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Eraser.Util
{
	/// <summary>
	/// Salsa 12 stream cipher class
	/// .NET does not have a stream cipher, thus this class was
	/// implemented.
	/// Also with use of Salsa as the cryptographical layer 
	/// the underlying stream could randomly seek in any direction.
	/// </summary>
	public unsafe class Salsa
	{
		public Salsa(byte[] key, byte[] iv)
		{
			Key = key;
			IV = iv;
			Position = 0;
		}

		#region Encryption Methods
		public void Encrypt(ref byte[] data)
		{
			Crypt(ref data, 0, data.Length);
		}

		public void Encrypt(ref byte[] data, int offset)
		{
			Crypt(ref data, offset, data.Length - offset);
		}

		public void Encrypt(ref byte[] data, int offset, int count)
		{
			Crypt(ref data, offset, count);
		}

		public byte[] Encrypt(byte[] data)
		{
			return Encrypt(data, 0);
		}

		public byte[] Encrypt(byte[] data, int offset)
		{
			return Encrypt(data, offset, data.Length - offset);
		}

		public byte[] Encrypt(byte[] data, int offset, int count)
		{
			byte[] temp = new byte[count];
			Buffer.BlockCopy(data, offset, temp, 0, count);
			Encrypt(ref temp, 0, count);
			return temp;
		}
		#endregion

		#region Decryption Methods
		public void Decrypt(ref byte[] data)
		{
			Crypt(ref data, 0, data.Length);
		}

		public void Decrypt(ref byte[] data, int offset)
		{
			Crypt(ref data, offset, data.Length - offset);
		}

		public void Decrypt(ref byte[] data, int offset, int count)
		{
			Crypt(ref data, offset, count);
		}

		public byte[] Decrypt(byte[] data)
		{
			return Decrypt(data, 0);
		}

		public byte[] Decrypt(byte[] data, int offset)
		{
			return Decrypt(data, offset, data.Length - offset);
		}

		public byte[] Decrypt(byte[] data, int offset, int count)
		{
			byte[] temp = new byte[count];
			Buffer.BlockCopy(data, 0, temp, 0, count);
			Decrypt(ref temp, offset, count);
			return temp;
		}
		#endregion

		#region XML Export/Import
		private void SetAttributes(System.Xml.XmlReader reader, ref bool Base64OrHex)
		{
			if (reader.HasAttributes)
			{
				String attr = String.Empty;
				attr = reader.GetAttribute("type");
				if (attr.Length > 0)
				{
					Base64OrHex = (attr.ToLower() == "base-64");
					Base64OrHex = !(attr.ToLower() == "hex");
				}
			}
		}

		public Salsa FromXmlString(string xmlString)
		{
			byte[] buffer = new byte[1024];

			var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xmlString));

			while (!reader.EOF && reader.Name.ToLower() != "salsa")
				reader.Read();

			if (reader.Name.ToLower() != "salsa")
				throw new ArgumentException("<salsa> node was not found", "xmlString");

			while (!reader.EOF)
			{
				reader.Read();
				if (reader.Name.ToLower() == "key")
				{
					int read = 0;
					bool base64 = false;

					SetAttributes(reader, ref base64);

					if (base64)
						read = reader.ReadContentAsBase64(buffer, 0, buffer.Length);
					else
						read = reader.ReadContentAsBinHex(buffer, 0, buffer.Length);

					byte[] k = new byte[read];
					Buffer.BlockCopy(buffer, 0, k, 0, read);
					Key = k;
				}
				else if (reader.Name.ToLower() == "iv")
				{
					int read = 0;
					bool base64 = false;

					SetAttributes(reader, ref base64);

					if (base64)
						read = reader.ReadContentAsBase64(buffer, 0, buffer.Length);
					else
						read = reader.ReadContentAsBinHex(buffer, 0, buffer.Length);

					byte[] i = new byte[read];
					Buffer.BlockCopy(buffer, 0, i, 0, read);
					IV = i;
				}
			}

			return this;
		}

		public string ToXmlString(bool exportKey)
		{
			if (exportKey)
				return string.Format("<salsa><key type='base-64'>{0}</key>" +
					"<iv type='base-64'>{1}</iv></salsa>",
				Convert.ToBase64String(Key),
					Convert.ToBase64String(IV));
			else
				return string.Format("<salsa><iv type='base-64'>{0}</iv></salsa>",
					Convert.ToBase64String(IV));
		}
		#endregion

		/// <summary>
		/// size of the key, in this imlementation we only
		/// use the 256-bit key and forget the 128-bit variant
		/// </summary>
		public int KeySize { get { return 256; } }

		/// <summary>
		/// size of the iv, allways 64-bits
		/// </summary>
		public int IVSize { get { return 64; } }

		/// <summary>
		/// Get the current key
		/// Set the stream key, note this will not restart
		/// the stream and might corrupt the data if 
		/// not handled correctly
		/// </summary>
		public byte[] Key
		{
			get { return key; }
			set
			{
				key = value;
				Array.Resize(ref key, 32);

				xpage[0] = BitConverter.ToUInt32(sigma, 0);
				xpage[1] = BitConverter.ToUInt32(Key, 0);
				xpage[2] = BitConverter.ToUInt32(Key, 4);
				xpage[3] = BitConverter.ToUInt32(Key, 8);
				xpage[4] = BitConverter.ToUInt32(Key, 12);
				xpage[5] = BitConverter.ToUInt32(sigma, 4);
				xpage[10] = BitConverter.ToUInt32(sigma, 8);
				xpage[11] = BitConverter.ToUInt32(Key, 16);
				xpage[12] = BitConverter.ToUInt32(Key, 20);
				xpage[13] = BitConverter.ToUInt32(Key, 24);
				xpage[14] = BitConverter.ToUInt32(Key, 28);
				xpage[15] = BitConverter.ToUInt32(sigma, 12);
			}
		}

		/// <summary>
		/// IV of the stream
		/// </summary>
		public byte[] IV
		{
			get { return iv; }
			set
			{
				iv = value;
				Array.Resize(ref iv, 8);
				xpage[6] = BitConverter.ToUInt32(iv, 0);
				xpage[7] = BitConverter.ToUInt32(iv, 4);
			}
		}
		
		/// <summary>
		/// Export this instance into xml format
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("<salsa><key type='base-64'>{0}</key>" +
				"<iv type='base-64'>{1}</iv></salsa>",
			Convert.ToBase64String(Key),
				Convert.ToBase64String(IV));
		}

		public UInt64 Position
		{
			get { return position; }
			set
			{
				position = value;
				
				xpage[8] = (UInt32)(value >> 6) & 0xFFFFFFFFU;
				xpage[9] = (UInt32)((value >> 6) >> 32) & 0xFFFFFFFFU;
			}
		}

		private void Crypt(ref byte[] data, int offset, int count)
		{
			int length = count;

			// left align the data 
			if ((Position & 63) != 0)
			{
				int left = Math.Min(64 - (int)(Position & 63), length);

				if (left != 0)
				{
					fixed (byte* source = &data[offset])
					fixed (byte* page = &Generate(xpage)[Position & 63])
					{
						Memory.Xor(source, page, left);
					}

					length -= left;
					offset += left;
					Position += (UInt64)left;
				}
#if DEBUG
				if ((Position & 63) != 0 && length != 0)
					throw new Exception();
#endif
			}

			while (length >= 64)
			{
				fixed (byte* source = &data[offset])
				fixed (byte* page = Generate(xpage))
				{
					Memory.Xor(source, page, 64);
				}

				Position += 64;
				offset += 64;
				length -= 64;
			}

			if (length > 0)
			{
				fixed (byte* source = &data[offset])
				fixed (byte* page = Generate(xpage))
				{
					Memory.Xor(source, page, length);
				}
				Position += (UInt64)length;
			}
		}

		private UInt32[] xpage = new UInt32[16];
		private byte[] key = null, iv = null;
		private UInt64 position = 0;

		private static UInt32 Rotate(UInt32 value, int amount)
		{
			return (value << amount) | (value >> (32 - amount));
		}

		private static byte[] Generate(UInt32[] input)
		{
			int i;
			byte[] output = new byte[64];
			UInt32[] x = new UInt32[16];

			input.CopyTo(x, 0);
			for (i = 12; i > 0; i -= 2)
			{
				x[4] ^= Rotate((x[0] + x[12]), 7);
				x[8] ^= Rotate((x[4] + x[0]), 9);
				x[12] ^= Rotate((x[8] + x[4]), 13);
				x[0] ^= Rotate((x[12] + x[8]), 18);
				x[9] ^= Rotate((x[5] + x[1]), 7);
				x[13] ^= Rotate((x[9] + x[5]), 9);
				x[1] ^= Rotate((x[13] + x[9]), 13);
				x[5] ^= Rotate((x[1] + x[13]), 18);
				x[14] ^= Rotate((x[10] + x[6]), 7);
				x[2] ^= Rotate((x[14] + x[10]), 9);
				x[6] ^= Rotate((x[2] + x[14]), 13);
				x[10] ^= Rotate((x[6] + x[2]), 18);
				x[3] ^= Rotate((x[15] + x[11]), 7);
				x[7] ^= Rotate((x[3] + x[15]), 9);
				x[11] ^= Rotate((x[7] + x[3]), 13);
				x[15] ^= Rotate((x[11] + x[7]), 18);
				x[1] ^= Rotate((x[0] + x[3]), 7);
				x[2] ^= Rotate((x[1] + x[0]), 9);
				x[3] ^= Rotate((x[2] + x[1]), 13);
				x[0] ^= Rotate((x[3] + x[2]), 18);
				x[6] ^= Rotate((x[5] + x[4]), 7);
				x[7] ^= Rotate((x[6] + x[5]), 9);
				x[4] ^= Rotate((x[7] + x[6]), 13);
				x[5] ^= Rotate((x[4] + x[7]), 18);
				x[11] ^= Rotate((x[10] + x[9]), 7);
				x[8] ^= Rotate((x[11] + x[10]), 9);
				x[9] ^= Rotate((x[8] + x[11]), 13);
				x[10] ^= Rotate((x[9] + x[8]), 18);
				x[12] ^= Rotate((x[15] + x[14]), 7);
				x[13] ^= Rotate((x[12] + x[15]), 9);
				x[14] ^= Rotate((x[13] + x[12]), 13);
				x[15] ^= Rotate((x[14] + x[13]), 18);
			}

			for (i = 0; i < 16; ++i)
				BitConverter.GetBytes(x[i] + input[i])
					.CopyTo(output, i << 2);
			return output;
		}

		private static readonly byte[] sigma =
			System.Text.Encoding.ASCII.GetBytes("expand 32-byte k");
	}
}