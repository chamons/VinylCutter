using System;
using System.IO;
using Xunit;

namespace VinylCutter.Tests
{
	[Collection("Uses TempDirectory")]
	public class TempDirectoryTests
	{
		[Fact]
		public void CreateExpectedDirectory ()
		{
			using (TempDirectory dir = new TempDirectory ())
				Assert.True (Directory.Exists (dir.Path));
		}
		
		[Fact]
		public void DirectoryRemovedOnDispose ()
		{
			string createdPath = null;
			using (TempDirectory dir = new TempDirectory ())
				createdPath = dir.Path;
			
			Assert.False (Directory.Exists (createdPath));
		}

		[Fact]
		public void MultipleNestedDoNotStomp ()
		{
			using (TempDirectory first = new TempDirectory ())
			{
				string firstPath = Path.Combine (first.Path, "first");
				File.WriteAllText (firstPath, "first");
				using (TempDirectory second = new TempDirectory ())
				{
					File.WriteAllText (Path.Combine (second.Path, "second"), "second");
				}
				Assert.True (File.Exists (firstPath));
			}
		}

	}
}
