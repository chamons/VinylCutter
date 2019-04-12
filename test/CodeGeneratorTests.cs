using System;
using System.Collections.Generic;
using Xunit;

namespace VinylCutter.Tests
{
	public class CodeGeneratorTests
	{
		string Generate (RecordInfo record, string injectCode = "", string globalNamespace = "") => Generate (record.Yield (), injectCode, globalNamespace);

		string Generate (IEnumerable<RecordInfo> records, string injectCode = "", string globalNamespace = "")
		{
			FileInfo file = new FileInfo (records, injectCode: injectCode, globalNamespace: globalNamespace);
			CodeGenerator generator = new CodeGenerator (file);
			return generator.Generate ();
		}
				
		[Theory]
		[InlineData ("SimpleClass", Visibility.Public, true, "public partial class SimpleClass\n{\n}\n")]
		[InlineData ("SimpleClass", Visibility.Private, true, "partial class SimpleClass\n{\n}\n")]
		[InlineData ("SimpleStruct", Visibility.Public, false, "public partial struct SimpleStruct\n{\n}\n")]
		public void GenerateSimpleClass (string name, Visibility visibility, bool isClass, string expected)
		{
			RecordInfo record = new RecordInfo (name, isClass, visibility);
			Assert.Equal (expected, Generate (record));
		}
		
		[Fact]
		public void SpacesBetweenRecords ()
		{
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public); 
			RecordInfo record2 = new RecordInfo ("SimpleClass2", true, Visibility.Public);

			string expected = @"public partial class SimpleClass
{
}

public partial class SimpleClass2
{
}
";

			Assert.Equal (expected, Generate (new RecordInfo[] { record, record2 }), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void GenerateBasicProperties ()
		{
			ItemInfo item = new ItemInfo ("Foo", "Int32");  
			ItemInfo item2 = new ItemInfo ("Bar", "Int32");  
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, new ItemInfo [] { item, item2 }); 

			string expected = @"public partial class SimpleClass
{
	public int Foo { get; }
	public int Bar { get; }

	public SimpleClass (int foo, int bar)
	{
		Foo = foo;
		Bar = bar;
	}
}
";
			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);

		}

		[Fact]
		public void Enumerables ()
		{
			ItemInfo item = new ItemInfo ("Foo", "Int32", true);  
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, true, item.Yield ()); 

			string expected = @"using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public partial class SimpleClass
{
	public ImmutableArray<int> Foo { get; }

	public SimpleClass (IEnumerable<int> foo)
	{
		Foo = ImmutableArray.CreateRange (foo ?? Array.Empty<int> ());
	}

	public SimpleClass WithFoo (IEnumerable<int> foo)
	{
		return new SimpleClass (foo);
	}
}
";
			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void Dictionary ()
		{
			ItemInfo item = new ItemInfo ("Foo", "string,Int32", false, true);
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, true, item.Yield ());

			string expected = @"using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public partial class SimpleClass
{
	public ImmutableDictionary<string, int> Foo { get; }

	public SimpleClass (Dictionary<string, int> foo)
	{
		Foo = foo.ToImmutableDictionary ();
	}

	public SimpleClass WithFoo (Dictionary<string, int> foo)
	{
		return new SimpleClass (foo);
	}
}
";
			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}

        [Fact]
		public void OtherRecordTypes ()
		{
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public); 
			ItemInfo item = new ItemInfo ("Items", "SimpleClass", true, false);  
			RecordInfo record2 = new RecordInfo ("Container", true, Visibility.Public, true, item.Yield ()); 

			string expected = @"using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public partial class SimpleClass
{
}

public partial class Container
{
	public ImmutableArray<SimpleClass> Items { get; }

	public Container (IEnumerable<SimpleClass> items)
	{
		Items = ImmutableArray.CreateRange (items ?? Array.Empty<SimpleClass> ());
	}

