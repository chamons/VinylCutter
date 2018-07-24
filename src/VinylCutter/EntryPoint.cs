using System;
using System.IO;

namespace VinylCutter
{
	class EntryPoint
	{
		public static void Main (string[] args)
		{
			if (args.Length != 1)
				Console.WriteLine ("mono VinylCutter.exe file");
			string text = File.ReadAllText (args[0]);
			Parser parser = new Parser (text);
			string output = new CodeGenerator (parser.Parse ()).Generate ();
			Console.WriteLine (output);
		}
	}
}
