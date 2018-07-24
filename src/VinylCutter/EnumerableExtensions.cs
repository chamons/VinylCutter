using System.Collections.Generic;
using System.Collections.Immutable;

namespace VinylCutter
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Yield<T> (this T item)
		{
			yield return item;
		}

		public static ImmutableList<T> YieldList<T> (this T item)
		{
			return item.Yield ().ToImmutableList ();
		}
	}
}
