using System;
using System.Text;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class CodeWriterTests
	{
		[Test]
		public void IndentTabsOutputOver ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.WriteLine ("asdf");
			writer.Indent ();
			writer.WriteLine ("fdsa");
			Assert.AreEqual ("asdf\n\tfdsa\n", writer.Generate ());
		}

		[Test]
		public void WriteLineAddsNewLineWriteDoesNot ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.WriteLine ("asdf");
			writer.Write ("fdsa");
			Assert.AreEqual ("asdf\nfdsa", writer.Generate ());
		}
		
		[Test]
		public void OverDeindentingThrows ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.Indent ();
			writer.Dedent ();
			Assert.Throws<InvalidOperationException> (() => writer.Dedent ());
		}
	}
}
