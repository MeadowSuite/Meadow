using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;

namespace Meadow.UnitTestTemplate
{
    public class MeadowAsserter : Asserter
    {
        public static readonly MeadowAsserter Instance = new MeadowAsserter();

        static readonly CollectionAsserter _collectionAsserter = new CollectionAsserter();
        public CollectionAsserter Collection => _collectionAsserter;

        public void StringContains(string value, string substring)
        {
            StringAssert.Contains(value, substring);
        }

        public void StringStartsWith(string value, string substring)
        {
            StringAssert.StartsWith(value, substring);
        }

        public void StringEndsWith(string value, string substring)
        {
            StringAssert.EndsWith(value, substring);
        }

        public void AreEqual(string expected, string actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
        }

        public void AreEqual(BigInteger expected, BigInteger actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public Task<T> ThrowsExceptionAsync<T>(Func<Task> action) where T : Exception
        {
            return Assert.ThrowsExceptionAsync<T>(action);
        }

        public Task<T> ThrowsExceptionAsync<T>(Func<Task> action, string message) where T : Exception
        {
            return Assert.ThrowsExceptionAsync<T>(action, message);
        }

        public Task<T> ThrowsExceptionAsync<T>(Func<Task> action, string message, params object[] parameters) where T : Exception
        {
            return Assert.ThrowsExceptionAsync<T>(action, message, parameters);
        }

        public T ThrowsException<T>(Func<object> action) where T : Exception
        {
            return Assert.ThrowsException<T>(action);
        }

        public T ThrowsException<T>(Func<object> action, string message) where T : Exception
        {
            return Assert.ThrowsException<T>(action, message);
        }

        public T ThrowsException<T>(Func<object> action, string message, params object[] parameters) where T : Exception
        {
            return Assert.ThrowsException<T>(action, message, parameters);
        }

        public T ThrowsException<T>(Action action) where T : Exception
        {
            return Assert.ThrowsException<T>(action);
        }

        public T ThrowsException<T>(Action action, string message) where T : Exception
        {
            return Assert.ThrowsException<T>(action, message);
        }

        public T ThrowsException<T>(Action action, string message, params object[] parameters) where T : Exception
        {
            return Assert.ThrowsException<T>(action, message, parameters);
        }

        public string ReplaceNullChars(string input)
        {
            return Assert.ReplaceNullChars(input);
        }


#if CHECK_CONSISTENCY
        
        // Ensure this Assert class instance has all the methods that the static Assert class has.

        static MeadowAsserter()
        {
            var originalMethods = typeof(Assert)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.ToString());

            var thisMethods = typeof(MeadowAsserter)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Select(m => m.ToString());

            foreach (var method in originalMethods)
            {
                var match = thisMethods.Contains(method);
                if (!match)
                {
                    throw new Exception();
                }
            }

        }
#endif

    }
}
