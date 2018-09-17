using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Models
{
    public static class LibVersion
    {
        public static readonly string Version;

        static LibVersion()
        {
            var asm = typeof(LibVersion).Assembly;
            var ver = asm.GetName().Version;
            Version = $"{ver.Major}.{ver.Minor}.{ver.Build}";
        }
    }
}
