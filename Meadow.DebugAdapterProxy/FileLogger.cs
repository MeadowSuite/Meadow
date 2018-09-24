using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace Meadow.DebugAdapterProxy
{
    class FileLogger
    {
        readonly string _logFilePath;
        readonly BlockingCollection<string> _logMessageQueue;
        readonly Thread _logWriterThread;

        static readonly UTF8Encoding UTF8 = new UTF8Encoding(false, false);

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;

            _logMessageQueue = new BlockingCollection<string>();
            _logWriterThread = new Thread(new ThreadStart(() => LogWriterLoop()));
            _logWriterThread.IsBackground = true;
            _logWriterThread.Start();
        }

        public void StopWait()
        {
            _logMessageQueue.CompleteAdding();
            _logWriterThread.Join();
        }

        public void Log(string message)
        {
            var msg = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} : {message}";
            var added = _logMessageQueue.TryAdd(msg);
            if (!added)
            {
                throw new Exception("Failed to add log message");
            }
        }

        void LogWriterLoop()
        {
            try
            {
                while (!_logMessageQueue.IsCompleted)
                {
                    var msg = _logMessageQueue.Take();

                    // Debug.WriteLine("[DEBUG] " + message);
                    // Trace.WriteLine("[TRACE] " + message);
                    bool pendingWrite = true;
                    do
                    {
                        try
                        {
                            File.AppendAllLines(_logFilePath, new[] { msg }, UTF8);
                            pendingWrite = false;
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(5);
                        }
                    }
                    while (pendingWrite);
                }
            }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { }
            catch (Exception)
            {

            }
        }
    }
}