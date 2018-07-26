using System;
using System.Linq;
using Model;

namespace Test
{
    public static class EntryPoint
    {
        public static void Main ()
        {
            var list = new NamedPointList (new Point [] { new Point (1, 1), new Point (2, 2) }, "MyList");
			Console.WriteLine ($"List {list.Name} has points of size ({string.Join (", ", list.Points.Select (p => p.Size))})!");
        }
    }
}
