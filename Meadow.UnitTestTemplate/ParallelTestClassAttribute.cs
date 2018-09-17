using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Meadow.UnitTestTemplate
{
    public class ParallelTestClassAttribute : TestClassAttribute
    {
        public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
        {
            // If the method attribute is already a parallel test one, we can simply return it.
            if (testMethodAttribute is ParallelTestMethodAttribute)
            {
                return testMethodAttribute;
            }

            // Otherwise we create a parallel test method attribute out of the regular one.
            return new ParallelTestMethodAttribute(base.GetTestMethodAttribute(testMethodAttribute));
        }
    }
}
