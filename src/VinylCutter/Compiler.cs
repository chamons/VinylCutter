using System;
using System.IO;
using System.Text;

namespace VinylCutter
{
	public class ParseCompileError : Exception
	{
		public string ErrorText;

		public ParseCompileError (string errorText)
		{
			ErrorText = errorText;
		}

		public override string ToString () => $"ParseCompileError -  {ErrorText}";
	}

	public class Compiler : IDisposable
	{
		const string CSC = "/Library/Frameworks/Mono.framework/Versions/Current/Commands/csc";

		public RunCommandDelegate RunCommand { get; set; } = ExternalCommand.RunCommand; 

		TempDirectory Directory;
		string CodePath => Path.Combine (Directory.Path, "code.cs");
		string AssemblyPath => Path.Combine (Directory.Path, "assembly.dll");

		public Compiler (string text)
		{
			Directory = new TempDirectory ();
			File.WriteAllText (CodePath, text);
		}

		public string Compile ()
		{
			StringBuilder output = new StringBuilder ();
			int ret = RunCommand (CSC, $"/t:library {CodePath} /out:{AssemblyPath}", output: output, suppressPrintOnErrors : true);
			if (ret != 0)
				throw new ParseCompileError (output.ToString ());
			return AssemblyPath;
		}
		
		bool Disposed = false;

		public void Dispose()
		{ 
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (Disposed)
				return; 

			if (disposing)
				Directory.Dispose ();
			Disposed = true;
		}

	}
}
