using System;
using System.IO;
using System.Management.Automation;
using System.Threading;

namespace Meadow.Cli.Commands
{
    [Cmdlet(VerbsCommon.Watch, "Solidity")]
    [Alias("watchSol")]
    public class WatchSolidityCommand : PSCmdlet
    {

        static readonly object _syncRoot = new object();
        static WatcherInfo _watcher;

        class WatcherInfo
        {
            public FileSystemWatcher Watcher { get; set; }
            public CancellationTokenSource CancelToken { get; set; }
            public Thread WatcherThread { get; set; }
            public ManualResetEventSlim UpdateEvent { get; set; }
            public SessionState SessionState { get; set; }
        }

        protected override void EndProcessing()
        {
            var config = this.ReadConfig();
            string solSourceDir = Util.GetSolSourcePath(config, SessionState);

            if (!Directory.Exists(solSourceDir))
            {
                Console.WriteLine($"Creating source directory at '{solSourceDir}'");
                Directory.CreateDirectory(solSourceDir);
            }

            lock (_syncRoot)
            {
                if (_watcher != null)
                {
                    var current = Path.GetFullPath(_watcher.Watcher.Path);

                    if (current == solSourceDir)
                    {
                        Console.WriteLine($"Source directory already being watched: {solSourceDir}");
                    }
                    else
                    {
                        Console.WriteLine($"Updating source file watcher from '{current}' to '{solSourceDir}'");
                        _watcher.Watcher.Path = solSourceDir;
                    }
                }
                else
                {
                    Console.WriteLine($"Watching for file changes in source directory: {solSourceDir}");
                    _watcher = CreateWatcher(config, SessionState);
                }
            }

            base.EndProcessing();
        }

        static WatcherInfo CreateWatcher(Config config, SessionState sessionState)
        {
            string solSourceDir = Util.GetSolSourcePath(config, sessionState);
            var cancelToken = new CancellationTokenSource();
            var updateEvent = new ManualResetEventSlim();

            var watcher = new FileSystemWatcher(solSourceDir, "*.sol")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.CreationTime
            };

            watcher.Changed += (s, e) => updateEvent.Set();
            watcher.Created += (s, e) => updateEvent.Set();
            watcher.Deleted += (s, e) => updateEvent.Set();
            watcher.Renamed += (s, e) => updateEvent.Set();

            var watcherInfo = new WatcherInfo
            {
                CancelToken = cancelToken,
                UpdateEvent = updateEvent,
                Watcher = watcher,
                SessionState = sessionState
            };

            watcherInfo.WatcherThread = new Thread(BlockingWatchDirectory);
            watcherInfo.WatcherThread.Start(watcherInfo);

            return watcherInfo;
        }


        static void BlockingWatchDirectory(object threadParam)
        {
            var watcherInfo = (WatcherInfo)threadParam;
            var throttleWaiter = new ManualResetEventSlim();
            try
            {
                while (!watcherInfo.CancelToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        watcherInfo.UpdateEvent.Wait(watcherInfo.CancelToken.Token);
                        Console.WriteLine("Source directory update detected");

                        watcherInfo.UpdateEvent.Reset();
                        if (watcherInfo.CancelToken.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        throttleWaiter.Wait(TimeSpan.FromMilliseconds(350));
                        if (watcherInfo.UpdateEvent.IsSet)
                        {
                            Console.WriteLine("Throttled directory watcher");
                            continue;
                        }

                        watcherInfo.UpdateEvent.Reset();
                        CompileSolidityCommand.Run(watcherInfo.SessionState);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            finally
            {
                watcherInfo.Watcher.Dispose();
            }
        }
    }
}