	public Container WithItems (IEnumerable<SimpleClass> items)
	{
		return new Container (items);
	}
}
";

			Assert.Equal (expected, Generate (new RecordInfo[] { record, record2 }), ignoreLineEndingDifferences: true);
		}
		
		[Fact]
		public void With ()
		{
			ItemInfo item = new ItemInfo ("Foo", "Int32");  
			ItemInfo item2 = new ItemInfo ("Bar", "Int32");  
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, true, new ItemInfo [] { item, item2 });

			string expected = @"public partial class SimpleClass
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
";
			
			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}
		
		[Fact]
		public void WithOnSingleProperty ()
		{
			ItemInfo item = new ItemInfo ("Foo", "Int32", false, false, true);  
			ItemInfo item2 = new ItemInfo ("Bar", "Int32");
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, new ItemInfo [] { item, item2 });

			string expected = @"public partial class SimpleClass
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
";

			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void Injection ()
		{
			ItemInfo item = new ItemInfo ("X", "Int32");
			ItemInfo item2 = new ItemInfo ("Y", "Int32");
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, new ItemInfo[] { item, item2 }, injectCode: "	int Size => X * Y;");

			string expected = @"public partial class SimpleClass
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
";

			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void TopLevelInjection ()
		{
			ItemInfo item = new ItemInfo ("X", "Int32");
			ItemInfo item2 = new ItemInfo ("Y", "Int32");
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, new ItemInfo[] { item, item2 });

			string expected = @"namespace Test
{
	public enum Visibility { Public, Private }

	public partial class SimpleClass
	{
		public int X { get; }
		public int Y { get; }

		public SimpleClass (int x, int y)
		{
			X = x;
			Y = y;
		}
	}
}
";
			Assert.Equal (expected, Generate (record, injectCode : "\tpublic enum Visibility { Public, Private }", globalNamespace : "Test"), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void Inherit ()
		{
			ItemInfo item = new ItemInfo ("X", "Int32");
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, item.Yield (), baseTypes: "IFoo");

			string expected = @"public partial class SimpleClass : IFoo
{
	public int X { get; }

	public SimpleClass (int x)
	{
		X = x;
	}
}
";

			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void Default ()
		{
			ItemInfo item = new ItemInfo ("X", "Int32", defaultValue: "42");
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, item.Yield ());

			string expected = @"public partial class SimpleClass
{
	public int X { get; }

	public SimpleClass (int x = 42)
	{
		X = x;
	}
}
";

			Assert.Equal (expected, Generate (record), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void Namespace ()
		{
			ItemInfo item = new ItemInfo ("X", "Int32", defaultValue: "42");
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false, item.Yield ());

			string expected = @"namespace Test
{
	public partial class SimpleClass
	{
		public int X { get; }

		public SimpleClass (int x = 42)
		{
			X = x;
		}
	}
}
";

			Assert.Equal (expected, Generate (record, globalNamespace : "Test"), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void DottedNamespace ()
		{
			RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, false);

			string expected = @"namespace Test.Second
{
	public partial class SimpleClass
	{
	}
}
";

			Assert.Equal (expected, Generate (record, globalNamespace : "Test.Second"), ignoreLineEndingDifferences: true);
		}

		[Fact]
		public void FullCapsNames ()
		{
			{
				ItemInfo item = new ItemInfo ("ID", "Int32");
				ItemInfo item2 = new ItemInfo ("UID", "Int32", true);

				RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, true, new ItemInfo [] { item, item2 });

				string expected = @"using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Test
{
	public partial class SimpleClass
	{
		public int ID { get; }
		public ImmutableArray<int> UID { get; }

		public SimpleClass (int id, IEnumerable<int> uid)
		{
			ID = id;
			UID = ImmutableArray.CreateRange (uid ?? Array.Empty<int> ());
		}

		public SimpleClass WithID (int id)
		{
			return new SimpleClass (id, UID);
		}

		public SimpleClass WithUID (IEnumerable<int> uid)
		{
			return new SimpleClass (ID, uid);
		}
	}
}
";

				Assert.Equal (expected, Generate (record, globalNamespace : "Test"), ignoreLineEndingDifferences: true);

			}
		}

		[Fact]
		public void Mutable ()
		{
			{
				ItemInfo item = new ItemInfo ("ID", "Int32");
				ItemInfo item2 = new ItemInfo ("Cache", "object", isMutable: true);
				ItemInfo item3 = new ItemInfo ("Lookup", "List<object>", isMutable: true);

				RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, true, new ItemInfo [] { item, item2, item3 });

				string expected = @"namespace Test
{
	public partial class SimpleClass
	{
		public int ID { get; }
		object Cache;
		List<object> Lookup;

		public SimpleClass (int id)
		{
			ID = id;
		}

		public SimpleClass WithID (int id)
		{
			return new SimpleClass (id) { Cache = this.Cache, Lookup = this.Lookup };
		}
	}
}
";

				Assert.Equal (expected, Generate (record, globalNamespace : "Test"), ignoreLineEndingDifferences: true);

			}
		}

		[Fact]
		public void MutableGeneric ()
		{
			{
				ItemInfo item = new ItemInfo ("Lookup", "List <Int32>", isMutable: true);

				RecordInfo record = new RecordInfo ("SimpleClass", true, Visibility.Public, true, new ItemInfo [] { item });

				string expected = @"namespace Test
{
	public partial class SimpleClass
	{
		List <Int32> Lookup;

		public SimpleClass ()
		{
		}
	}
}
";

				Assert.Equal (expected, Generate (record, globalNamespace : "Test"), ignoreLineEndingDifferences: true);

			}
		}
	}
}
