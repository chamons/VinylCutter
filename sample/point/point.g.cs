using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Model
{
	public partial class Point
	{
		public int X { get; }
		public int Y { get; }

		public Point (int x, int y)
		{
			X = x;
			Y = y;
		}

		public Point WithX (int x)
		{
			return new Point (x, Y);
		}

		public Point WithY (int y)
		{
			return new Point (X, y);
		}

        public double Size => X * Y;
	}

	public partial class NamedPointList
	{
		public ImmutableArray<Point> Points { get; }
		public string Name { get; }

		public NamedPointList (IEnumerable<Point> points, string name = "Default")
		{
			Points = ImmutableArray.CreateRange (points ?? Array.Empty<Point> ());
			Name = name;
		}
	}
}
