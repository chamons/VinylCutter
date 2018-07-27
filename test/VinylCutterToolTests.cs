using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace VinylCutter.Tests
{
	[Collection("Uses TempDirectory")]
	public class VinylCutterToolTests
	{
		[Fact]
		public void NoInputFailsValidation ()
		{
			VinylCutterTool tool = new VinylCutterTool ();
			Assert.False (tool.ValidateOptions (new List<string> ()));
			Assert.True (tool.ValidateOptions (new List<string> () { "Foo.cs" }));
			tool.ReadFromStandardIn = true;
			Assert.True (tool.ValidateOptions (new List<string> ()));
		}
		
		[Fact]
		public void FilesOutputToDirectoryWithCorrectExtension ()
		{
			using (TempDirectory temp = new TempDirectory ())
			{
				string inputPath = Path.Combine (temp.Path, "input.cs");
				string expectedOutputPath = Path.Combine (temp.Path, "input.bar");
				File.WriteAllText (inputPath, "public class Foo {}");

				VinylCutterTool tool = new VinylCutterTool () 
				{
					OutputDirectory = temp.Path,
					FileExtension = "bar"
				};
				Assert.True (tool.ValidateOptions (new List<string> () { inputPath }));
				tool.Run ();
				Assert.True (File.Exists (expectedOutputPath));
			}
		}
	}
}
