using System;
using System.Linq;
using Xunit;

namespace VinylCutter.Tests
{
	public class ParserTests
	{
		FileInfo Parse (string text)
		{
			Parser parser = new Parser (text);
			return parser.Parse ();
		}
		
		[Theory]
		[InlineData ("public class SimpleClass { }", "SimpleClass", true)]
		[InlineData ("public struct SimpleStruct { }", "SimpleStruct", false)]
		public void SimpleReflectedInfo (string text, string name, bool isClass)
		{
			FileInfo file = Parse (text);
			Assert.Single (file.Records);
			Assert.Equal (name, file.Records[0].Name);
			Assert.Equal (isClass, file.Records[0].IsClass);
			Assert.False (file.Records[0].IncludeWith);
			Assert.Equal ("", file.Records[0].BaseTypes);
			Assert.Equal ("", file.InjectCode);
			Assert.Equal ("", file.GlobalNamespace);
		}

		[Fact]
		public void PropertiesAreTracked ()
		{
			FileInfo file = Parse ("public class SimpleClass { int X { get; } }");
			Assert.Single (file.Records[0].Items);
			Assert.Equal ("X", file.Records[0].Items[0].Name);
			Assert.Equal ("Int32", file.Records[0].Items[0].TypeName);
			Assert.Equal (CollectionType.None, file.Records[0].Items[0].CollectionType);
			Assert.False (file.Records[0].Items[0].IncludeWith);
			Assert.False (file.Records[0].Items[0].IsMutable);
		}

		[Fact]
		public void VariablesAreTracked ()
		{
			FileInfo file = Parse ("public class SimpleClass { double Y; }");

			Assert.Single (file.Records[0].Items);
			Assert.Equal ("Y", file.Records[0].Items [0].Name);
			Assert.Equal ("Double", file.Records[0].Items [0].TypeName);
			Assert.Equal (CollectionType.None, file.Records[0].Items[0].CollectionType);
			Assert.False (file.Records[0].Items [0].IncludeWith);
			Assert.False (file.Records[0].Items [0].IsMutable);

		}

		[Fact]
		public void IEnumerables ()
		{
			FileInfo file = Parse ("public class SimpleClass { List<int> Z; }");

			Assert.Single (file.Records[0].Items);
			Assert.Equal ("Z", file.Records[0].Items [0].Name);
			Assert.Equal (CollectionType.List, file.Records[0].Items[0].CollectionType);
			Assert.Equal ("int", file.Records[0].Items [0].TypeName);
		}

		[Fact]
		public void HashSet ()
		{
			FileInfo file = Parse ("public class SimpleClass { HashSet<int> Z; }");

			Assert.Single (file.Records[0].Items);
			Assert.Equal ("Z", file.Records[0].Items [0].Name);
			Assert.Equal (CollectionType.HashSet, file.Records[0].Items[0].CollectionType);
			Assert.Equal ("int", file.Records[0].Items [0].TypeName);	
		}

		[Fact]
		public void CollectionsWithGenerics ()
		{
			FileInfo file = Parse ("public class SimpleClass { List<List<int>> Z; }");

			Assert.Single (file.Records[0].Items);
			Assert.Equal ("Z", file.Records[0].Items [0].Name);
			Assert.Equal (CollectionType.List, file.Records[0].Items[0].CollectionType);
			Assert.Equal ("List<int>", file.Records[0].Items [0].TypeName);
		}

		[Fact]
		public void CollectionsWithDictionary ()
		{
			FileInfo file = Parse ("public class SimpleClass { Dictionary<int, string> Z; }");

			Assert.Single (file.Records[0].Items);
			Assert.Equal ("Z", file.Records[0].Items[0].Name);
			Assert.Equal (CollectionType.Dictionary, file.Records[0].Items[0].CollectionType);
			Assert.Equal ("int,string", file.Records[0].Items[0].TypeName);
		}

