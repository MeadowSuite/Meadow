using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Net;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Meadow.Cli.Commands
{
    [Cmdlet(ApprovedVerbs.Get, "Template")]
    public class GetTemplateCommand : PSCmdlet
    {
        [Parameter(Position = 0)]
        public Uri ProjectUri { get; set; } = new Uri("https://github.com/MeadowSuite/cli-project-template/archive/v1.zip");

        protected override void EndProcessing()
        {
            try
            {
                DownloadAndExtractTemplate(ProjectUri).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Host.UI.WriteErrorLine(ex.ToString());
            }
        }

        async Task DownloadAndExtractTemplate(Uri templateZipWebUri)
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Host.UI.WriteLine($"Downloading {templateZipWebUri}");
                using (var client = new HttpClient())
                using (var result = await client.GetAsync(templateZipWebUri))
                {
                    result.EnsureSuccessStatusCode();
                    using (var zipContentStream = await result.Content.ReadAsStreamAsync())
                    using (var zipArchive = new ZipArchive(zipContentStream, ZipArchiveMode.Read))
                    {
                        Host.UI.WriteLine($"Extracting template zip to current directory...");

                        // Github archive zips embed the repo files into a single parent directory,
                        // so first extract to temp directory, then move repo files out of the root folder
                        // into the current directory. 
                        Directory.CreateDirectory(tmpDir);
                        zipArchive.ExtractToDirectory(tmpDir, overwriteFiles: true);
                    }
                }

                // Move template files out of temp directory into current directory.
                var cwd = SessionState.Path.CurrentLocation.Path;
                var repoDir = Directory.GetFileSystemEntries(tmpDir).Single();
                MoveFiles(repoDir, cwd);
                Host.UI.WriteLine($"Completed");
            }
            finally
            {
                try
                {
                    // cleanup temp directory
                    Directory.Delete(tmpDir, recursive: true);
                }
                catch { }
            }
        }


        static void MoveFiles(string sourceDirectory, string targetDirectory)
        {
            var sourcePath = Path.GetFullPath(sourceDirectory);
            var targetPath = Path.GetFullPath(targetDirectory);

            // Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath, StringComparison.Ordinal));
            }

            // Move all the files & Replaces any files with the same name
            foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var newPath = filePath.Replace(sourcePath, targetPath, StringComparison.Ordinal);
                if (File.Exists(newPath))
                {
                    throw new Exception($"File already exists at: {newPath}");
                }

                File.Move(filePath, newPath);
            }
        }

    }
}