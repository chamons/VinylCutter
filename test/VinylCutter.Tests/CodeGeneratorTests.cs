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
		public void GenerateBasicProperties ()
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
	}
}
