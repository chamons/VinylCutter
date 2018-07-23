using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace VinylCutter
{
	public class ParseInfo
	{
		public string Name;
		public bool IsClass;

		public ParseInfo (string name, bool isClass)
		{
			Name = name;
			IsClass = isClass;
		}

		public static ParseInfo Create (TypeDefinition type)
		{
			return new ParseInfo (type.Name, type.IsClass);
		}
	}

	public class Parser
	{
		string Text;

		public Parser (string text)
		{
			Text = text;
		}

		public List<ParseInfo> Parse ()
		{
			var infos = new List<ParseInfo> ();
			using (Compiler compiler = new Compiler (Text))
			{
				string assemblyPath = compiler.Compile ();
				var module = ModuleDefinition.ReadModule (assemblyPath);
				foreach (TypeDefinition type in module.Types.Where (x => x.IsPublic))
				{
					infos.Add (ParseInfo.Create (type));
				}
			}
			return infos;
		}
	}
}
