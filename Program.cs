using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using DropNetRT;
using DropNetRT.Exceptions;
using DropNetRT.Models;
using PneumaticTube.Properties;

namespace PneumaticTube
{
    internal class Program
    {
        private enum ExitCode : int
        {
            Success = 0,
            FileNotFound = 2,
            AccessDenied = 5,
            BadArguments = 160,
            FileExists = 80,
            Canceled = 1223,
            UnknownError = int.MaxValue
        }

        private static int Main(string[] args)
        {
            var options = new UploadOptions();

            if (!Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return (int)ExitCode.BadArguments;
            }

            if (options.Reset)
            {
                Settings.Default.USER_SECRET = String.Empty;
                Settings.Default.USER_TOKEN = String.Empty;
                Settings.Default.Save();
            }

            var client = DropNetClientFactory.CreateDropNetClient();

            // See if we have a token already
            if (String.IsNullOrEmpty(Settings.Default.USER_TOKEN) || String.IsNullOrEmpty(Settings.Default.USER_SECRET))
            {
                Console.WriteLine(
                    "You'll need to authorize this account with PneumaticTube; a browser window will now open asking you to log into Dropbox and allow the app. When you've done that, hit Enter.");

                var requestToken = client.GetRequestToken().Result;

                // Pop open the authorization page in the default browser
                var url = client.BuildAuthorizeUrl(requestToken);
                Process.Start(url);

                // Wait for the user to hit Enter
                Console.ReadLine();

                var accessToken = client.GetAccessToken().Result;

                // Save the token and secret 
                Settings.Default.USER_SECRET = accessToken.Secret;
                Settings.Default.USER_TOKEN = accessToken.Token;
                Settings.Default.Save();
            }

            client.SetUserToken(new UserLogin
            {
                Token = Settings.Default.USER_TOKEN,
                Secret = Settings.Default.USER_SECRET
            });

            var source = Path.GetFullPath(options.LocalPath);
            var filename = Path.GetFileName(source);

            if (!File.Exists(source))
            {
                Console.WriteLine("Source file does not exist.");
                return (int)ExitCode.FileNotFound;
            }

            // Fix up Dropbox path (fix Windows-style slashes)
            options.DropboxPath = options.DropboxPath.Replace(@"\", "/");

            Console.WriteLine("Uploading {0} to {1}", filename, options.DropboxPath);
            Console.WriteLine("Ctrl-C to cancel");

            var exitCode = ExitCode.UnknownError;

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var task = Task.Run(() => Upload(source, filename, options, client, cts.Token), cts.Token);

            try
            {
                task.Wait(cts.Token);
                exitCode = ExitCode.Success;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Upload canceled");

                exitCode = ExitCode.Canceled;
            }
            catch(AggregateException ex)
            {
                foreach(var exception in ex.Flatten().InnerExceptions)
                {
                    if(exception is DropboxException)
                    {
                        exitCode = HandleException(exception as DropboxException);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("An error occurred and your file was not uploaded.");
                Console.WriteLine(ex);
            }

            return (int)exitCode;
        }

        private static void Upload(string source, string filename, UploadOptions options, DropNetClient client, CancellationToken cancellationToken)
        {
            using (var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                var progress = options.Bytes 
                    ? (IProgress<long>) new BytesProgressDisplay(fs.Length)
                    : new PercentProgressDisplay(fs.Length);

                // TODO Check for force chunked option or file size >= 150MB, otherwise use regular upload

                if(!options.Chunked && fs.Length >= 150 * 1024 * 1024)
                {
                    Console.WriteLine("File is larger than 150MB, using chunked uploading.");
                    options.Chunked = true;
                }

                Metadata uploaded;

                if(options.Chunked)
                {
                    uploaded = client.UploadChunked(options.DropboxPath, filename, fs, cancellationToken, progress).Result;
                }
                else
                {
                    uploaded = client.Upload(options.DropboxPath, filename, fs, cancellationToken).Result;
                }

                Console.WriteLine("Whoosh...");
                Console.WriteLine("Uploaded {0} to {1}; Revision {2}", uploaded.Name, uploaded.Path,
                    uploaded.Revision);
            }
        }

        private static ExitCode HandleException(DropboxException ex)
        {
            Console.WriteLine("An error occurred and your file was not uploaded.");
            Console.WriteLine(ex.StatusCode);
            Console.WriteLine(ex.Response);

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ExitCode.AccessDenied;
            }
            else if (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Shouldn't happen with the DropNet defaults (overwrite = true), but just in case 
                return ExitCode.FileExists;
            }

            return ExitCode.UnknownError;
        }
    }
}