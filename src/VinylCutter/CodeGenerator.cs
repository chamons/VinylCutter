using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VinylCutter
{
	public class CodeGenerator
	{
		List <ParseInfo> ParseInfos;

		public CodeGenerator (IEnumerable <ParseInfo> parseInfos)
		{
			ParseInfos = parseInfos.ToList ();
		}

		public string Generate ()
		{
			CodeWriter writer = new CodeWriter ();
			GenerateUsings (writer);
			for (int i = 0 ; i < ParseInfos.Count ; ++i)
			{
				ParseInfo parseInfo = ParseInfos[i];
				GenerateFromInfo (parseInfo, writer);
				if (i != ParseInfos.Count - 1)
					writer.WriteLine ();
			}

			return writer.Generate ();
		}

		void GenerateUsings (CodeWriter writer)
		{
			if (ParseInfos.Any (x => x.Items.Any (y => y.IsCollection)))
			{
				writer.WriteLine ("using System.Collections.Immutable;");
				writer.WriteLine ();
			}
		}

		static void GenerateFromInfo (ParseInfo parseInfo, CodeWriter writer)
		{
			GenerateClassHeader (parseInfo, writer);
			writer.Indent ();

			foreach (var classItem in parseInfo.Items)
				GenerateProperty (classItem, writer);
			if (parseInfo.Items.Length > 0)
				writer.WriteLine ();
			GenerateConstructor (parseInfo, writer);
			GenerateWith (parseInfo, writer);

			writer.Dedent ();
			GenerateClassFooter (writer);
		}

		static string CreateConstructorArgs (ParseInfo parseInfo)
		{
			StringBuilder builder = new StringBuilder ();
			for (int i = 0 ; i < parseInfo.Items.Length ; ++i)
			{
				ClassItem classItem = parseInfo.Items[i];
				builder.Append ($"{GetTypeName (classItem)} {classItem.Name.CamelPrefix ()}");
				if (i != parseInfo.Items.Length - 1)
					builder.Append (", ");
			}
			return builder.ToString ();
		}
		
		static string CreateConstructorInvokeArgs (ParseInfo parseInfo, int indexToNotCapitalize = -1)
		{
			StringBuilder builder = new StringBuilder ();
			for (int i = 0 ; i < parseInfo.Items.Length ; ++i)
			{
				ClassItem classItem = parseInfo.Items[i];
				string classItemName = indexToNotCapitalize == i ? classItem.Name.CamelPrefix () : classItem.Name;
				builder.Append (classItemName);
				if (i != parseInfo.Items.Length - 1)
					builder.Append (", ");
			}
			return builder.ToString ();
		}


		static void GenerateConstructor (ParseInfo parseInfo, CodeWriter writer)
		{
			if (parseInfo.Items.Length == 0)
				return;

			writer.WriteLine ($"public {parseInfo.Name} ({CreateConstructorArgs (parseInfo)})");
			writer.WriteLine ("{");
			writer.Indent ();
			foreach (var classItem in parseInfo.Items)
				writer.WriteLine ($"{classItem.Name} = {classItem.Name.CamelPrefix ()};");
			writer.Dedent ();
			writer.WriteLine ("}");
		}

		static void GenerateWith (ParseInfo parseInfo, CodeWriter writer)
		{
			if (parseInfo.Items.Length == 0)
				return;

			if (!parseInfo.IncludeWith && !parseInfo.Items.Any (x => x.ForcedIncludeWith))
				return;

			writer.WriteLine ();
			for (int i = 0 ; i < parseInfo.Items.Length ; ++i)
			{
				if (!(parseInfo.IncludeWith || parseInfo.Items[i].ForcedIncludeWith))
					continue;

				ClassItem classItem = parseInfo.Items[i];
				string itemTypeName = GetTypeName (classItem);
				writer.WriteLine ($"public {parseInfo.Name} With{classItem.Name} ({itemTypeName} {classItem.Name.CamelPrefix ()})");
				writer.WriteLine ("{");
				writer.Indent ();

				writer.WriteLine ($"return new {parseInfo.Name} ({CreateConstructorInvokeArgs (parseInfo, i)});");

				writer.Dedent ();
				writer.WriteLine ("}");
				if (i != parseInfo.Items.Length - 1)
					writer.WriteLine ();
			}
		}

		static void GenerateProperty (ClassItem item, CodeWriter writer)
		{
			writer.WriteLine ($"public {GetTypeName (item)} {item.Name} {{ get; }}");
		}

		static void GenerateClassHeader (ParseInfo parseInfo, CodeWriter writer)
		{
			// https://github.com/chamons/VinylCutter/issues/3		
			string visibility = parseInfo.Visibility == Visibility.Public ? "public " : "";
			string recordType = parseInfo.IsClass ? "class" : "struct";
			writer.WriteLine ($"{visibility}partial {recordType} {parseInfo.Name}");
			writer.WriteLine ("{");
		}
		
		static void GenerateClassFooter (CodeWriter writer)
		{
			writer.WriteLine ("}");
		}

		static string GetTypeName (ClassItem item)
		{
			if (item.IsCollection)
				return $"ImmutableList<{MakeFriendlyTypeName (item.TypeName)}>";
			return MakeFriendlyTypeName (item.TypeName);
		}

		static string MakeFriendlyTypeName (string typeName)
		{
			switch (typeName)
			{
				case "Boolean":
					return "bool";
				case "Byte":
					return "byte";
				case "SByte":
					return "sbyte";
				case "Char":
					return "char";
				case "String":
					return "string";
				case "Int16":
					return "short";
				case "Int32":
					return "int";
				case "Int64":
					return "long";
				case "UInt16":
					return "ushort";
				case "UInt32":
					return "uint";
				case "UInt64":
					return "ulong";
				case "Single":
					return "float";
				case "Double":
					return "double";
				default:
					return typeName;
			}
		}
	}
}
