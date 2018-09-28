using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Meadow.SolCodeGen
{
    static class Util
    {
        public static string GetRelativeFilePath(string solSourceDir, string absolutePath)
        {
            if (absolutePath.StartsWith(solSourceDir, StringComparison.OrdinalIgnoreCase))
            {
                absolutePath = absolutePath.Substring(solSourceDir.Length).TrimStart(new[] { '\\', '/' });
            }
            else if (Path.IsPathRooted(absolutePath))
            {
                throw new Exception($"Unexpected source file path from solc output: {absolutePath}, source dir: {solSourceDir}");
            }

            return absolutePath;
        }
    }
}
