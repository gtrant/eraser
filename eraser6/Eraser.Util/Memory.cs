using System;
using System.Text;

namespace Eraser.Util
{
	public static unsafe class Memory
	{
		private static char ToHexChar(byte b)
		{
			if (b < 10)
				return (char)(b + char.GetNumericValue('0'));
			else
				return (char)((b - 10) + char.GetNumericValue('a'));
		}

		public static string ToHex(void* data, int size)
		{
			StringBuilder s = new StringBuilder(size * 2);
			for (byte* pointer = (byte*)data; size-- != 0; pointer++)
			{
				s.Append(ToHexChar((byte)(*pointer & 0xF)));
				s.Append(ToHexChar((byte)((*pointer >> 4) & 0xF)));
			}
			return s.ToString();
		}

		public static void Xor(void* dest, void* src, int size)
		{
			int* D = (int*)dest;
			int* S = (int*)src;
			int wsize = size / sizeof(uint);
			size -= wsize * sizeof(uint);

			while (wsize-- > 0) *D++ ^= *S++;

			if (size > 0)
			{
				byte* BD = (byte*)D;
				byte* BS = (byte*)S;
				while (size-- > 0) *BD++ ^= *BS++;
			}
		}

		public static void Xor(byte* dest, byte* src, int count)
		{
			Xor((void*)dest, (void*)src, count);
		}

		public static void Xor(int* dest, int* src, int count)
		{
			Xor((byte*)dest, (byte*)src, count * sizeof(int));
		}

		public static void Xor(IntPtr dest, IntPtr src, int count)
		{
			Xor(dest.ToPointer(), src.ToPointer(), count);
		}

		public static void Set(void* dest, byte value, int size)
		{
			uint* D = (uint*)dest;
			int wsize = size / sizeof(uint);
			size -= wsize * sizeof(uint);

			if (wsize > 0)
			{
				uint V = 0;
				if (value != 0)
					for (int i = 0; i < sizeof(uint); i++, V <<= sizeof(byte) * 8)
						V |= value;

				while (wsize-- > 0) *D++ = V;
			}

			if (size > 0)
			{
				byte* BD = (byte*)D;
				while (size-- > 0) *BD++ = (byte)value;
			}
		}

		public static void Set(byte* dest, byte value, int count)
		{
			Set((void*)dest, value, count * sizeof(byte));
		}

		public static void Set(int* dest, int value, int count)
		{
			int* D = (int*)dest;
			while (count-- > 0) *D++ = (int)value;
		}

		public static void Set(IntPtr dest, byte value, int count)
		{
			Set(dest.ToPointer(), value, count);
		}

		public static void Zero(void* dest, byte value, int size)
		{
			Set(dest, 0, size);
		}

		public static void Zero(byte* dest, int count)
		{
			Set(dest, 0, count);
		}

		public static void Zero(int* dest, int count)
		{
			Set(dest, 0, count * sizeof(int));
		}

		public static void Zero(IntPtr dest, int count)
		{
			Set(dest.ToPointer(), 0, count);
		}
	}
}
