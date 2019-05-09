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

	[Inject]
	public enum Direction { North, South, East, West }

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

		[Inject]
		string FullName => Name;
	}

	interface CharacterResolver {}

	[With]
	public struct GameState 
	{
		long Tick;

		[Mutable]
		List<CharacterResolver> ActiveResolvers;

        Dictionary<int,int> Map;
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

	public enum Direction { North, South, East, West }

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

		string FullName => Name;
	}

	public partial struct GameState
	{
		public long Tick { get; }
		List<CharacterResolver> ActiveResolvers;
		public ImmutableDictionary<int, int> Map { get; }

		public GameState (long tick, IDictionary<int, int> map)
		{
			Tick = tick;
			Map = map.ToImmutableDictionary ();
		}

		public GameState WithTick (long tick)
		{
			return new GameState (tick, Map) { ActiveResolvers = this.ActiveResolvers };
		}

		public GameState WithMap (IDictionary<int, int> map)
		{
			return new GameState (Tick, map) { ActiveResolvers = this.ActiveResolvers };
		}
	}
}
", output, ignoreLineEndingDifferences: true);
		}
	}
}
