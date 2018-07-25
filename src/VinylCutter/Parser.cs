using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace VinylCutter
{
	public class ClassItem
	{
		public string Name { get; private set; }
		public string TypeName { get; private set; }
		public bool IsCollection { get; private set; }
		public bool IncludeWith { get; private set; }

		public ClassItem (string name, string typeName) : this (name, typeName, false, false)
		{
		}

		public ClassItem (string name, string typeName, bool isCollection, bool includeWith)
		{
			Name = name;
			TypeName = typeName;
			IsCollection = isCollection;
			IncludeWith = includeWith;
		}
	}

	public enum Visibility { Public, Private }

	public class ParseInfo
	{
		public string Name { get; private set; }
		public Visibility Visibility { get; private set; }
		public bool IsClass { get; private set; }
		public ImmutableArray<ClassItem> Items;
		public bool IncludeWith { get; private set; }
		public string InjectCode { get; }

		public ParseInfo (string name, bool isClass, Visibility visibility, bool includeWith = false, IEnumerable<ClassItem> items = null, string injectCode = "")
		{
			Name = name;
			Visibility = visibility;
			IsClass = isClass;
			Items = ImmutableArray.CreateRange (items ?? Enumerable.Empty<ClassItem> ());
			IncludeWith = includeWith;
			InjectCode = injectCode;
		}
	}

	public class Parser
	{
		bool HasWith (ISymbol s) => s.GetAttributes ().Any (x => x.AttributeClass.Equals (WithAttributeSymbol));
		bool HasInject (ISymbol s) => s.GetAttributes ().Any (x => x.AttributeClass.Equals (InjectAttributeSymbol));

		static bool IsInternalConstruct (ISymbol s) => s.Name.Contains ("<") || s.Name.Contains (">");

		string Text;
		string Prelude = @"
using System;
using System.Collections.Generic;

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public class Skip : System.Attribute { } 

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class With : System.Attribute { } 

[AttributeUsage (AttributeTargets.All)]
public class Inject : System.Attribute { } 
";

		List<ParseInfo> Infos;
		INamedTypeSymbol AttributeSymbol;
		INamedTypeSymbol InjectAttributeSymbol;
		INamedTypeSymbol WithAttributeSymbol;
		INamedTypeSymbol SkipAttributeSymbol;
		INamedTypeSymbol ListSymbol;
		SemanticModel Model;

		public Parser (string text)
		{
			Text = text;
		}

		public List<ParseInfo> Parse ()
		{
			Infos = new List<ParseInfo> ();

			SyntaxTree tree = CSharpSyntaxTree.ParseText (Prelude + Text);
			var root = (CompilationUnitSyntax)tree.GetRoot ();
			var mscorlib = MetadataReference.CreateFromFile (typeof (object).Assembly.Location);
			var compilation = CSharpCompilation.Create ("Vinyl").AddReferences (mscorlib).AddSyntaxTrees (tree);

			AttributeSymbol = compilation.GetTypeByMetadataName (typeof (System.Attribute).FullName);
			InjectAttributeSymbol = compilation.GetTypeByMetadataName ("Inject");
			WithAttributeSymbol = compilation.GetTypeByMetadataName ("With");
			SkipAttributeSymbol = compilation.GetTypeByMetadataName ("Skip");
			ListSymbol = compilation.GetTypeByMetadataName ("System.Collections.Generic.List`1");

			Model = compilation.GetSemanticModel (tree);

			foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
				HandlePossibleRecord (Model.GetDeclaredSymbol (c));

			foreach (var s in root.DescendantNodes ().OfType<StructDeclarationSyntax> ())
				HandlePossibleRecord (Model.GetDeclaredSymbol (s));

			return Infos;
		}

		void HandlePossibleRecord (INamedTypeSymbol itemInfo)
		{
			if (itemInfo.IsAbstract || itemInfo.EnumUnderlyingType != null)
				return;

			if (itemInfo.BaseType.Equals (AttributeSymbol))
				return;

			if (itemInfo.GetAttributes ().Any (x => x.AttributeClass.Equals (SkipAttributeSymbol)))
				return;

			List<ClassItem> classItems = new List<ClassItem> ();

			foreach (var member in itemInfo.GetMembers ().OfType<IPropertySymbol>().Where (x => !IsInternalConstruct (x) && !HasInject (x))) 
				classItems.Add (CreateClassItem (member, member.Type));

			foreach (var field in itemInfo.GetMembers ().OfType<IFieldSymbol>().Where (x => !IsInternalConstruct(x) && !HasInject (x)))
				classItems.Add (CreateClassItem (field, field.Type));

			string injectCode = GetInjectionCode (itemInfo);

			Visibility visibility = itemInfo.DeclaredAccessibility == Accessibility.Public ? Visibility.Public : Visibility.Private;
			Infos.Add (new ParseInfo (itemInfo.Name, itemInfo.IsReferenceType, visibility, HasWith (itemInfo), classItems, injectCode));
		}

		string GetInjectionCode (INamedTypeSymbol itemInfo)
		{
			SourceText sourceText = Model.SyntaxTree.GetText ();
			StringBuilder builder = new StringBuilder ();
			foreach (var injectItem in itemInfo.GetMembers ().Where (x => HasInject (x)))
			{
				foreach (var syntaxReferece in injectItem.DeclaringSyntaxReferences)
				{
					var lines = sourceText.GetSubText (syntaxReferece.Span).Lines;
					builder.Append (string.Join ("\n", lines.Skip (1)));
				}
			}
			return builder.ToString ();
		}

		ClassItem CreateClassItem (ISymbol symbol, ITypeSymbol type)
		{
			if (type.OriginalDefinition.Equals (ListSymbol))
			{
				INamedTypeSymbol t = (INamedTypeSymbol)type;
				return new ClassItem (symbol.Name, t.TypeArguments[0].Name, true, HasWith (symbol));
			}
			return new ClassItem (symbol.Name, type.Name, false, HasWith (symbol));
		}
	}
}
