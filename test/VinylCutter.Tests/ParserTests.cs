using System;
using System.Linq;
using NUnit.Framework;

namespace VinylCutter.Tests
{
	[TestFixture]
	public class ParserTests
	{
		FileInfo Parse (string text)
		{
			Parser parser = new Parser (text);
			return parser.Parse ();
		}
		
		[Test]
		[TestCase ("public class SimpleClass { }", "SimpleClass", true)]
		[TestCase ("public struct SimpleStruct { }", "SimpleStruct", false)]
		public void SimpleReflectedInfo (string text, string name, bool isClass)
		{
			FileInfo file = Parse (text);
			Assert.AreEqual (1, file.Records.Length);
			Assert.AreEqual (name, file.Records[0].Name);
			Assert.AreEqual (isClass, file.Records[0].IsClass);
			Assert.IsFalse (file.Records[0].IncludeWith);
			Assert.AreEqual ("", file.Records[0].BaseTypes);
			Assert.AreEqual ("", file.InjectCode);
			Assert.AreEqual ("", file.GlobalNamespace);
		}

		[Test]
		public void PropertiesAreTracked ()
		{
			FileInfo file = Parse ("public class SimpleClass { int X { get; } }");
			Assert.AreEqual (1, file.Records[0].Items.Length);
			Assert.AreEqual ("X", file.Records[0].Items[0].Name);
			Assert.AreEqual ("Int32", file.Records[0].Items[0].TypeName);
			Assert.IsFalse (file.Records[0].Items[0].IsCollection);
			Assert.IsFalse (file.Records[0].Items[0].IncludeWith);
		}

		[Test]
		public void VariablesAreTracked ()
		{
			FileInfo file = Parse ("public class SimpleClass { double Y; }");

			Assert.AreEqual (1, file.Records[0].Items.Length);
			Assert.AreEqual ("Y", file.Records[0].Items [0].Name);
			Assert.AreEqual ("Double", file.Records[0].Items [0].TypeName);
			Assert.IsFalse (file.Records[0].Items [0].IsCollection);
			Assert.IsFalse (file.Records[0].Items [0].IncludeWith);
		}

		[Test]
		public void IEnumerables ()
		{
			FileInfo file = Parse ("public class SimpleClass { List<int> Z; }");

			Assert.AreEqual (1, file.Records[0].Items.Length);
			Assert.AreEqual ("Z", file.Records[0].Items [0].Name);
			Assert.IsTrue(file.Records[0].Items [0].IsCollection);
			Assert.AreEqual ("Int32", file.Records[0].Items [0].TypeName);
		}

		[Test]
		public void OtherRecordTypes ()
		{
			FileInfo file = Parse (@"
public class Element { int X; }
public class Container { List <Element> E; }
");

			Assert.AreEqual (1, file.Records[0].Items.Length);
			var container = file.Records.First (x => x.Name == "Container");
			Assert.AreEqual ("E", container.Items [0].Name);
			Assert.AreEqual ("Element", container.Items [0].TypeName);
			Assert.IsTrue (container.Items [0].IsCollection);
		}

		[Test]
		public void ClassWithAttributes ()
		{
			FileInfo file = Parse (@"
[With]
public class SimpleClass { int X; }
");

			Assert.IsTrue (file.Records[0].IncludeWith);
			Assert.IsFalse (file.Records[0].Items[0].IncludeWith);

		}

		[Test]
		public void ItemSpecificWithAttributes ()
		{
			FileInfo file = Parse (@"
public class SimpleClass { [With] int X; }
");

			Assert.IsFalse (file.Records[0].IncludeWith);
			Assert.IsTrue (file.Records[0].Items[0].IncludeWith);
		}
		
		[Test]
		public void Visibilities ()
		{
			Func <string, Visibility> parseVisibility = s => (Parse(s).Records[0].Visibility);

			Assert.AreEqual (Visibility.Public, parseVisibility ("public class SimpleClass {}"));
			Assert.AreEqual (Visibility.Private, parseVisibility ("class SimpleClass {}"));
		}

		[Test]
		public void Skip ()
		{
			FileInfo file = Parse (@"
public class SimpleClass { int X; }
[Skip]
public class SkippedSimpleClass { int X; }
[Skip]
public interface SkippedInterface { int X { get; } }
");

			Assert.AreEqual (1, file.Records.Length);
			Assert.AreEqual ("SimpleClass", file.Records[0].Name);
		}

		[Test]
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
			Assert.AreEqual (0, file.Records.Length);
		}

		[Test]
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
			Assert.AreEqual ("\tint Size => X * Y;", file.Records[0].InjectCode);
			Assert.AreEqual (2, file.Records[0].Items.Length);
		}

		[Test]
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
			Assert.AreEqual ("public enum Visibility { Public, Private }", file.InjectCode);
			Assert.AreEqual (1, file.Records.Length);
			Assert.AreEqual ("\tbool Show => Status == Visibility.Public;", file.Records[0].InjectCode);
			Assert.AreEqual (2, file.Records[0].Items.Length);
		}

		[Test]
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
			Assert.AreEqual ("Test", file.GlobalNamespace);
			Assert.AreEqual ("\tpublic enum Visibility { Public, Private }", file.InjectCode);
			Assert.AreEqual (1, file.Records.Length);
			Assert.AreEqual ("\t\tbool Show => Status == Visibility.Public;", file.Records[0].InjectCode);
			Assert.AreEqual (2, file.Records[0].Items.Length);
		}

		[Test]
		public void Inherit ()
		{
			FileInfo file = Parse (@"public interface IFoo {} public class Foo {}
public class SimpleClass : Foo, IFoo
{
	int X; 
	int Y; 
}
");
			Assert.AreEqual ("Foo, IFoo", file.Records.First (x => x.Name == "SimpleClass").BaseTypes);
		}

		[Test]
		public void Default ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default (""0"")]
	int X;
	int Y; 
}
");
			Assert.AreEqual ("0", file.Records[0].Items[0].DefaultValue);
			Assert.AreEqual (null, file.Records[0].Items[1].DefaultValue);
		}

		[Test]
		public void NullDefault ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default (""null"")]
	string X;
}
");
			Assert.AreEqual ("null", file.Records[0].Items[0].DefaultValue);
		}

		[Test]
		public void BoolDefault ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default (""false"")]
	bool X;
}
");
			Assert.AreEqual ("false", file.Records[0].Items[0].DefaultValue);
		}

		[Test]
		public void EmptyStringDefault ()
		{
			FileInfo file = Parse (@"public class SimpleClass
{
	[Default ("""")]
	bool X;
}
");
			Assert.AreEqual ("\"\"", file.Records[0].Items[0].DefaultValue);
		}

		[Test]
		public void Namespace ()
		{
			FileInfo file = Parse (@"namespace Test { public class SimpleClass { } }");
			Assert.AreEqual ("Test", file.GlobalNamespace);
		}

		[Test]
		public void CompileError ()
		{
			Parser parser = new Parser (@"public class SimpleClass
{
");
			Assert.Throws<ParseCompileError> (() => parser.Parse ());
		}
	}
}
