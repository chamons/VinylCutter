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
	}
}
