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
			Path = System.IO.Path.Combine (System.IO.Path.GetTempPath (), "VinylCutter");
			Directory.CreateDirectory (Path);
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
