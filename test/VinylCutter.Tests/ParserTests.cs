using System;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class ParserTests
	{
		[Test]
		public void ReturnsReflectedInfoForSimpleCase ()
		{
			Parser parser = new Parser (@"public class SimpleClass { }");
			var info = parser.Parse ();
			Assert.AreEqual (1, info.Count);
			Assert.AreEqual ("SimpleClass", info[0].Name);
		}

		[Test]
		public void ThrowOnInvalidCompiledInput ()
		{
			Parser parser = new Parser (@"public class CompilerError {");
			Assert.Throws<ParseCompileError> (() => parser.Parse ());
		}
	}
}
