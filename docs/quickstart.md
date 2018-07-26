## Quickstart

Until nuget support is complete, the first step is obtaining and building VinyCutter:

- `git clone git@github.com:chamons/VinylCutter.git && cd VinylCutter`
- `make prepare && make dist`

Next let's start with a point and named list of points:

```csharp
namespace Model
{
	public class Point 
	{
		int X; 
		int Y; 
	}
	
	public class NamedPointList
	{
		List<Point> Points;
		string Name;
	}
}
```

And see what that generated to:

`$ ./dist/VinylCutter --stdout point.rcs`

```csharp
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
	}

	public partial class NamedPointList
	{
		public ImmutableArray<Point> Points { get; }
		public string Name { get; }

		public NamedPointList (IEnumerable<Point> points, string name)
		{
			Points = ImmutableArray.CreateRange (points ?? Array.Empty<Point> ());
			Name = name;
		}
	}
}
```

Looking good, but let's customize the output a bit.

Start by adding a calculated property to the generated code:

```csharp
	[Inject]
	public double Size => X * Y;
```

and make the name default to some reasonable value:

```csharp
	[Default ("\"Default\"")]
	string Name;
```

and add some convience methods to generate a new point if one value changes:

```csharp
[With]
public class Point 
```

Now we'll need to:

- Regenerate the output: `./dist/VinylCutter point.rcs`
- Create a small test program:

```csharp
using System;
using Model;

namespace Test
{
	public static class EntryPoint
	{
		public static void Main ()
		{
			var list = new NamedPointList (new Point [] { new Point (1, 1), new Point (2, 2) }, "MyList");
			Console.WriteLine ($"List {list.Name} has points of size ({string.Join (", ", list.Points.Select (p => p.Size))})!");
		}
	}
}
```
- Compile them together in a new csproj, along with the System.Collections.Immutable nuget.


This sample can be found in the [sample folder](https://github.com/chamons/VinylCutter/tree/master/sample/point).
