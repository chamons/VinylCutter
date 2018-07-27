using System;
using System.IO;
using System.Text;

namespace VinylCutter
{
	public class TempDirectory : IDisposable
	{
		public string Path { get; private set; }

		public TempDirectory ()
		{
			Path = FindFreeDirectory ();
			Directory.CreateDirectory (Path);
		}

		static string FindFreeDirectory ()
		{
			int attempt = 0;
			string suffix = "";
			while (true)
			{
				string path = System.IO.Path.Combine (System.IO.Path.GetTempPath (), "VinylCutter" + suffix);
				if (!Directory.Exists (path))
					return path;
				attempt++;
				suffix = "-" + attempt.ToString ();
			}
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
			{
				if (Directory.Exists (Path))
					Directory.Delete (Path, true);
			}
			Disposed = true;
		}
	}
}
