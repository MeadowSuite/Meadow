using Meadow.Core.Utils;
using System;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Cli
{
    static class Util
    {
        public static string GetUniqueID()
        {
            var guidBytes = new byte[9];
            System.Security.Cryptography.RandomNumberGenerator.Fill(guidBytes);
            var guid = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "_" + BaseEncoding.ToBaseString(guidBytes, BaseEncoding.CHAR_SETS.BASE36); //HexUtil.GetHexFromBytes(guidBytes);
            return guid;
        }

        public static string GetSolSourcePath(Config config, SessionState sessionState)
        {
            if (Path.IsPathRooted(config.SourceDirectory))
            {
                return Path.GetFullPath(config.SourceDirectory);
            }
            else
            {
                var cwd = sessionState.Path.CurrentLocation.Path;
                var dir = Path.Combine(cwd, config.SourceDirectory);
                return Path.GetFullPath(dir);
            }
        }

        public static T GetResultSafe<T>(this Task<T> task)
        {
            if (SynchronizationContext.Current == null)
            {
                return task.Result;
            }

            if (task.IsCompleted)
            {
                return task.Result;
            }

            var tcs = new TaskCompletionSource<T>();
            task.ContinueWith(
                t =>
                {
                    var ex = t.Exception;
                    if (ex != null)
                    {
                        tcs.SetException(ex);
                    }
                    else
                    {
                        tcs.SetResult(t.Result);
                    }
                }, 
                TaskScheduler.Default);

            return tcs.Task.Result;
        }
    }
}
