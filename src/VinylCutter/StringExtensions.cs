using System;

namespace VinylCutter
{
	public static class StringExtensions
	{
		public static string CamelPrefix (this string s)
		{
			if (String.IsNullOrEmpty (s))
				return s;
			return Char.ToLower (s[0]) + s.Substring (1);
		}
	}
}
