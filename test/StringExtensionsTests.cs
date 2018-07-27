using System;
using Xunit;

namespace VinylCutter.Tests
{
	public class StringExtensionsTests
	{
		[Fact]
		public void PrefixEmptyString ()
		{
			Assert.Equal ("", "".CamelPrefix ());
		}

		[Fact]
		public void PrefixSingleCharacter ()
		{
			Assert.Equal ("f", "F".CamelPrefix ());
			Assert.Equal ("f", "f".CamelPrefix ());
		}
		
		[Fact]
		public void PrefixWord ()
		{
			Assert.Equal ("foo", "Foo".CamelPrefix ());
			Assert.Equal ("foo", "foo".CamelPrefix ());
		}
	}
}
