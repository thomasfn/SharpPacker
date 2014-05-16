using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpPackerTests
{
    public static class Program
    {
        public static void Main()
        {
            PackFileTests tests = new PackFileTests();
            tests.SimpleWriteRead();
            tests.AddRemove();
            tests.Move();
            tests.UpdateFile();

        }

    }
}
