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
			string testCode = @"public interface IPoint {}
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
";
			Parser parser = new Parser (testCode);
			string output = new CodeGenerator (parser.Parse ()).Generate ();
			Assert.AreEqual (@"using System.Collections.Immutable;

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
	public ImmutableList<Point> Points { get; }
	public string Name { get; }

	public PointList (ImmutableList<Point> points, string name)
	{
		Points = points;
		Name = name;
	}

	public PointList WithPoints (ImmutableList<Point> points)
	{
		return new PointList (points, Name);
	}

	int Length => Points.Count;
}
", output);
		}
	}
}
