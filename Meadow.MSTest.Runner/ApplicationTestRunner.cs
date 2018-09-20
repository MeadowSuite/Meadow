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
using System.Xml;

namespace Meadow.MSTest.Runner
{
    public class ApplicationTestRunner
    {
        string[] _assemblies;

        public ApplicationTestRunner(params Assembly[] assemblies)
        {
            _assemblies = assemblies.Select(a => a.Location).ToArray();
        }

        public ApplicationTestRunner(params string[] assemblies)
        {
            _assemblies = assemblies;
        }

        public static void RunAllTests(Assembly scanAssembly = null)
        {
            var assemblies = new HashSet<Assembly>();
            if (scanAssembly != null)
            {
                assemblies.Add(scanAssembly);
            }

            assemblies.Add(Assembly.GetEntryAssembly());
            assemblies.Add(Assembly.GetCallingAssembly());

            var applicationTestRunner = new ApplicationTestRunner(assemblies.ToArray());
            applicationTestRunner.RunTests();
        }

        public void RunTests()
        {
            var runContext = new RunContext();
            var frameworkHandler = new MyFrameworkHandle(GetConsoleLogger());

            const string MSTEST_ADAPTER_DLL = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.dll";
            const string MSTEST_EXECUTOR_TYPE = "Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.MSTestExecutor";

            var cwd = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var msTestAdapterAssembly = Assembly.LoadFrom(Path.Combine(cwd, MSTEST_ADAPTER_DLL));
            var testExecutorType = msTestAdapterAssembly.GetType(MSTEST_EXECUTOR_TYPE, throwOnError: true);

            dynamic testExecutor = Activator.CreateInstance(testExecutorType);
            testExecutor.RunTests(_assemblies, runContext, frameworkHandler);

            //var tDisc = new MSTestDiscoverer();
            //var eng = new TestEngine();
            //var e = new ExecutionManager(new MyRequestData());
            //e.Initialize(Array.Empty<string>());
            //e.StartTestRun(new TestExecutionContext())
            //var testExecutionManager = new TestExecutionManager();
            //testExecutionManager.RunTests(assemblyPaths, runContext, frameworkHandler, new TestRunCancellationToken());
        }

        Action<string> GetConsoleLogger()
        {
            var output = Console.OpenStandardOutput();
            var sw = new StreamWriter(output);
            return msg =>
            {
                sw.WriteLine(msg);
                sw.Flush();
                Debug.WriteLine(msg);
                //Trace.WriteLine(msg);
            };
        }

        class MyRunContext : IRunContext
        {
            readonly RunContext _default = new RunContext();

            public bool KeepAlive => _default.KeepAlive;

            public bool InIsolation => _default.InIsolation;

            public bool IsDataCollectionEnabled => _default.IsDataCollectionEnabled;

            public bool IsBeingDebugged => _default.IsBeingDebugged;

            public string TestRunDirectory => _default.TestRunDirectory;

            public string SolutionDirectory => _default.SolutionDirectory;

            public IRunSettings RunSettings => _default.RunSettings;

            public ITestCaseFilterExpression GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
            {
                return ((IRunContext)_default).GetTestCaseFilter(supportedProperties, propertyProvider);
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
