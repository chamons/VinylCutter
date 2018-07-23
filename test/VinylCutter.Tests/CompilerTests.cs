using System;
using System.IO;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class CompilerTests
	{
		[Test]
		public void CreatesAssemblyForDurationOfCompiler ()
		{
			string assemblyPath = null;
			using (Compiler compiler = new Compiler ("public class Foo {}"))
			{
				assemblyPath = compiler.Compile ();
				Assert.IsTrue (File.Exists (assemblyPath));
			}
			Assert.IsFalse (File.Exists (assemblyPath));
		}

		[Test]
		public void ThrowsWhenCompileCommandReturnsNonZero ()
		{
			Compiler compiler = new Compiler ("");
			compiler.RunCommand = (path, args, env, output, suppressPrintOnErrors) => 42;
			Assert.Throws<ParseCompileError> (() => compiler.Compile ());
		}
	}
}
