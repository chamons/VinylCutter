using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class IntegrationTests
	{
		[Test]
		public void SmokeTest ()
		{
			string testCode = @"
namespace Integration
{
	interface IPoint {}

	[Inject]
	public enum Visibility { Public, Private }

	[With]
	public class Point : IPoint
	{
		double X;
		[Default (""42"")]
		double Y;
	}

	public class PointList
	{
		[With]
		List<Point> Points;
		string Name;

		[Inject]
		int Length => Points.Count;
	}
}
";
			Parser parser = new Parser (testCode);
			string output = new CodeGenerator (parser.Parse ()).Generate ();
			Assert.AreEqual (@"using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Integration
{
	public enum Visibility { Public, Private }

	public partial class Point : IPoint
	{
		public double X { get; }
		public double Y { get; }

		public Point (double x, double y = 42)
		{
			X = x;
			Y = y;
		}

		public Point WithX (double x)
		{
			return new Point (x, Y);
		}

		public Point WithY (double y)
		{
			return new Point (X, y);
		}
	}

	public partial class PointList
	{
		public ImmutableArray<Point> Points { get; }
		public string Name { get; }

		public PointList (IEnumerable<Point> points, string name)
		{
			Points = ImmutableArray.CreateRange (points ?? Array.Empty<Point> ());
			Name = name;
		}

		public PointList WithPoints (IEnumerable<Point> points)
		{
			return new PointList (points, Name);
		}

		int Length => Points.Count;
	}
}
", output);
		}
	}
}
