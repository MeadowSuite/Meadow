using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meadow.UnitTestTemplate
{
    // TODO: Features to add test description, notes that can point to contract, function, and line number.
    //       The test report generator will use this info for the report.
    public class TestAttribute : Attribute, ITestMethod
    {
        public string TestMethodName => throw new NotImplementedException();

        public string TestClassName => throw new NotImplementedException();

        public Type ReturnType => throw new NotImplementedException();

        public object[] Arguments => throw new NotImplementedException();

        public ParameterInfo[] ParameterTypes => throw new NotImplementedException();

        public MethodInfo MethodInfo => throw new NotImplementedException();

        public Attribute[] GetAllAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public TAttributeType[] GetAttributes<TAttributeType>(bool inherit) where TAttributeType : Attribute
        {
            throw new NotImplementedException();
        }

        public TestResult Invoke(object[] arguments)
        {
            throw new NotImplementedException();
        }
    }
}
