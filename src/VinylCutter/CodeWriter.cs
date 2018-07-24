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
			Write ("\n");
		}
		
		public void WriteLine (string s)
		{
			Write (s + "\n");
		}

		public void Write (string s)
		{
			Output.Append ('\t', IndentLevel);
			Output.Append (s);
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
