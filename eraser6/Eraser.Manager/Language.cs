/* 
 * $Id$
 * Copyright 2008 The Eraser Project
 * Original Author: Kasra Nassiri <cjax@users.sourceforge.net>
 * Modified By: Joel Low <lowjoel@users.sourceforge.net>
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.IO;

namespace Eraser.Manager
{
	/// <summary>
	/// Basic language class holding the language-related subset of the CultureInfo class
	/// </summary>
	public class Language
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="info">The culture information structure to retrieve language
		/// information from.</param>
		public Language(CultureInfo info)
		{
			culture = info;
		}

		public override string ToString()
		{
			return culture.NativeName;
		}

		/// <summary>
		/// Gets the culture name in the format "&lt;languagecode2&gt;-&lt;country/regioncode2&gt;".
		/// 
		/// The culture name in the format "&lt;languagecode2&gt;-&lt;country/regioncode2&gt;", where
		/// &lt;languagecode2&gt; is a lowercase two-letter code derived from ISO 639-1 and
		/// &lt;country/regioncode2&gt; is an uppercase two-letter code derived from ISO 3166.
		/// </summary>
		public string Name
		{
			get { return culture.Name; }
		}

		/// <summary>
		/// Gets the culture name in the format "&lt;languagefull&gt; (&lt;country/regionfull&gt;)"
		/// in the language of the localized version of .NET Framework.
		/// 
		/// The culture name in the format "&lt;languagefull&gt; (&lt;country/regionfull&gt;)" in
		/// the language of the localized version of .NET Framework, where &lt;languagefull&gt;
		/// is the full name of the language and &lt;country/regionfull&gt; is the full name
		/// of the country/region.
		/// </summary>
		public string DisplayName
		{
			get { return culture.DisplayName; }
		}

		/// <summary>
		/// Gets the culture name in the format "&lt;languagefull&gt; (&lt;country/regionfull&gt;)"
		/// in English.
		/// 
		/// The culture name in the format "&lt;languagefull&gt; (&lt;country/regionfull&gt;)" in
		/// English, where &lt;languagefull&gt; is the full name of the language and
		/// &lt;country/regionfull&gt; is the full name of the country/region.
		/// </summary>
		public string EnglishName
		{
			get { return culture.EnglishName; }
		}

		/// <summary>
		/// Converts the current Language object into a .NET CultureInfo object.
		/// </summary>
		/// <param name="lang">The language object being converted.</param>
		/// <returns>A CultureInfo object, containing the data for the given language.</returns>
		public static implicit operator CultureInfo(Language lang)
		{
			return lang.culture;
		}

		public override bool Equals(object obj)
		{
			CultureInfo rhs = obj as CultureInfo;
			if (rhs != null)
				return rhs.Equals(culture);

			return base.Equals(obj);
		}

		public static bool operator ==(Language language, CultureInfo culture)
		{
			return language.Equals(culture);
		}

		public static bool operator !=(Language language, CultureInfo culture)
		{
			return !language.Equals(culture);
		}

		public override int GetHashCode()
		{
			return culture.GetHashCode();
		}

		CultureInfo culture;
	}

	/// <summary>
	/// A class managing all plugins dealing with languages.
	/// </summary>
	public static class LanguageManager
	{
		/// <summary>
		/// Retrieves all present language plugins
		/// </summary>
		/// <returns>A list, with an instance of each Language class</returns>
		public static IList<Language> Items
		{
			get
			{
				List<Language> result = new List<Language>();
				foreach (CultureInfo info in CultureInfo.GetCultures(CultureTypes.AllCultures))
				{
					if (string.IsNullOrEmpty(info.Name))
						continue;
					else if (new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
						Path.DirectorySeparatorChar + info.Name).Exists)
						result.Add(new Language(info));
				}

				//Last resort
				if (result.Count == 0)
					result.Add(new Language(CultureInfo.GetCultureInfo("EN")));
				return result;
			}
		}
	}
}
