using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Meadow.SolCodeGen.Test
{
    public static class Util
    {
        private static string _solutionDir;

        public static string FindSolutionDirectory()
        {
            if (_solutionDir != null)
            {
                return _solutionDir;
            }

            const string TEST_PROJ_DIR_NAME = "Meadow.SolCodeGen.Test";

            // Find the solution directory (start at this assembly directory and move up).
            var cwd = typeof(Integration).Assembly.Location;
            string checkDir = null;
            do
            {
                cwd = Path.GetDirectoryName(cwd);
                var children = Directory.GetDirectories(cwd, TEST_PROJ_DIR_NAME, SearchOption.TopDirectoryOnly);
                if (children.Any(p => p.EndsWith(TEST_PROJ_DIR_NAME, StringComparison.Ordinal)))
                {
                    checkDir = cwd;
                }
            }
            while (checkDir == null);

            return _solutionDir = checkDir;
        }

    }
}
