using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Eraser.Util
{
	/// <summary>
	/// Implements a cryptographically trasnformed file stream
	/// </summary>
	public class CryptoFileStream: FileStream
	{
		#region Constructor
		public CryptoFileStream(byte[] key, byte[] iv, SafeFileHandle handle, FileAccess access)
			: base(handle, access)
		{
			ConstructSalsa(key, iv);
		}

		public CryptoFileStream(byte[] key, byte[] iv, string path, FileMode mode)
			: base(path, mode)
		{
			ConstructSalsa(key, iv);
		}

		public CryptoFileStream(byte[] key, byte[] iv, string path, FileMode mode, FileAccess access, FileShare share)
			: base(path, mode, access, share)
		{
			ConstructSalsa(key, iv);
		}
		#endregion

		public override long Position
		{
			get { return base.Position; }
			set
			{
				base.Position = value;
				Salsa.Position = (UInt64)value;
			}
		}

		public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
		{
			throw new NotImplementedException();
			return base.BeginRead(array, offset, numBytes, userCallback, stateObject);
		}

		public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
		{
			throw new NotImplementedException();
			return base.BeginWrite(array, offset, numBytes, userCallback, stateObject);
		}

		public override int ReadByte()
		{
			Position = base.Position;

			byte[] buffer = new byte[1] { (byte)base.ReadByte() };
			Salsa.Decrypt(ref buffer);
			return buffer[0];
		}

		public override int Read(byte[] array, int offset, int count)
		{
			Position = base.Position;

			int read = base.Read(array, offset, count);
			Salsa.Decrypt(ref array);
			return read;
		}

		public override void Write(byte[] array, int offset, int count)
		{
			Position = base.Position;

			base.Write(Salsa.Encrypt(array, offset, count), 0, count);
		}

		public override void WriteByte(byte value)
		{
			Position = base.Position;

			byte[] buffer = new byte[1] { value };
			Salsa.Encrypt(ref buffer);
			base.WriteByte(buffer[0]);
		}


		protected void ConstructSalsa(byte[] key, byte[] iv)
		{
			Salsa = new Salsa(key, iv);
		}

		protected Salsa Salsa { get; set; }
	}
}
