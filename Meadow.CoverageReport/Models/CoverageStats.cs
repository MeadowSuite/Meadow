using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.CoverageReport.Models
{
    public class CoverageStats
    {
        public int LineCount { get; set; }
        public int LineCoveredCount { get; set; }
        public double LineCoveragePercent => GetPercent(LineCount, LineCoveredCount);

        public int BranchCount { get; set; }
        public int BranchCoveredCount { get; set; }
        public double BranchCoveragePercent => GetPercent(BranchCount, BranchCoveredCount);

        public int FunctionCount { get; set; }
        public int FunctionCoveredCount { get; set; }
        public double FunctionCoveragePercent => GetPercent(FunctionCount, FunctionCoveredCount);

        static double GetPercent(int total, int hit)
        {
            if (total == 0 && hit == 0)
            {
                return 100;
            }

            if (hit == 0)
            {
                return 0;
            }

            return Math.Round((hit / (double)total) * 100, 2);
        }
    }
}
