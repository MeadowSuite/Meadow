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

namespace Meadow.Cli.Commands
{
    [Cmdlet(ApprovedVerbs.Get, "Template")]
    public class GetTemplateCommand : PSCmdlet
    {
        [Parameter(Position = 0)]
        [ValidatePatternFriendly(
                                Pattern = "^[A-Za-z0-9\\s]+$",
                                Message = "FileName parameter should only include upper and lower case letters and spaces.  Special characters in FileName are not valid.")]
        public string FileName { get; set; } = "Meadow-AuditTemplate-master";

        [Parameter(Position = 1)]
        public string FilePath { get; set; } = Environment.CurrentDirectory;

        [Parameter(Position = 2)]

        protected override void EndProcessing()
        {
            Uri projectUri = ProjectUri;
            string fileName = String.Concat("\\", String.Concat(FileName, ".zip"));
            string filePath = FilePath;
            string saveLocation = String.Concat(filePath, String.Concat("\\", fileName));

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(projectUri);
                request.Headers["Accept"] = "*/*";
                request.Headers["AllowAutoRedirect"] = "true";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (File.Exists(saveLocation))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Found existing archive with name specified!\n");
                        Console.ResetColor();
                        var choices = new Collection<ChoiceDescription>(new[]
                        {
                        new ChoiceDescription("&Cancel"),
                        new ChoiceDescription("&Overwrite")
                        });
                        var overwrite = Host.UI.PromptForChoice($"Zip archive with that name already exists at {saveLocation}", "Continue and overwite existing archive?", choices, 0);
                        if (overwrite != 1)
                        {
                            Host.UI.WriteErrorLine("Zip archive has not been overwritten...\n");
                            return;
                        }
                        else
                        {
                            File.Delete(saveLocation);
                            Stream responseStream = response.GetResponseStream();
                            FileStream fileStream = new FileStream(saveLocation, FileMode.Create, FileAccess.Write);
                            BinaryWriter binFile = new BinaryWriter(fileStream);
                            int i;
                            while ((i = responseStream.ReadByte()) != -1)
                            {
                                binFile.Write(Convert.ToByte(i));
                            }

                            if (binFile.BaseStream.Position == binFile.BaseStream.Length
                            && fileStream.Position == fileStream.Length)
                            {
                                Console.WriteLine("Status from remote server: " + response.StatusCode);
                                Console.WriteLine("Additional information: " + response.StatusDescription);
                                fileStream.Close();
                                fileStream.Dispose();
                                binFile.Close();
                                binFile.Dispose();
                                response.Close();
                                response.Dispose();
                                Console.WriteLine("Template archive download is complete.\n");
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("An error may have occured.  Please check the output for more information.\n");
                                Console.ResetColor();
                            }
                        }
                    }
                    else
                    {
                        Stream responseStream = response.GetResponseStream();
                        FileStream fileStream = new FileStream(saveLocation, FileMode.Create, FileAccess.Write);
                        BinaryWriter binFile = new BinaryWriter(fileStream);
                        int i;
                        while ((i = responseStream.ReadByte()) != -1)
                        {
                            binFile.Write(Convert.ToByte(i));
                        }

                        if (binFile.BaseStream.Position == binFile.BaseStream.Length
                        && fileStream.Position == fileStream.Length)
                        {
                            Console.WriteLine("Status from remote server: " + response.StatusCode);
                            Console.WriteLine("Additional information: " + response.StatusDescription);
                            fileStream.Close();
                            fileStream.Dispose();
                            binFile.Close();
                            binFile.Dispose();
                            response.Close();
                            response.Dispose();
                            Console.WriteLine("Template archive download is complete.\n");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("An error may have occured.  Please check the output for more information.\n");
                            Console.ResetColor();
                        }
                    }
                }

