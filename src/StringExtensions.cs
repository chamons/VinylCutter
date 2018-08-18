using System;
using System.Linq;

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

		public static string SmartLowerCase (this string s)
		{
			if (s.All (x => Char.IsUpper (x)))
				return s.ToLower ();
			return s.CamelPrefix ();
		}
	}
}
