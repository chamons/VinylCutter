using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VinylCutter
{
	public class CodeGenerator
	{
		FileInfo File;

		public CodeGenerator (FileInfo file)
		{
			File = file;
		}

		public string Generate ()
		{
			CodeWriter writer = new CodeWriter ();
			GenerateUsings (writer);

			GenerateNamespaceHeader (writer);

			GenerateTopLevelInjects (writer);

			for (int i = 0 ; i < File.Records.Length ; ++i)
			{
				RecordInfo record = File.Records[i];
				GenerateRecord (record, writer);
				if (i != File.Records.Length - 1)
					writer.WriteLine ();
			}
			GenerateNamespaceFooter (writer);

			return writer.Generate ();
		}

		void GenerateTopLevelInjects (CodeWriter writer)
		{
			if (!string.IsNullOrEmpty (File.InjectCode))
			{
				writer.WriteLineIgnoringIndent (File.InjectCode);
				writer.WriteLine ();
			}
		}

		void GenerateNamespaceHeader (CodeWriter writer)
		{
			if (!string.IsNullOrEmpty (File.GlobalNamespace))
			{
				writer.WriteLine ($"namespace {File.GlobalNamespace}");
				writer.WriteLine ("{");
				writer.Indent ();
			}
		}

		void GenerateNamespaceFooter (CodeWriter writer)
		{
			if (!string.IsNullOrEmpty (File.GlobalNamespace)) 
			{
				writer.Dedent ();
				writer.WriteLine ("}");
			}
		}

		void GenerateUsings (CodeWriter writer)
		{
			if (File.Records.Any (x => x.Items.Any (y => y.CollectionType != CollectionType.None)))
			{
				writer.WriteLine ("using System;");
				writer.WriteLine ("using System.Collections.Generic;");
				writer.WriteLine ("using System.Collections.Immutable;");
				writer.WriteLine ();
			}
		}

		static void GenerateRecord (RecordInfo record, CodeWriter writer)
		{
			GenerateClassHeader (record, writer);
			writer.Indent ();

			foreach (var classItem in record.Items)
				GenerateProperty (classItem, writer);
			if (record.Items.Length > 0)
				writer.WriteLine ();
			GenerateConstructor (record, writer);
			GenerateWith (record, writer);

			writer.Dedent ();
			GenerateInjection (record, writer);

			GenerateClassFooter (writer);
		}

		static string CreateConstructorArgs (RecordInfo record)
		{
			StringBuilder builder = new StringBuilder ();
			bool first = true;
			for (int i = 0 ; i < record.Items.Length ; ++i)
			{
				ItemInfo classItem = record.Items[i];
				if (classItem.IsMutable)
					continue;

				if (first)
					first = false;
				else
					builder.Append (", ");
				
				string defaultValue = classItem.DefaultValue != null ? $" = {classItem.DefaultValue}" : "";
				builder.Append ($"{GetTypeName (classItem, true)} {classItem.Name.SmartLowerCase ()}{defaultValue}");
			}
			return builder.ToString ();
		}

		static string CreateConstructorInvokeArgs (RecordInfo record, int indexToNotCapitalize)
		{
			StringBuilder builder = new StringBuilder ();
			bool first = true;
			for (int i = 0 ; i < record.Items.Length ; ++i)
			{
				ItemInfo classItem = record.Items[i];
				if (classItem.IsMutable)
					continue;

				if (first)
					first = false;
				else
					builder.Append (", ");
				
				
				string classItemName = indexToNotCapitalize == i ? classItem.Name.SmartLowerCase () : classItem.Name;
				builder.Append (classItemName);
			}
			return builder.ToString ();
		}


		static string CreateMutableAssignmentBlock (RecordInfo record)
		{
			StringBuilder builder = new StringBuilder ();

			if (record.Items.Any (x => x.IsMutable))
			{
				builder.Append (" {");

				bool first = true;
				for (int i = 0 ; i < record.Items.Length ; ++i)
				{
					ItemInfo classItem = record.Items[i];
					if (!classItem.IsMutable)
						continue;

					if (first)
						first = false;
					else
						builder.Append (",");

					builder.Append ($" {classItem.Name} = this.{classItem.Name}");
				}
				builder.Append (" }");
			}

			return builder.ToString ();
		}

		static void GenerateConstructor (RecordInfo record, CodeWriter writer)
		{
			if (record.Items.Length == 0)
				return;

			writer.WriteLine ($"public {record.Name} ({CreateConstructorArgs (record)})");
			writer.WriteLine ("{");
			writer.Indent ();
			foreach (var classItem in record.Items.Where (x => !x.IsMutable))
				writer.WriteLine ($"{classItem.Name} = {GenerateFieldAssign (classItem)};");
			writer.Dedent ();
			writer.WriteLine ("}");
		}

		static string GenerateFieldAssign (ItemInfo item)
		{
			switch (item.CollectionType)
			{
				case CollectionType.List:
					return $"ImmutableArray.CreateRange ({item.Name.SmartLowerCase ()} ?? Array.Empty<{MakeFriendlyTypeName (item.TypeName)}> ())";
				case CollectionType.HashSet:
					return $"ImmutableHashSet.CreateRange ({item.Name.SmartLowerCase ()} ?? Array.Empty<{MakeFriendlyTypeName (item.TypeName)}> ())";
				case CollectionType.Dictionary:
					return $"{item.Name.SmartLowerCase ()}.ToImmutableDictionary ()";
				default:
					return item.Name.SmartLowerCase ();
			}
		}

		static void GenerateWith (RecordInfo record, CodeWriter writer)
		{
			if (record.Items.Length == 0)
				return;

			if (!(record.IncludeWith || record.Items.Any (x => x.IncludeWith)))
				return;

			for (int i = 0 ; i < record.Items.Length ; ++i)
			{
				if (record.Items[i].IsMutable)
					continue;

				if (!(record.IncludeWith || record.Items[i].IncludeWith))
					continue;

				writer.WriteLine ();
				ItemInfo classItem = record.Items[i];
				string itemTypeName = GetTypeName (classItem, true);
				writer.WriteLine ($"public {record.Name} With{classItem.Name} ({itemTypeName} {classItem.Name.SmartLowerCase ()})");
				writer.WriteLine ("{");
				writer.Indent ();

				writer.WriteLine ($"return new {record.Name} ({CreateConstructorInvokeArgs (record, i)}){CreateMutableAssignmentBlock (record)};");

				writer.Dedent ();
				writer.WriteLine ("}");
			}
		}

		static void GenerateProperty (ItemInfo item, CodeWriter writer)
		{
			if (item.IsMutable)
				writer.WriteLine ($"{GetTypeName (item, false)} {item.Name};");
			else
				writer.WriteLine ($"public {GetTypeName (item, false)} {item.Name} {{ get; }}");
		}

		static void GenerateClassHeader (RecordInfo record, CodeWriter writer)
		{
			// https://github.com/chamons/VinylCutter/issues/3		
			string visibility = record.Visibility == Visibility.Public ? "public " : "";
			string recordType = record.IsClass ? "class" : "struct";
			string baseTypes = record.BaseTypes != "" ? $" : {record.BaseTypes}" : "";
			writer.WriteLine ($"{visibility}partial {recordType} {record.Name}{baseTypes}");
			writer.WriteLine ("{");
		}
		
		static void GenerateClassFooter (CodeWriter writer)
		{
			writer.WriteLine ("}");
		}

		static void GenerateInjection (RecordInfo record, CodeWriter writer)
		{
			if (!string.IsNullOrEmpty (record.InjectCode)) 
			{
				writer.WriteLine ();
				writer.WriteLineIgnoringIndent (record.InjectCode);
			}
		}

		static string GetTypeName (ItemInfo item, bool isArg)
		{
			switch (item.CollectionType)
			{
				case CollectionType.List:
				{
					string arrayType = isArg ? "IEnumerable" : "ImmutableArray";
					return $"{arrayType}<{MakeFriendlyTypeName (item.TypeName)}>";
				}
				case CollectionType.HashSet:
				{
					string arrayType = isArg ? "IEnumerable" : "ImmutableHashSet";
					return $"{arrayType}<{MakeFriendlyTypeName (item.TypeName)}>";
				}
				case CollectionType.Dictionary:
					string dictType = isArg ? "IDictionary" : "ImmutableDictionary";
					string par = string.Join (", ", item.TypeName.Split (',').Select (x => MakeFriendlyTypeName (x)));
					return $"{dictType}<{par}>";
				default:
					return MakeFriendlyTypeName (item.TypeName);
			}
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