                WriteObject(new { FileName, FilePath });
            }
            catch (WebException webException) when (webException.Status == WebExceptionStatus.UnknownError)
            {

                Host.UI.WriteErrorLine("You have encountered an unknown error, please check your local environment for potential issues and try again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + webException.Status + "\n");
                if (webException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + webException.Response + "\n");
                }

                if (webException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + webException.Message + "\n");
                }
            }
            catch (WebException connectionFailedException) when (connectionFailedException.Status == WebExceptionStatus.ConnectFailure)
            {
                Host.UI.WriteErrorLine("The connection to the server has failed. You may have encountered an issue with your firewall or network security.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + connectionFailedException.Status + "\n");

                if (connectionFailedException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + connectionFailedException.Response + "\n");
                }

                if (connectionFailedException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + connectionFailedException.Message + "\n");
                }
            }
            catch (WebException connectionClosedException) when (connectionClosedException.Status == WebExceptionStatus.ConnectionClosed)
            {
                Host.UI.WriteErrorLine("The connection to the server has been closed.  The server may have reached capacity for requests at this time.\rPlease wait until a short time has elapsed and try again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + connectionClosedException.Status + "\n");

                if (connectionClosedException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + connectionClosedException.Response + "\n");
                }

                if (connectionClosedException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + connectionClosedException.Message + "\n");
                }
            }
            catch (WebException nameResolutionException) when (nameResolutionException.Status == WebExceptionStatus.NameResolutionFailure)
            {
                Host.UI.WriteErrorLine("Name resolution has failed.  Please verify that the URL used leads to a zip archive of the project you wish to import.\rThis may indicate an issue with your DNS server.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + nameResolutionException.Status + "\n");

                if (nameResolutionException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + nameResolutionException.Response + "\n");
                }

                if (nameResolutionException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + nameResolutionException.Message + "\n");
                }
            }
            catch (WebException requestCancledException) when (requestCancledException.Status == WebExceptionStatus.RequestCanceled)
            {
                Host.UI.WriteErrorLine("Your request has been cancled by the Server.  This may indicate that your client or the server has reached capacity for requests.\rPlease wait until a short time has elapsed and try again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + requestCancledException.Status + "\n");

                if (requestCancledException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + requestCancledException.Response + "\n");
                }

                if (requestCancledException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + requestCancledException.Message + "\n");
                }
            }
            catch (WebException receiveFailedException) when (receiveFailedException.Status == WebExceptionStatus.ReceiveFailure)
            {
                Host.UI.WriteErrorLine("File reception has failed.  This may indicate that the server has reached capacity for requests at this time.\rPlease wait until a short time has elapsed and try again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + receiveFailedException.Status + "\n");

                if (receiveFailedException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + receiveFailedException.Response + "\n");
                }

                if (receiveFailedException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + receiveFailedException.Message + "\n");
                }
            }
            catch (WebException timeOutException) when (timeOutException.Status == WebExceptionStatus.Timeout)
            {
                Host.UI.WriteErrorLine("Your connection to the server has timed out.  This may indicate that the server is having an issue fulfilling requests in a timely manner.\rPlease wait until a short time has elapsed and try again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + timeOutException.Status + "\n");

                if (timeOutException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + timeOutException.Response + "\n");
                }

                if (timeOutException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + timeOutException.Message + "\n");
                }
            }
            catch (WebException sendFailedException) when (sendFailedException.Status == WebExceptionStatus.SendFailure)
            {
                Host.UI.WriteErrorLine("Request transmission has failed.  This may indicate that the server has reached capacity for requests at this time.\rPlease wait until a short time has elapsed and try again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + sendFailedException.Status + "\n");

                if (sendFailedException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + sendFailedException.Response + "\n");
                }

                if (sendFailedException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + sendFailedException.Message + "\n");
                }
            }
            catch (WebException trustFailureException) when (trustFailureException.Status == WebExceptionStatus.TrustFailure)
            {
                Host.UI.WriteErrorLine("The certificate for the server you are trying to connect to cannot be verified.  Please check to make sure that the server has a valid certificate to prove its identity and try again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + trustFailureException.Status + "\n");

                if (trustFailureException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + trustFailureException.Response + "\n");
                }

                if (trustFailureException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + trustFailureException.Message + "\n");
                }
            }
            catch (WebException messageLengthException) when (messageLengthException.Status == WebExceptionStatus.MessageLengthLimitExceeded)
            {
                Host.UI.WriteErrorLine("The length of the request sent to the server or received from the server was too long to process.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + messageLengthException.Status + "\n");

                if (messageLengthException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + messageLengthException.Response + "\n");
                }

                if (messageLengthException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + messageLengthException.Message + "\n");
                }
            }
            catch (WebException pendingException) when (pendingException.Status == WebExceptionStatus.Pending)
            {
                Host.UI.WriteErrorLine("Your request is still pending.  Please cancel this request and try once a short time has elapsed.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + pendingException.Status + "\n");

                if (pendingException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + pendingException.Response + "\n");
                }

                if (pendingException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + pendingException.Message + "\n");
                }
            }
            catch (WebException protocolException) when (protocolException.Status == WebExceptionStatus.ProtocolError)
            {
                Host.UI.WriteErrorLine("The message was received from the server, but indicated that a protocol error was encountered.  This may indicate that the server was unable to process the request due to missing information or access rights.\rPlease try again once a short time has elapsed.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + protocolException.Status + "\n");

                if (protocolException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + protocolException.Response + "\n");
                }

                if (protocolException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + protocolException.Message + "\n");
                }
            }
            catch (WebException serverProtocolException) when (serverProtocolException.Status == WebExceptionStatus.ServerProtocolViolation)
            {
                Host.UI.WriteErrorLine("The message from the server was received, but was an invalid HTTP response.  Please check the URL and try this request again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + serverProtocolException.Status + "\n");

                if (serverProtocolException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + serverProtocolException.Response + "\n");
                }

                if (serverProtocolException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + serverProtocolException.Message + "\n");
                }
            }
            catch (WebException keepAliveException) when (keepAliveException.Status == WebExceptionStatus.KeepAliveFailure)
            {
                Host.UI.WriteErrorLine("The connection was closed even though a keep-alive (persistent connection) was requested.  Please wait until a short time has elapsed and try your request again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + keepAliveException.Status + "\n");

                if (keepAliveException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + keepAliveException.Response + "\n");
                }

                if (keepAliveException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + keepAliveException.Message + "\n");
                }
            }
            catch (WebException cacheEntryException) when (cacheEntryException.Status == WebExceptionStatus.CacheEntryNotFound)
            {
                Host.UI.WriteErrorLine("The specified entry was not found in the cache.  Please clear your browser and/or DNS cache and try the request again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + cacheEntryException.Status + "\n");

                if (cacheEntryException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + cacheEntryException.Response + "\n");
                }

                if (cacheEntryException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + cacheEntryException.Message + "\n");
                }
            }
            catch (WebException pipelineException) when (pipelineException.Status == WebExceptionStatus.PipelineFailure)
            {
                Host.UI.WriteErrorLine("The request made to the server was a pipeline (compound) request, and was closed before it could complete.\rPlease wait until a short time has elapsed and try your request again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + pipelineException.Status + "\n");

                if (pipelineException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + pipelineException.Response + "\n");
                }

                if (pipelineException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + pipelineException.Message + "\n");
                }
            }
            catch (WebException proxyNameResolutionException) when (proxyNameResolutionException.Status == WebExceptionStatus.ProxyNameResolutionFailure)
            {
                Host.UI.WriteErrorLine("The DNS server was unable to resolve your proxy server address.  Please check your proxy settings and validate that the correct name or IP address for the proxy server was specified and try again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + proxyNameResolutionException.Status + "\n");

                if (proxyNameResolutionException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + proxyNameResolutionException.Response + "\n");
                }

                if (proxyNameResolutionException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + proxyNameResolutionException.Message + "\n");
                }
            }
            catch (WebException prohibitedCachePolicyException) when (prohibitedCachePolicyException.Status == WebExceptionStatus.RequestProhibitedByCachePolicy)
            {
                Host.UI.WriteErrorLine("The current cache policy does not allow this type of request to be cached.  This may happen if the server you are trying to contact requires direct interaction to complete this request.\rPlease check to make sure that you can make a cached request to the server and try again.  Error output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + prohibitedCachePolicyException.Status + "\n");

                if (prohibitedCachePolicyException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + prohibitedCachePolicyException.Response + "\n");
                }

                if (prohibitedCachePolicyException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + prohibitedCachePolicyException.Message + "\n");
                }
            }
            catch (WebException prohibitedProxyException) when (prohibitedProxyException.Status == WebExceptionStatus.RequestProhibitedByProxy)
            {
                Host.UI.WriteErrorLine("The request you have made to the server is not allowed by your proxy server.  Please check your proxy policy or speak to your local administrator to allow a request of this type to reach the destination server and accept a response.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + prohibitedProxyException.Status + "\n");

                if (prohibitedProxyException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + prohibitedProxyException.Response + "\n");
                }

                if (prohibitedProxyException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + prohibitedProxyException.Message + "\n");
                }
            }
            catch (WebException secureChannelException) when (secureChannelException.Status == WebExceptionStatus.SecureChannelFailure)
            {
                Host.UI.WriteErrorLine("An error occured when trying a Secure Socket Layer (encrypted connection) to the server.  Please validate the server certificate matches your intended destination and that the server accepts SSL connections and try again.\rError output may contain further information.\n");
                Host.UI.WriteErrorLine("Error Status: " + secureChannelException.Status + "\n");

                if (secureChannelException.Response != null)
                {
                    Host.UI.WriteErrorLine("Server Response: " + secureChannelException.Response + "\n");
                }

                if (secureChannelException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error Message: " + secureChannelException.Message + "\n");
                }
            }
            catch (ValidationMetadataException validationException)
            {
                Host.UI.WriteErrorLine(validationException.Message);
            }
        }      
    }

    [Cmdlet(ApprovedVerbs.Expand, "Template")]
    public class ExpandTemplateCommand : PSCmdlet
    {
        [Parameter(
                    Position = 0,
                    ValueFromPipelineByPropertyName = true)]
        [ValidatePatternFriendly(
                                Pattern = "^[A-Za-z0-9\\s]+$",
                                Message = "FileName parameter should only include upper and lower case letters and spaces.  Special characters in FileName are not valid.")]
        public string FileName { get; set; } = "Meadow-AuditTemplate-master";

        [Parameter(
                    Position = 1,
                    ValueFromPipelineByPropertyName = true,
                    ValueFromRemainingArguments = true)]
        public string FilePath { get; set; } = Environment.CurrentDirectory;

        [Parameter(Position = 2)]
        public string Destination { get; set; } = Environment.CurrentDirectory;

        protected override void EndProcessing()
        {
            string fileName = String.Concat("\\", String.Concat("\\", FileName));
            string filePath = FilePath;
            string directoryName = String.Concat("\\", Path.GetFileNameWithoutExtension(filePath + fileName));
            string destination = String.Concat(Destination, directoryName);
            string zipLocation = String.Concat(filePath, "\\" + fileName);

            try
            {
                if (File.Exists(zipLocation))
                {
                    Console.WriteLine("Checking file path for existing files...");
                    if (Directory.Exists(destination))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Found existing directory with the specified name!\n");
                        Console.ResetColor();
                        var choices = new Collection<ChoiceDescription>(new[]
                        {
                            new ChoiceDescription("&Cancel"),
                            new ChoiceDescription("&Overwrite")
                        });
                        var overwrite = Host.UI.PromptForChoice($"Files already exists at {destination}", "Continue and overwite existing files?", choices, 0);
                        if (overwrite != 1)
                        {
                            Host.UI.WriteErrorLine("Files have not been overwritten...\n");
                            return;
                        }
                        else
                        {
                            Directory.Delete(destination, true);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Unpacking audit template to local disk...");
                            ZipFile.ExtractToDirectory(zipLocation, destination);
                            Console.WriteLine("...audit template files unpacked successfully.\n");
                            Console.WriteLine("Template project located at...\n");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Unpacking audit template to local disk...");
                        ZipFile.ExtractToDirectory(zipLocation, destination);
                        Console.WriteLine("...audit template files unpacked successfully.\n");
                        Console.WriteLine("Template project located at...\n");
                        Console.ResetColor();

                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to locate zip archive for template at " + zipLocation + "\n");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Please specify the correct path to a zip archive containing the template project you wish to use.\n");
                    Console.ResetColor();
                }

                Destination = destination;
                WriteObject(new { FilePath, FileName, Destination });
            }
            catch (PathTooLongException pathLength)
            {
                Host.UI.WriteErrorLine("Specified path is too long, please change FilePath and Destination to less nested directories.  Please keep in mind that the nesting of directories inside the archive can also cause an issue once unpacked at the destination as the file structure will be preserved as it exists in the archive.");
                Host.UI.WriteErrorLine("Error code: " + pathLength.HResult);
                Host.UI.WriteErrorLine("Error message: " + pathLength.Message);
                Host.UI.WriteErrorLine("Inner exception: " + pathLength.InnerException);
            }
            catch (ArgumentException argException)
            {
                Host.UI.WriteErrorLine("FilePath or Destination path is null/empty.  Please specify a valid path to your FilePath (zip archive location) and the Destination you would like to have it unpacked to.");
                Host.UI.WriteErrorLine("Error code: " + argException.HResult);

                if (argException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + argException.Message);
                }

                if (argException.InnerException != null)
                {
                    Host.UI.WriteErrorLine("Inner exception: " + argException.InnerException);
                }
            }
            catch (DirectoryNotFoundException directoryException)
            {
                Host.UI.WriteErrorLine("Directory specified in FilePath, FileName, or Destination does not exist.  Please change the FilePath and Destination path information to valid directories and FileName to a valid zip archive on your system.");
                Host.UI.WriteErrorLine("Error code: " + directoryException.HResult);

                if (directoryException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + directoryException.Message);
                }

                if (directoryException.InnerException != null)
                {
                    Host.UI.WriteErrorLine("Inner exception: " + directoryException.InnerException);
                }
            }
            catch (IOException ioException)
            {
                Host.UI.WriteErrorLine("An IO exception has occured.  This can be the result of duplicate filenames being unpacked at the Destination location, a file missing a name in the archive, the FilePath or FileName specified not existing, or the directory specified at the Destination already exists.");
                Host.UI.WriteErrorLine("Error code: " + ioException.HResult);

                if (ioException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + ioException.Message);
                }

                if (ioException.InnerException != null)
                {
                    Host.UI.WriteErrorLine("Inner exception: " + ioException.InnerException);
                }
            }
            catch (UnauthorizedAccessException accessException)
            {
                Host.UI.WriteErrorLine("You do not have rights required to access the FilePath or Destination specified.  Please specify a FilePath and Destination that you have read/write access to.");
                Host.UI.WriteErrorLine("Error code: " + accessException.HResult);

                if (accessException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + accessException.Message);
                }

                if (accessException != null)
                {
                    Host.UI.WriteErrorLine("Inner exception: " + accessException.InnerException);
                }
            }
            catch (NotSupportedException supportException)
            {
                Host.UI.WriteErrorLine("FilePath, FileName, or Destination as specified contain invalid formatting.  Please verify that FilePath, FileName, and Destination path information do not contain any invalid characters or names.");
                Host.UI.WriteErrorLine("Error code: " + supportException.HResult);

                if (supportException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + supportException.Message);
                }

                if (supportException.InnerException != null)
                {
                    Host.UI.WriteErrorLine("Inner exception: " + supportException.InnerException);
                }
            }
            catch (InvalidDataException dataException)
            {
                Host.UI.WriteErrorLine("The zip archive specified by FilePath/FileName does not contain valid data.  Please try the download again.");
                Host.UI.WriteErrorLine("Error code: " + dataException.HResult);

                if (dataException.Message != null)
                {
                    Host.UI.WriteErrorLine("Error message: " + dataException.Message);
                }

                if (dataException.InnerException != null)
                {
                    Host.UI.WriteErrorLine("Inner exception: " + dataException.InnerException);
                }
            }
        }
    }
}