        [Fact]
		public void OtherRecordTypes ()
		{
			FileInfo file = Parse (@"
public class Element { int X; }
public class Container { List <Element> E; }
");

			Assert.Single (file.Records[0].Items);
			var container = file.Records.First (x => x.Name == "Container");
			Assert.Equal ("E", container.Items [0].Name);
			Assert.Equal ("Element", container.Items [0].TypeName);
			Assert.Equal (CollectionType.List, container.Items [0].CollectionType);
		}

		[Fact]
		public void ClassWithAttributes ()
		{
			FileInfo file = Parse (@"
[With]
public class SimpleClass { int X; }
");

			Assert.True (file.Records[0].IncludeWith);
			Assert.False (file.Records[0].Items[0].IncludeWith);

		}

		[Fact]
		public void ItemSpecificWithAttributes ()
		{
			FileInfo file = Parse (@"
public class SimpleClass { [With] int X; }
");

			Assert.False (file.Records[0].IncludeWith);
			Assert.True (file.Records[0].Items[0].IncludeWith);
		}
		
		[Fact]
		public void Visibilities ()
		{
			Func <string, Visibility> parseVisibility = s => (Parse(s).Records[0].Visibility);

			Assert.Equal (Visibility.Public, parseVisibility ("public class SimpleClass {}"));
			Assert.Equal (Visibility.Private, parseVisibility ("class SimpleClass {}"));
		}

		[Fact]
		public void Skip ()
		{
			FileInfo file = Parse (@"
public class SimpleClass { int X; }
[Skip]
public class SkippedSimpleClass { int X; }
[Skip]
public interface SkippedInterface { int X { get; } }
");

			Assert.Single (file.Records);
			Assert.Equal ("SimpleClass", file.Records[0].Name);
		}

		[Fact]
		public void SkipEnums ()
		{
			FileInfo file = Parse (@"
public enum ParsingConfidence
{
	High,
	Likely,
	Low,
	Invalid,
}
");
			Assert.Empty (file.Records);
		}

		[Fact]
		public void Inject ()
		{
			FileInfo file = Parse (@"public class SimpleClass 
{
	int X; 
	int Y; 

	[Inject]
	int Size => X * Y;
}
");
			Assert.Equal ("\tint Size => X * Y;", file.Records[0].InjectCode);
			Assert.Equal (2, file.Records[0].Items.Length);
		}

		[Fact]
		public void InjectTopLevelItems ()
		{
			FileInfo file = Parse (@"[Inject]
public enum Visibility { Public, Private }

public class SimpleClass
{
	Visibility Status;
	int Size;

	[Inject]
	bool Show => Status == Visibility.Public;
}
");
			Assert.Equal ("public enum Visibility { Public, Private }", file.InjectCode);
			Assert.Single (file.Records);
			Assert.Equal ("\tbool Show => Status == Visibility.Public;", file.Records[0].InjectCode);
			Assert.Equal (2, file.Records[0].Items.Length);
		}

		[Fact]
		public void InjectTopLevelItemsWithNamespace ()
		{
			FileInfo file = Parse (@"namespace Test
{
	[Inject]
	public enum Visibility { Public, Private }

	public class SimpleClass
	{
		Visibility Status;
		int Size;

		[Inject]
		bool Show => Status == Visibility.Public;
	}
}
");
			Assert.Equal ("Test", file.GlobalNamespace);
			Assert.Equal ("\tpublic enum Visibility { Public, Private }", file.InjectCode);
			Assert.Single (file.Records);
			Assert.Equal ("\t\tbool Show => Status == Visibility.Public;", file.Records[0].InjectCode);
			Assert.Equal (2, file.Records[0].Items.Length);
		}

		[Fact]
		public void Inherit ()
		{
			FileInfo file = Parse (@"public interface IFoo {} public class Foo {}
public class SimpleClass : Foo, IFoo
{
	int X; 
	int Y; 
}
");
			Assert.Equal ("Foo, IFoo", file.Records.First (x => x.Name == "SimpleClass").BaseTypes);
		}

		[Fact]
		public void Default ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default (""0"")]
	int X;
	int Y; 
}
");
			Assert.Equal ("0", file.Records[0].Items[0].DefaultValue);
			Assert.Null (file.Records[0].Items[1].DefaultValue);
		}

		[Fact]
		public void NullDefault ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default (""null"")]
	string X;
}
");
			Assert.Equal ("null", file.Records[0].Items[0].DefaultValue);
		}

		[Fact]
		public void BoolDefault ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default (""false"")]
	bool X;
}
");
			Assert.Equal ("false", file.Records[0].Items[0].DefaultValue);
		}

		[Fact]
		public void EmptyStringDefault ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default ("""")]
	bool X;
}
");
			Assert.Equal ("\"\"", file.Records[0].Items[0].DefaultValue);
		}

		[Fact]
		public void Namespace ()
		{
			FileInfo file = Parse (@"namespace Test { public class SimpleClass { } }");
			Assert.Equal ("Test", file.GlobalNamespace);
		}

		[Fact]
		public void DottedNamespace ()
		{
			FileInfo file = Parse (@"namespace Test.Second { public class SimpleClass { } }");
			Assert.Equal ("Test.Second", file.GlobalNamespace);
		}

		[Fact]
		public void Mutable ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	bool X;

	[Mutable]
	object o;

	[Mutable]
	List<object> Caches;
}
");
			Assert.False (file.Records[0].Items[0].IsMutable);
			Assert.True (file.Records[0].Items[1].IsMutable);
			Assert.Equal ("Object", file.Records[0].Items[1].TypeName);
			Assert.True (file.Records[0].Items[2].IsMutable);
			Assert.Equal ("List<object>", file.Records[0].Items[2].TypeName);
			Assert.Equal (CollectionType.None, file.Records[0].Items[2].CollectionType);
		}

		[Fact]
		public void MutableWithGeneric ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Mutable]
	List<List<int>> o;
}
");
			Assert.True (file.Records[0].Items[0].IsMutable);
			Assert.Equal ("List<List<int>>", file.Records[0].Items[0].TypeName);
		}

		[Fact]
		public void IgnoresStubs ()
		{
			FileInfo file = Parse (@"public class Character
	{
		string Name;
	}

	interface CharacterResolver {}");
			
			Assert.Single (file.Records);
			Assert.Equal ("Character", file.Records[0].Name);
		}

		[Fact]
		public void MultipleInjectsHaveProperWhitespaceInRecord ()
		{
			FileInfo file = Parse (@"public class Character
	{
		string Name;

		[Inject]
		public interface Foo {}

		[Inject]
		public interface Bar {}
	}");

			Assert.Equal ("\t\tpublic interface Foo {}\n\n\t\tpublic interface Bar {}", file.Records[0].InjectCode);
		}

		[Fact]
		public void MultipleInjectsHaveProperWhitespaceTopLevel ()
		{
			FileInfo file = Parse (@"		[Inject]
		public interface Foo {}

		[Inject]
		public interface Bar {}

	public class Character
	{
		string Name;
	}");

			Assert.Equal ("\t\tpublic interface Foo {}\n\n\t\tpublic interface Bar {}", file.InjectCode);
		}

		[Fact]
		public void CompileError ()
		{
			Parser parser = new Parser (@"public class SimpleClass
{
");
			Assert.Throws<ParseCompileError> (() => parser.Parse ());
		}
	}
}
