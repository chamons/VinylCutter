# VinylCutter
 A C# "Record Class" Code Generator 

![Vinyl Record](https://upload.wikimedia.org/wikipedia/commons/b/b1/Vinyl_record_LP_10inch.JPG)

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

There is a [proposal](https://github.com/dotnet/csharplang/blob/master/proposals/records) to fix this, but even if accepted it will be a long time until it is usable everywhere.

**VinylCutter** is a single code generator to generate the boilerplate for you. 

## Usage
