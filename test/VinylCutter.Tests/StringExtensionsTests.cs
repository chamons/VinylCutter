using System;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class StringExtensionsTests
	{
		[Test]
		public void PrefixEmptyString ()
		{
			Assert.AreEqual ("", "".CamelPrefix ());
		}

		[Test]
		public void PrefixSingleCharacter ()
		{
			Assert.AreEqual ("f", "F".CamelPrefix ());
			Assert.AreEqual ("f", "f".CamelPrefix ());
		}
		
		[Test]
		public void PrefixWord ()
		{
			Assert.AreEqual ("foo", "Foo".CamelPrefix ());
			Assert.AreEqual ("foo", "foo".CamelPrefix ());
		}
	}
}
