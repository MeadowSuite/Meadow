using System;

namespace Meadow.CoverageReport
{
    public class UnitTestResult
    {
        public string Namespace { get; set; }
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
