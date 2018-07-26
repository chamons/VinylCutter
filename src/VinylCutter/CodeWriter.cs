using System;
using System.Collections.Generic;
using System.Text;

namespace VinylCutter
{
	public class CodeWriter
	{
		StringBuilder Output;
		int IndentLevel = 0;

		public CodeWriter ()
		{
			Output = new StringBuilder ();
		}
		
		public void WriteLine ()
		{
			Output.Append ("\n");
		}
		
		public void WriteLine (string s)
		{
			Write (s + "\n");
		}

		public void WriteLineIgnoringIndent (string s)
		{
			WriteWithIndent (s + "\n", 0);
		}

		public void Write (string s)
		{
			WriteWithIndent (s, IndentLevel);
		}

		void WriteWithIndent (string s, int indent)
		{
			Output.Append ('\t', indent);
			Output.Append(s);
		}

		public string Generate ()
		{
			return Output.ToString ();
		}

		public void Indent ()
		{
			IndentLevel++;
		}

		public void Dedent ()
		{
			if (IndentLevel <= 0)
				throw new InvalidOperationException ($"Can not Dedent from level {IndentLevel}.");
			IndentLevel--;
		}
	}
}
