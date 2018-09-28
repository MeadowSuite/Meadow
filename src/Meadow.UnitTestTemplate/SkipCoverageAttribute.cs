using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meadow.UnitTestTemplate
{
    /// <summary>
    /// Prevents the post-test coverage data analysis.
    /// Useful for tests that create broken or unique contract deployments
    /// where the coverage data will fail to match to the known contracts.
    /// </summary>
    public class SkipCoverageAttribute : TestPropertyAttribute
    {
        public SkipCoverageAttribute()
            : base(nameof(SkipCoverageAttribute), string.Empty)
        {

        }
    }

}
