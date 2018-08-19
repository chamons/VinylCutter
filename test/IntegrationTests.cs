using System;
using System.Collections.Generic;
using Xunit;

namespace VinylCutter.Tests
{
	public class IntegrationTests
	{
		[Fact]
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

	interface CharacterResolver {}

	[With]
	public struct GameState 
	{
		long Tick;

		[Mutable]
		List<CharacterResolver> ActiveResolvers;
	}
}
";
			Parser parser = new Parser (testCode);
			string output = new CodeGenerator (parser.Parse ()).Generate ();
			Assert.Equal (@"using System;
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

	public partial struct GameState
	{
		public long Tick { get; }
		List <CharacterResolver> ActiveResolvers;

		public GameState (long tick)
		{
			Tick = tick;
		}

		public GameState WithTick (long tick)
		{
			return new GameState (tick) { ActiveResolvers = this.ActiveResolvers };
		}
	}
}
", output);
		}
	}
}
