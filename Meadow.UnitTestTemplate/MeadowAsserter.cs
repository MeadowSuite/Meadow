using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate
{
    public class MeadowAsserter : Asserter
    {
        public Task ThrowsExceptionAsync<T>(Func<Task> action) where T : Exception
        {
            return Assert.ThrowsExceptionAsync<T>(action);
        }

        public T ThrowsException<T>(Action action) where T : Exception
        {
            return Assert.ThrowsException<T>(action);
        }
    }
}
