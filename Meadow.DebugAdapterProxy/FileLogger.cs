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

        readonly FileStream _fileStream;
        readonly StreamWriter _streamWriter;

        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;

            _fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _streamWriter = new StreamWriter(_fileStream, new UTF8Encoding(false, false));

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
            var msg = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} : {message}";
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
                using (_fileStream)
                using (_streamWriter)
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
                                _streamWriter.WriteLine(msg);
                                _streamWriter.Flush();
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
            }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { }
            catch (Exception)
            {

            }
        }
    }
}
