using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.UnitTestTemplate
{
    /// <summary>
    /// Provides information regarding the state of a test, as needed by this test framework.
    /// </summary>
    internal class InternalTestState
    {
        #region Properties
        /// <summary>
        /// Indicates if tests are working on/should be working on an external node.
        /// </summary>
        internal bool InExternalNodeContext { get; set; }

        /// <summary>
        /// Indicates the test start time.
        /// </summary>
        internal DateTimeOffset StartTime { get; set; }
        /// <summary>
        /// Indicates the test end time.
        /// </summary>
        internal DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// Indicates whether test initialization occurred without error.
        /// </summary>
        internal bool InitializationSuccess { get; set; }
        /// <summary>
        /// Indicates whether test cleanup occurred without error.
        /// </summary>
        internal bool CleanupSuccess { get; set; }
        #endregion
    }
}
