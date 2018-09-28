using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Models
{
    public class SourceFileMap : CoverageStats
    {
        public string IndexHtmlFilePath { get; set; }
        public string SolidityCompilerVersion { get; set; }

        /// <summary>
        /// This file's index in the solc sources list output.
        /// </summary>
        public int SourceFileIndex { get; set; }

        public string SourceFilePath { get; set; }
        public string SourceFileName { get; set; }
        public string SourceFileDirectory { get; set; }
        public string SourceHashSha256 { get; set; }

        public SourceFileLine[] SourceFileLines { get; set; }

    }
}
