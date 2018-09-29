using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace Meadow.MSTest.Runner
{
    public class ApplicationTestRunner
    {
        string[] _assemblies;
        (string FullyQualifiedTestName, string SourceAssembly)[] _testCases;

        private ApplicationTestRunner()
        {

        }

        public static ApplicationTestRunner CreateFromAssemblies(params Assembly[] assemblies)
        {
            var runner = new ApplicationTestRunner();
            runner._assemblies = assemblies.Select(a => a.Location).ToArray();
            return runner;
        }

        public static ApplicationTestRunner CreateFromAssemblies(params string[] assemblies)
        {
            var runner = new ApplicationTestRunner();
            runner._assemblies = assemblies;
            return runner;
        }

        public static ApplicationTestRunner CreateFromSpecificTests(params (string FullyQualifiedTestName, string SourceAssembly)[] testCases)
        {
            var runner = new ApplicationTestRunner();
            runner._assemblies = testCases.Select(t => t.SourceAssembly).Distinct().ToArray();
            runner._testCases = testCases;
            return runner;
        }

        public static void RunAllTests(Assembly scanAssembly = null, CancellationToken cancellationToken = default)
        {
            var assemblies = new HashSet<Assembly>();
            if (scanAssembly != null)
            {
                assemblies.Add(scanAssembly);
            }

            assemblies.Add(Assembly.GetEntryAssembly());
            assemblies.Add(Assembly.GetCallingAssembly());

            var applicationTestRunner = CreateFromAssemblies(assemblies.ToArray());
            applicationTestRunner.RunTests(cancellationToken);
        }

        public static void RunSpecificTests(Assembly assembly, params string[] fullyQualifiedTestNames)
        {
            RunSpecificTests(new[] { Assembly.GetEntryAssembly(), Assembly.GetCallingAssembly(), assembly }, fullyQualifiedTestNames);
        }

        public static void RunSpecificTests(params string[] fullyQualifiedTestNames)
        {
            RunSpecificTests(new[] { Assembly.GetEntryAssembly(), Assembly.GetCallingAssembly() }, fullyQualifiedTestNames);
        }

        static void RunSpecificTests(Assembly[] assemblies, string[] fullyQualifiedTestNames)
        {
            var assemblyLocations = assemblies.Select(a => a.Location).Distinct();
            var testCases = new List<(string FullyQualifiedTestName, string SourceAssembly)>();
            foreach (var assembly in assemblyLocations)
            {
                foreach (var testName in fullyQualifiedTestNames)
                {
                    testCases.Add((testName, assembly));
                }
            }

            var runner = CreateFromSpecificTests(testCases.ToArray());
            runner.RunTests();
        }

        public void RunTests(CancellationToken cancellationToken = default)
        {
            var runContext = new MyRunContext(_testCases);
            var frameworkHandler = new MyFrameworkHandle(GetConsoleLogger());

            const string MSTEST_ADAPTER_DLL = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll";
            const string MSTEST_EXECUTOR_TYPE = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.MSTestExecutor";

            string msTestAdapterAssemblyPath = null;
            bool foundFile = false;

            foreach (var assemblyDir in GetPossibleAssemblyDirectories())
            {
                msTestAdapterAssemblyPath = Path.Combine(assemblyDir, MSTEST_ADAPTER_DLL);
                if (File.Exists(msTestAdapterAssemblyPath))
                {
                    foundFile = true;
                    break;
                }
            }

            if (!foundFile)
            {
                throw new Exception($"Could not find {MSTEST_ADAPTER_DLL}");
            }

            var msTestAdapterAssembly = Assembly.LoadFrom(msTestAdapterAssemblyPath);
            var testExecutorType = msTestAdapterAssembly.GetType(MSTEST_EXECUTOR_TYPE, throwOnError: true);

            dynamic testExecutor = Activator.CreateInstance(testExecutorType);

            cancellationToken.Register(() =>
            {
                testExecutor.Cancel();
            });

            testExecutor.RunTests(_assemblies, runContext, frameworkHandler);

            //var tDisc = new MSTestDiscoverer();
            //var eng = new TestEngine();
            //var e = new ExecutionManager(new MyRequestData());
            //e.Initialize(Array.Empty<string>());
            //e.StartTestRun(new TestExecutionContext())
            //var testExecutionManager = new TestExecutionManager();
            //testExecutionManager.RunTests(assemblyPaths, runContext, frameworkHandler, new TestRunCancellationToken());
        }

        IEnumerable<string> GetPossibleAssemblyDirectories()
        {
            yield return Directory.GetCurrentDirectory();
            yield return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            yield return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        Action<string> GetConsoleLogger()
        {
            //var output = Console.OpenStandardOutput();
            //var sw = new StreamWriter(output);
            return msg =>
            {
                return;
                //sw.WriteLine(msg);
                //sw.Flush();
                //Debug.WriteLine(msg);
                //Trace.WriteLine(msg);
            };
        }

        class MyRunContext : IRunContext
        {
            readonly RunContext _default = new RunContext();
            readonly MyTestCaseFilterExpression _testFilter;

            public MyRunContext((string FullyQualifiedTestName, string SourceAssembly)[] testCases)
            {
                _testFilter = new MyTestCaseFilterExpression(testCases);
            }

            public bool KeepAlive => _default.KeepAlive;

            public bool InIsolation => _default.InIsolation;

            public bool IsDataCollectionEnabled => _default.IsDataCollectionEnabled;

            public bool IsBeingDebugged => _default.IsBeingDebugged;

            public string TestRunDirectory => _default.TestRunDirectory;

            public string SolutionDirectory => _default.SolutionDirectory;

            public IRunSettings RunSettings => _default.RunSettings;

            public ITestCaseFilterExpression GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
            {
                return _testFilter;
            }
        }

        class MyTestCaseFilterExpression : ITestCaseFilterExpression
        {
            readonly (string FullyQualifiedTestName, string SourceAssembly)[] _testCases;

            public MyTestCaseFilterExpression((string FullyQualifiedTestName, string SourceAssembly)[] testCases)
            {
                _testCases = testCases;
            }

            public string TestCaseFilterValue
            {
                get
                {
                    return null;
                }
            }

            public bool MatchTestCase(TestCase testCase, Func<string, object> propertyValueProvider)
            {
                if (_testCases == null)
                {
                    return true;
                }

                if (_testCases.Any(t => t.FullyQualifiedTestName == testCase.FullyQualifiedName && t.SourceAssembly == testCase.Source))
                {
                    return true;
                }

                return false;
            }
        }

        class MyFrameworkHandle : IFrameworkHandle
        {
            public bool EnableShutdownAfterTestRun { get; set; } = true;

            readonly Action<string> _logger;

            public MyFrameworkHandle(Action<string> logger)
            {
                _logger = logger;
            }

            public int LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables)
            {
                throw new NotImplementedException();
            }

            public void RecordAttachments(IList<AttachmentSet> attachmentSets)
            {
            }

            public void RecordEnd(TestCase testCase, TestOutcome outcome)
            {
            }

            public void RecordResult(TestResult testResult)
            {
                _logger?.Invoke($"{testResult.DisplayName} - {testResult.Outcome}");
            }

            public void RecordStart(TestCase testCase)
            {
            }

            public void SendMessage(TestMessageLevel testMessageLevel, string message)
            {
            }
        }

    }
}
