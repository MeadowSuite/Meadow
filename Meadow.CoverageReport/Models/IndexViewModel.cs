using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Meadow.CoverageReport.Models
{
    public class IndexViewModel : CoverageStats
    {
        public string SolidityCompilerVersion { get; set; }

        public SourceFileMap[] SourceFileMaps { get; set; }

        public IGrouping<string, UnitTestResult>[] UnitTestOutcome { get; set; }

    }
}
