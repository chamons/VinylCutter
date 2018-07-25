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
		public string DefaultValue { get; private set; }

		public ClassItem (string name, string typeName, bool isCollection = false, bool includeWith = false, string defaultValue = null)
		{
			Name = name;
			TypeName = typeName;
			IsCollection = isCollection;
			IncludeWith = includeWith;
			DefaultValue = defaultValue;
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
		public string BaseTypes { get; private set; }
		public string InjectCode { get; }

		public ParseInfo (string name, bool isClass, Visibility visibility, bool includeWith = false, IEnumerable<ClassItem> items = null, string baseTypes = "", string injectCode = "")
		{
			Name = name;
			Visibility = visibility;
			IsClass = isClass;
			Items = ImmutableArray.CreateRange (items ?? Enumerable.Empty<ClassItem> ());
			IncludeWith = includeWith;
			BaseTypes = baseTypes;
			InjectCode = injectCode;
		}
	}

	public class Parser
	{
		bool HasWith (ISymbol s) => s.GetAttributes ().Any (x => x.AttributeClass.Equals (Symbols.WithAttribute));
		bool HasInject (ISymbol s) => s.GetAttributes ().Any (x => x.AttributeClass.Equals (Symbols.InjectAttribute));

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

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
public class Default : System.Attribute { public Default (object value) {} }
";

		List<ParseInfo> Infos;
		Symbols Symbols;
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

			Symbols = new Symbols (compilation);

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

			if (itemInfo.BaseType.Equals (Symbols.Attribute))
				return;

			if (itemInfo.GetAttributes ().Any (x => x.AttributeClass.Equals (Symbols.SkipAttribute)))
				return;

			List<ClassItem> classItems = new List<ClassItem> ();

			foreach (var member in itemInfo.GetMembers ().OfType<IPropertySymbol>().Where (x => !IsInternalConstruct (x) && !HasInject (x))) 
				classItems.Add (CreateClassItem (member, member.Type));

			foreach (var field in itemInfo.GetMembers ().OfType<IFieldSymbol>().Where (x => !IsInternalConstruct(x) && !HasInject (x)))
				classItems.Add (CreateClassItem (field, field.Type));

			string baseType = GetBaseTypes (itemInfo);
			string injectCode = GetInjectionCode (itemInfo);

			Visibility visibility = itemInfo.DeclaredAccessibility == Accessibility.Public ? Visibility.Public : Visibility.Private;
			Infos.Add (new ParseInfo (itemInfo.Name, itemInfo.IsReferenceType, visibility, includeWith : HasWith (itemInfo), items: classItems, baseTypes: baseType, injectCode: injectCode));
		}

		string GetBaseTypes (INamedTypeSymbol itemInfo)
		{
			List<string> baseTypes = new List<string> ();
			string baseType = GetBaseType (itemInfo);
			if (baseType != null)
				baseTypes.Add (baseType);
			baseTypes.AddRange (itemInfo.Interfaces.Select (x => x.Name));
			return string.Join (", ", baseTypes);
		}

		string GetBaseType (INamedTypeSymbol itemInfo)
		{
			if (itemInfo.IsValueType)
				return itemInfo.BaseType.Equals (Symbols.ValueType) ? null : itemInfo.BaseType.Name;
			else
				return itemInfo.BaseType.Equals (Symbols.Object) ? null : itemInfo.BaseType.Name;
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
			string defaultValue = GetDefaultValue (symbol);
			if (type.OriginalDefinition.Equals (Symbols.List))
			{
				INamedTypeSymbol t = (INamedTypeSymbol)type;
				return new ClassItem (symbol.Name, t.TypeArguments[0].Name, true, HasWith (symbol), defaultValue);
			}
			return new ClassItem (symbol.Name, type.Name, false, HasWith (symbol), defaultValue);
		}
	
		string GetDefaultValue (ISymbol symbol)
		{
			AttributeData defaultAttribute = symbol.GetAttributes ().FirstOrDefault (x => x.AttributeClass.Equals (Symbols.DefaultAttribute));
			if (defaultAttribute == null)
				return null;
			object defaultValue = defaultAttribute.ConstructorArguments[0].Value;
			return defaultValue == null ? "null" : defaultValue.ToString (); // Default (null) is encoded as null
		}
	}

	public class Symbols
	{
		public INamedTypeSymbol Attribute;
		public INamedTypeSymbol Object;
		public INamedTypeSymbol ValueType;
		public INamedTypeSymbol InjectAttribute;
		public INamedTypeSymbol WithAttribute;
		public INamedTypeSymbol SkipAttribute;
		public INamedTypeSymbol DefaultAttribute;
		public INamedTypeSymbol List;

		public Symbols (CSharpCompilation compilation)
		{
			Attribute = compilation.GetTypeByMetadataName (typeof (System.Attribute).FullName);
			Object = compilation.GetTypeByMetadataName (typeof (System.Object).FullName);
			ValueType = compilation.GetTypeByMetadataName (typeof (System.ValueType).FullName);
			InjectAttribute = compilation.GetTypeByMetadataName ("Inject");
			WithAttribute = compilation.GetTypeByMetadataName ("With");
			SkipAttribute = compilation.GetTypeByMetadataName ("Skip");
			DefaultAttribute = compilation.GetTypeByMetadataName ("Default");
			List = compilation.GetTypeByMetadataName ("System.Collections.Generic.List`1");
		}
	}
}
