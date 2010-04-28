using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Eraser.Util
{
	/// <summary>
	/// Binds old/new versions of the same type so they can be interoperable; this
	/// binds types only by their name, not by version nor public key.
	/// </summary>
	public class TypeNameSerializationBinder : SerializationBinder
	{
		public TypeNameSerializationBinder(Type type)
		{
			Type = type;
		}

		public override Type BindToType(string assemblyName, string typeName)
		{
			if (Regex.Matches(typeName, TaskListRegex).Count == 1)
			{
				return Type;
			}

			return null;
		}

		/// <summary>
		/// The regular expression to match against all version and public keys
		/// </summary>
		private string TaskListRegex
		{
			get
			{
#if false
				if (Type == null)
				{
					return "\\[Eraser.([a-zA-z_0-9.]+), Eraser.([^,]+), " + VersionRegex +
						", Culture=([^,]+), " + PublicKeyRegex + "\\]";
				}
#endif

				string baseName = Type.FullName;
				baseName = baseName.Replace("[", "\\[");
				baseName = baseName.Replace("]", "\\]");
				baseName = Regex.Replace(baseName, VersionRegex, VersionRegex);
				baseName = Regex.Replace(baseName, PublicKeyRegex, PublicKeyRegex);
				return baseName;
			}
		}

		/// <summary>
		/// The regular expression that matches version declarations in Type names.
		/// </summary>
		private static readonly string VersionRegex = "Version=([\\.0-9]+)";

		/// <summary>
		/// The regular expression that matches public key tokens in Type names.
		/// </summary>
		private static readonly string PublicKeyRegex = "PublicKeyToken=([0-9a-fA-F]+)";

		/// <summary>
		/// The current version's type we are binding to.
		/// </summary>
		private Type Type;
	}
}
