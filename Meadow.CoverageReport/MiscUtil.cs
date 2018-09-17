using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Meadow.CoverageReport
{
    public static class MiscUtil
    {

        public static string GetFileUrl(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            var uri = "file:///" + filePath;
            return uri;
        }

        public static void OpenBrowser(string filePath)
        {
            var url = GetFileUrl(filePath);

            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&", StringComparison.Ordinal);
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }

        /// <summary>
        /// Creates or empties out a directory
        /// </summary>
        public static void ResetDirectory(string dir)
        {
            // Clear directory if exists.
            // The thread sleeps are for a bug with directory actions not being fully blocking.
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                while (Directory.Exists(dir))
                {
                    Thread.Sleep(1);
                }
            }

            Directory.CreateDirectory(dir);
            while (!Directory.Exists(dir))
            {
                Thread.Sleep(1);
            }
        }

    }
}
