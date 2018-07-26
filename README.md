# VinylCutter

**A "Record Class" Code Generator for C#**

<img src="https://upload.wikimedia.org/wikipedia/commons/b/b1/Vinyl_record_LP_10inch.JPG" alt="drawing" width="450px"/>

## Overview

C# lacks a true "record class", unlike F# or Ruby, which makes simple data class declarations like:

```fsharp
type Customer = { Name: string Name: string }
```

```ruby
Struct.new("Customer", :name, :address)
```

very long...

```csharp
public struct Customer
{
	public string Name { get; private set; }
	public string Address { get; private set; }
	
	public Customer (string name, string address)
	{
		Name = name;
		Address = address;
	}
}
```

even more so when you add methods to return new instances with modified items.

There is a [proposal](https://github.com/dotnet/csharplang/blob/master/proposals/records.md) to fix this. However even if accepted, it will be a long time until it is usable everywhere.

**VinylCutter** is a simple code generator to generate the boilerplate for you. 

## Usage

Until nuget support [is added](https://github.com/chamons/VinylCutter/issues/21) users will need to build VinylCutter to try it out.

This generally entails a simple `make prepare && make dist` but see the [Development](docs/dev.md) guide for more details.

A number of common use cases are covered in the [Quickstart](docs/quickstart.md).

VinylCutter is currently packaged as a command line tool until [MSBuild support](https://github.com/chamons/VinylCutter/issues/20) is complete.

```Usage: VinylCutter.exe [OPTIONS]+ [FILES]+
Generate C# code from record definitions.

Options:
      --stdin                Read record definitions from stdin, not a file.
      --stdout               Output record generated code to stdout, not a file.
  -o, --output=VALUE         Directory to output file to. (Defaults to current
                               directory)
      --extension=VALUE      Suffix to append to each file name written to
                               output directory. (Defaults to .g.cs)
  -h, --help                 show this message and exit
```

One or more C# "definition" files are passed in to define the shape of the record class to generate. 

Each record is based upon the name and namespace of each class defined, with read only properties matching every property or field declared. 

```csharp
[With] public class Point { int X; int Y; }
```

```csharp
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
}
```

A number of [Attributes](docs/attributes.md), such as the `[With]` used above, can be added to further customize the generated code.


## Development

Interested in hacking on VinylCutter?

Check out the 
[Development](docs/dev.md) document for details on how to get started.