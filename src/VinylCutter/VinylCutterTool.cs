using System;
using System.IO;
using System.Collections.Generic;

namespace VinylCutter
{
	class InputStream
	{
		public string FileName;
		public string Text;

		public InputStream (string fileName, string text)
		{
			FileName = fileName;
			Text = text;
		}
	}

	public class VinylCutterTool
	{
		public bool ReadFromStandardIn { get; set; }
		public bool WriteToStandardOut { get; set; }

		public string OutputDirectory { get; set; } = System.IO.Directory.GetCurrentDirectory ();
		public string FileExtension { get; set; } = "g.cs";

		public List<string> Files;

		public bool ValidateOptions (List<string> files)
		{
			Files = files;
			if (!ReadFromStandardIn && files.Count == 0)
				return false;
			return true;
		}


		public void Run ()
		{ 
			foreach (InputStream stream in GetInput ())
			{
				string output = ParseOneFile (stream.Text);
				OutputOneFile (stream.FileName, output);

			}
		}

		void OutputOneFile (string inputFileName, string output)
		{
			if (WriteToStandardOut)
			{
				Console.WriteLine (output);
			}
			else
			{
				if (inputFileName == null)
					inputFileName = "code.cs";

				string fileNameNoExtension = Path.GetFileNameWithoutExtension (inputFileName);
				string newFileNane = fileNameNoExtension + "." + FileExtension;
				string outputPath = Path.Combine (OutputDirectory, newFileNane);
				File.WriteAllText (outputPath, output);
			}
		}

		string ParseOneFile (string text)
		{
			var parser = new Parser (text);
			FileInfo file = parser.Parse ();
			var codeGenerator = new CodeGenerator (file);
			return codeGenerator.Generate ();
		}

		IEnumerable<InputStream> GetInput ()
		{
			if (ReadFromStandardIn)
			{
				yield return new InputStream (null, Console.In.ReadToEnd ());
			}
			else
			{
				foreach (var file in Files)
					yield return new InputStream (file, File.ReadAllText (file));
			}
		}
	}
}
