using System;
using System.IO;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class TempDirectoryTests
	{
		[Test]
		public void CreateExpectedDirectory ()
		{
			using (TempDirectory dir = new TempDirectory ())
				Assert.IsTrue (Directory.Exists (dir.Path));
		}
		
		[Test]
		public void DirectoryRemovedOnDispose ()
		{
			string createdPath = null;
			using (TempDirectory dir = new TempDirectory ())
				createdPath = dir.Path;
			
			Assert.IsFalse (Directory.Exists (createdPath));
		}

		[Test]
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
				Assert.IsTrue (File.Exists (firstPath));
			}
		}

	}
}
