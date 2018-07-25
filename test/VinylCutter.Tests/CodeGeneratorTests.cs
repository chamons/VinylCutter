using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class CodeGeneratorTests
	{
		[Test]
		[TestCase ("SimpleClass", Visibility.Public, true, "public partial class SimpleClass\n{\n}\n")]
		[TestCase ("SimpleClass", Visibility.Private, true, "partial class SimpleClass\n{\n}\n")]
		[TestCase ("SimpleStruct", Visibility.Public, false, "public partial struct SimpleStruct\n{\n}\n")]
		public void GenerateSimpleClass (string name, Visibility visibility, bool isClass, string expected)
		{
			ParseInfo parseInfo = new ParseInfo (name, isClass, visibility); 
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (expected, generator.Generate ());
		}
		
		[Test]
		public void SpacesBetweenRecords ()
		{
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public); 
			ParseInfo parseInfo2 = new ParseInfo ("SimpleClass2", true, Visibility.Public); 
			CodeGenerator generator = new CodeGenerator (new ParseInfo [] { parseInfo, parseInfo2 });
			Assert.AreEqual (@"public partial class SimpleClass
{
}

public partial class SimpleClass2
{
}
", generator.Generate ());
		}

		[Test]
		public void GenerateBasicProperties ()
		{
			ClassItem item = new ClassItem ("Foo", "Int32");  
			ClassItem item2 = new ClassItem ("Bar", "Int32");  
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public, false, new ClassItem [] { item, item2 }); 
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (@"public partial class SimpleClass
{
	public int Foo { get; }
	public int Bar { get; }

	public SimpleClass (int foo, int bar)
	{
		Foo = foo;
		Bar = bar;
	}
}
", generator.Generate ());
		}

		[Test]
		public void Enumerables ()
		{
			ClassItem item = new ClassItem ("Foo", "Int32", true, false);  
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public, true, item.Yield ()); 
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (@"using System.Collections.Immutable;

public partial class SimpleClass
{
	public ImmutableList<int> Foo { get; }

	public SimpleClass (ImmutableList<int> foo)
	{
		Foo = foo;
	}

	public SimpleClass WithFoo (ImmutableList<int> foo)
	{
		return new SimpleClass (foo);
	}
}
", generator.Generate ());
		}
		
		[Test]
		public void OtherRecordTypes ()
		{
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public); 
			ClassItem item = new ClassItem ("Items", "SimpleClass", true, false);  
			ParseInfo parseInfo2 = new ParseInfo ("Container", true, Visibility.Public, true, item.Yield ()); 
			CodeGenerator generator = new CodeGenerator (new ParseInfo [] { parseInfo, parseInfo2 });
			Assert.AreEqual (@"using System.Collections.Immutable;

public partial class SimpleClass
{
}

public partial class Container
{
	public ImmutableList<SimpleClass> Items { get; }

	public Container (ImmutableList<SimpleClass> items)
	{
		Items = items;
	}

	public Container WithItems (ImmutableList<SimpleClass> items)
	{
		return new Container (items);
	}
}
", generator.Generate ());
		}
		
		[Test]
		public void With ()
		{
			ClassItem item = new ClassItem ("Foo", "Int32");  
			ClassItem item2 = new ClassItem ("Bar", "Int32");  
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public, true, new ClassItem [] { item, item2 }); 
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (@"public partial class SimpleClass
{
	public int Foo { get; }
	public int Bar { get; }

	public SimpleClass (int foo, int bar)
	{
		Foo = foo;
		Bar = bar;
	}

	public SimpleClass WithFoo (int foo)
	{
		return new SimpleClass (foo, Bar);
	}

	public SimpleClass WithBar (int bar)
	{
		return new SimpleClass (Foo, bar);
	}
}
", generator.Generate ());
		}
		
		[Test]
		public void WithOnSingleProperty ()
		{
			ClassItem item = new ClassItem ("Foo", "Int32", false, true);  
			ClassItem item2 = new ClassItem ("Bar", "Int32");
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public, false, new ClassItem [] { item, item2 }); 
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (@"public partial class SimpleClass
{
	public int Foo { get; }
	public int Bar { get; }

	public SimpleClass (int foo, int bar)
	{
		Foo = foo;
		Bar = bar;
	}

	public SimpleClass WithFoo (int foo)
	{
		return new SimpleClass (foo, Bar);
	}
}
", generator.Generate ());
		}

		[Test]
		public void Injection ()
		{
			ClassItem item = new ClassItem ("X", "Int32");
			ClassItem item2 = new ClassItem ("Y", "Int32");
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public, false, new ClassItem[] { item, item2 }, injectCode: "	int Size => X * Y;");
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (@"public partial class SimpleClass
{
	public int X { get; }
	public int Y { get; }

	public SimpleClass (int x, int y)
	{
		X = x;
		Y = y;
	}

	int Size => X * Y;
}
", generator.Generate ());
		}

		[Test]
		public void Inherit ()
		{
			ClassItem item = new ClassItem ("X", "Int32");
			ParseInfo parseInfo = new ParseInfo ("SimpleClass", true, Visibility.Public, false, item.Yield (), baseTypes: "IFoo");
			CodeGenerator generator = new CodeGenerator (parseInfo.Yield ());
			Assert.AreEqual (@"public partial class SimpleClass : IFoo
{
	public int X { get; }

	public SimpleClass (int x)
	{
		X = x;
	}
}
", generator.Generate ());
		}
	}
}
