using System;
using System.Text;
using Xunit;

namespace VinylCutter.Tests
{
	public class CodeWriterTests
	{
		[Fact]
		public void IndentTabsOutputOver ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.WriteLine ("asdf");
			writer.Indent ();
			writer.WriteLine ("fdsa");
			Assert.Equal ("asdf\n\tfdsa\n", writer.Generate ());
		}

		[Fact]
		public void WriteLineAddsNewLineWriteDoesNot ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.WriteLine ("asdf");
			writer.Write ("fdsa");
			Assert.Equal ("asdf\nfdsa", writer.Generate ());
		}
		
		[Fact]
		public void OverDeindentingThrows ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.Indent ();
			writer.Dedent ();
			Assert.Throws<InvalidOperationException> (() => writer.Dedent ());
		}
		
		[Fact]
		public void EmptyLineDoesNotInheritIndent ()
		{
			CodeWriter writer = new CodeWriter ();
			writer.Indent ();
			writer.WriteLine ();
			Assert.Equal ("\n", writer.Generate ());
		}

	}
}
