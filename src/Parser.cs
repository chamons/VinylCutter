using System;
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
public class Default : System.Attribute { public Default (string value) {} }

[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
public class Mutable : System.Attribute { }
";

		List<RecordInfo> Records;
		Symbols Symbols;
		SemanticModel Model;
		SourceText SourceText;
		string FirstNamespace;

		public Parser (string text)
		{
			Text = text;
		}

		public FileInfo Parse ()
		{
			Records = new List<RecordInfo>();
			FirstNamespace = "";

			SyntaxTree tree = CSharpSyntaxTree.ParseText (Prelude + Text);
			CSharpCompilation compilation = Compile (tree);
			Model = compilation.GetSemanticModel (tree);
			SourceText = Model.SyntaxTree.GetText ();
			Symbols = new Symbols (compilation);

			var root = (CompilationUnitSyntax)tree.GetRoot ();
			foreach (var c in root.DescendantNodes ().OfType<ClassDeclarationSyntax> ())
				HandlePossibleRecord (Model.GetDeclaredSymbol (c));

			foreach (var s in root.DescendantNodes ().OfType<StructDeclarationSyntax> ())
				HandlePossibleRecord (Model.GetDeclaredSymbol (s));

			string injectCode = FindTopLevelInjectItems (root);

			return new FileInfo (Records, injectCode, FirstNamespace);
		}

		static CSharpCompilation Compile (SyntaxTree tree)
		{
			var root = (CompilationUnitSyntax)tree.GetRoot ();
			var mscorlib = MetadataReference.CreateFromFile (typeof (object).Assembly.Location);
			var compilation = CSharpCompilation.Create ("Vinyl").AddReferences (mscorlib).AddSyntaxTrees (tree).WithOptions (new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary));

			var compilerDiagnostics = compilation.GetDiagnostics ();
			var compilerErrors = compilerDiagnostics.Where (i => i.Severity == DiagnosticSeverity.Error);
			if (compilerErrors.Count () > 0)
				throw new ParseCompileError (string.Join ("\n", compilerErrors.Select (x => x.ToString ())));

			return compilation;
		}

		string FindTopLevelInjectItems (CompilationUnitSyntax root)
		{
			StringBuilder builder = new StringBuilder ();

			foreach (var s in root.DescendantNodes (descendIntoChildren: n => n == root || n is NamespaceDeclarationSyntax))
			{
				ISymbol symbol = Model.GetDeclaredSymbol (s);
				if (symbol != null && HasInject (symbol))
					builder.Append (GetSourceCode (symbol));
			}
			return builder.ToString ();
		}

		void HandlePossibleRecord (INamedTypeSymbol symbol)
		{
			if (symbol.IsAbstract || symbol.EnumUnderlyingType != null)
				return;

			if (symbol.BaseType.Equals (Symbols.Attribute))
				return;

			if (symbol.GetAttributes ().Any (x => x.AttributeClass.Equals (Symbols.SkipAttribute)))
				return;

			if (FirstNamespace == "" && symbol.ContainingNamespace != null)
				FirstNamespace = symbol.ContainingNamespace.IsGlobalNamespace ? "" : symbol.ContainingNamespace.ToString ();

			List<ItemInfo> items = new List<ItemInfo> ();

			foreach (var member in symbol.GetMembers ().OfType<IPropertySymbol>().Where (x => !IsInternalConstruct (x) && !HasInject (x))) 
				items.Add (CreateClassItem (member, member.Type));

			foreach (var field in symbol.GetMembers ().OfType<IFieldSymbol>().Where (x => !IsInternalConstruct(x) && !HasInject (x)))
				items.Add (CreateClassItem (field, field.Type));

			string baseType = GetBaseTypes (symbol);
			string injectCode = FindInjectionCodeOnMembers (symbol);

			Visibility visibility = symbol.DeclaredAccessibility == Accessibility.Public ? Visibility.Public : Visibility.Private;
			Records.Add (new RecordInfo (symbol.Name, symbol.IsReferenceType, visibility, includeWith : HasWith (symbol), items: items, baseTypes: baseType, injectCode: injectCode));
		}

		string GetBaseTypes (INamedTypeSymbol symbol)
		{
			List<string> baseTypes = new List<string> ();
			string baseType = GetBaseType (symbol);
			if (baseType != null)
				baseTypes.Add (baseType);
			baseTypes.AddRange (symbol.Interfaces.Select (x => x.Name));
			return string.Join (", ", baseTypes);
		}

		string GetBaseType (INamedTypeSymbol symbol)
		{
			if (symbol.IsValueType)
				return symbol.BaseType.Equals (Symbols.ValueType) ? null : symbol.BaseType.Name;
			else
				return symbol.BaseType.Equals (Symbols.Object) ? null : symbol.BaseType.Name;
		}

		string FindInjectionCodeOnMembers (INamedTypeSymbol symbol)
		{
			StringBuilder builder = new StringBuilder ();

			foreach (var injectItem in symbol.GetMembers ().Where (x => HasInject (x)))
				builder.Append (GetSourceCode (injectItem));

			return builder.ToString ();
		}

		string GetSourceCode (ISymbol injectItem)
		{
			if (injectItem.DeclaringSyntaxReferences.Length > 1)
				throw new InvalidOperationException ("Inject split over multiple DeclaringSyntaxReferences?");

			var lines = SourceText.GetSubText (injectItem.DeclaringSyntaxReferences[0].Span).Lines;
			return string.Join ("\n", lines.Skip (1));
		}

		ItemInfo CreateClassItem (ISymbol symbol, ITypeSymbol type)
		{
			string defaultValue = GetDefaultValue (symbol);
			bool mutableValue = GetMutable (symbol);
			if (type.OriginalDefinition.Equals (Symbols.List))
			{
				INamedTypeSymbol t = (INamedTypeSymbol)type;
				if (mutableValue) // Mutable Items are not converted to immutable collections if they are lists
					return new ItemInfo (symbol.Name, $"List <{t.TypeArguments[0].Name}>", false, HasWith (symbol), defaultValue, true);
				else
					return new ItemInfo (symbol.Name, t.TypeArguments[0].Name, true, HasWith (symbol), defaultValue);
			}
			return new ItemInfo (symbol.Name, type.Name, false, HasWith (symbol), defaultValue, mutableValue);
		}
	
		string GetDefaultValue (ISymbol symbol)
		{
			AttributeData defaultAttribute = symbol.GetAttributes ().FirstOrDefault (x => x.AttributeClass.Equals (Symbols.DefaultAttribute));
			if (defaultAttribute == null)
				return null;

			string defaultValue = (string)defaultAttribute.ConstructorArguments[0].Value;
			// Special case [Default ("")] to two double quotes, not empty string
			// Escaping a common case is not fun
			if (defaultValue == "")
				return "\"\"";
			return defaultValue;
		}

		bool GetMutable (ISymbol symbol)
		{
			return symbol.GetAttributes ().Any (x => x.AttributeClass.Equals (Symbols.MutableAttribute));
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
		public INamedTypeSymbol MutableAttribute;
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
			MutableAttribute = compilation.GetTypeByMetadataName ("Mutable");
			List = compilation.GetTypeByMetadataName ("System.Collections.Generic.List`1");
		}
	}
}
