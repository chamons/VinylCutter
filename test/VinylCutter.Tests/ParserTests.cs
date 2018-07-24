using System;
using System.Linq;
using Mono.Cecil;
using NUnit.Framework;

namespace VinylCutter.Tests
{

		
	[TestFixture]
	public class ParserTests
	{
		[Test]
		[TestCase ("public class SimpleClass { }", "SimpleClass", true)]
		[TestCase ("public struct SimpleStruct { }", "SimpleStruct", false)]
		public void SimpleReflectedInfo (string text, string name, bool isClass)
		{
			Parser parser = new Parser (text);
			var info = parser.Parse ();
			Assert.AreEqual (1, info.Count);
			Assert.AreEqual (name, info [0].Name);
			Assert.AreEqual (isClass, info [0].IsClass);
			Assert.IsTrue (info [0].IncludeWith);
		}

		[Test]
		public void PropertiesAreTracked ()
		{
			Parser parser = new Parser ("public class SimpleClass { int X { get; } }");
			var info = parser.Parse ();
			Assert.AreEqual (1, info [0].Items.Length);
			Assert.AreEqual ("X", info [0].Items [0].Name);
			Assert.AreEqual ("Int32", info [0].Items [0].TypeName);
			Assert.IsFalse (info [0].Items [0].IsCollection);
			Assert.IsFalse (info [0].Items [0].ForcedIncludeWith);
		}

		[Test]
		public void VariablesAreTracked ()
		{
			Parser parser = new Parser ("public class SimpleClass { double Y; }");
			var info = parser.Parse ();
			Assert.AreEqual (1, info [0].Items.Length);
			Assert.AreEqual ("Y", info [0].Items [0].Name);
			Assert.AreEqual ("Double", info [0].Items [0].TypeName);
			Assert.IsFalse (info [0].Items [0].IsCollection);
			Assert.IsFalse (info [0].Items [0].ForcedIncludeWith);
		}

		[Test]
		public void IEnumerables ()
		{
			Parser parser = new Parser ("public class SimpleClass { List<int> Z; }");
			var info = parser.Parse ();
			Assert.AreEqual (1, info [0].Items.Length);
			Assert.AreEqual ("Z", info [0].Items [0].Name);
			Assert.IsTrue(info [0].Items [0].IsCollection);
			Assert.AreEqual ("Int32", info [0].Items [0].TypeName);
		}

		[Test]
		public void OtherRecordTypes ()
		{
			Parser parser = new Parser (@"
public class Element { int X; }
public class Container { List <Element> E; }
");
			var info = parser.Parse ();
			Assert.AreEqual (1, info [0].Items.Length);
			var container = info.First (x => x.Name == "Container");
			Assert.AreEqual ("E", container.Items [0].Name);
			Assert.AreEqual ("Element", container.Items [0].TypeName);
			Assert.IsTrue (container.Items [0].IsCollection);
		}

		[Test]
		public void ClassWithoutAttributes ()
		{
			Parser parser = new Parser (@"
[Without]
public class SimpleClass { int X; }
");
			var info = parser.Parse ();
			Assert.IsFalse (info [0].IncludeWith);
			Assert.IsFalse (info [0].Items[0].ForcedIncludeWith);

		}

		[Test]
		public void ItemSpecificWithAttributes ()
		{
			Parser parser = new Parser (@"
[Without]
public class SimpleClass { [With] int X; }
");
			var info = parser.Parse ();
			Assert.IsFalse (info [0].IncludeWith);
			Assert.IsTrue (info [0].Items[0].ForcedIncludeWith);
		}
		
		[Test]
		public void Visibilities ()
		{
			Func <string, Visibility> parseVisibility = s => (new Parser (s)).Parse()[0].Visibility;

			Assert.AreEqual (Visibility.Public, parseVisibility ("public class SimpleClass {}"));
			Assert.AreEqual (Visibility.Private, parseVisibility ("class SimpleClass {}"));
		}


		[Test]
		public void ThrowOnInvalidCompiledInput ()
		{
			Parser parser = new Parser (@"public class CompilerError {");
			Assert.Throws<ParseCompileError> (() => parser.Parse ());
		}
	}
}
