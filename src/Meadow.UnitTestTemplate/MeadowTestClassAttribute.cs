using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meadow.UnitTestTemplate
{
    public class MeadowTestClassAttribute : TestClassAttribute
    {
        public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
        {
            return new MeadowTestMethodAttribute();
        }

        public override bool IsDefaultAttribute()
        {
            return base.IsDefaultAttribute();
        }
    }
}
