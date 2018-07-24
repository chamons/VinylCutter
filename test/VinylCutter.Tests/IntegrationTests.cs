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
public class Point
{
	double X;
	double Y;
}

[Without]
public class PointList
{
	[With]
	List<Point> Points;
	string Name;
}
";
			Parser parser = new Parser (testCode);
			string output = new CodeGenerator (parser.Parse ()).Generate ();
			Assert.AreEqual (@"using System.Collections.Immutable;

public partial class Point
{
	public double X { get; }
	public double Y { get; }

	public Point (double x, double y)
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
}
", output);
		}
	}
}
