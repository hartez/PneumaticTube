using System;
using System.Collections.Generic;
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
        static bool ShowCancelHelp = true;

        private static int Main(string[] args)
        {
            var options = new UploadOptions();

            if(!Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return (int)ExitCode.BadArguments;
            }

            if(options.Reset)
            {
                Settings.Default.USER_SECRET = string.Empty;
                Settings.Default.USER_TOKEN = string.Empty;
                Settings.Default.Save();
            }

            var client = DropNetClientFactory.CreateDropNetClient();

            // See if we have a token already
            if(string.IsNullOrEmpty(Settings.Default.USER_TOKEN) || string.IsNullOrEmpty(Settings.Default.USER_SECRET))
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

            if(!File.Exists(source) && !Directory.Exists(source))
            {
                Console.WriteLine("Source does not exist.");
                return (int)ExitCode.FileNotFound;
            }

            // Fix up Dropbox path (fix Windows-style slashes)
            options.DropboxPath = options.DropboxPath.Replace(@"\", "/");

            string[] files;

            // Determine whether source is a file or directory
            var attr = File.GetAttributes(source);
            if(attr.HasFlag(FileAttributes.Directory))
            {
                // TODO see if we like what this looks like for directories
                Output($"Uploading folder \"{source}\" to {options.DropboxPath}", options);
                Output("Ctrl-C to cancel", options);
                ShowCancelHelp = false;

                // TODO Figure out what, if anything, we want to do about subdirectories
                files = Directory.GetFiles(source);
            }
            else
            {
                files = new[] {source};
            }       

            var exitCode = ExitCode.UnknownError;

            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var task = Task.Run(() => Upload(files, options, client, cts.Token), cts.Token);

            try
            {
                task.Wait(cts.Token);
                exitCode = ExitCode.Success;
            }
            catch(OperationCanceledException)
            {
                Output("Upload canceled", options);

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

        private static async Task Upload(IEnumerable<string> paths, UploadOptions options, DropNetClient client,
            CancellationToken cancellationToken)
        {
            foreach(var path in paths)
            {
                var source = Path.GetFullPath(path);
                var filename = Path.GetFileName(source);

                await Upload(source, filename, options, client, cancellationToken);
            }
        }

        private static async Task Upload(string source, string filename, UploadOptions options, DropNetClient client,
            CancellationToken cancellationToken)
        {
            Output($"Uploading {filename} to {options.DropboxPath}", options);
            Console.Title = $"Uploading {filename} to {options.DropboxPath}";

            if(ShowCancelHelp)
            {
                Output("Ctrl-C to cancel", options);
                ShowCancelHelp = false;
            }

            using(var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                Metadata uploaded;

                if(options.Chunked)
                {
                    var progress = ConfigureProgressHandler(options, fs.Length);

                    if(!options.Chunked && fs.Length >= 150*1024*1024)
                    {
                        Output("File is larger than 150MB, using chunked uploading.", options);
                        options.Chunked = true;
                    }

                    uploaded = await client.UploadChunked(options.DropboxPath, filename, fs, cancellationToken, progress);
                }
                else
                {
                    uploaded = await client.Upload(options.DropboxPath, filename, fs, cancellationToken);
                }

                Output("Whoosh...", options);
                Output($"Uploaded {uploaded.Name} to {uploaded.Path}; Revision {uploaded.Revision}", options);
            }
        }

        private static void Output(string message, UploadOptions options)
        {
            if(options.Quiet)
            {
                return;
            }

            Console.WriteLine(message);
        }

        private static IProgress<long> ConfigureProgressHandler(UploadOptions options, long fileSize)
        {
            if(options.NoProgress || options.Quiet)
            {
                return new NoProgressDisplay(fileSize, options.Quiet);
            }

            if(options.Bytes)
            {
                return new BytesProgressDisplay(fileSize);
            }

            return new PercentProgressDisplay(fileSize);
        }

        private static ExitCode HandleException(DropboxException ex)
        {
            Console.WriteLine("An error occurred and your file was not uploaded.");
            Console.WriteLine(ex.StatusCode);
            Console.WriteLine(ex.Response);

            switch(ex.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return ExitCode.AccessDenied;
                case HttpStatusCode.Conflict:
                    // Shouldn't happen with the DropNet defaults (overwrite = true), but just in case 
                    return ExitCode.FileExists;
            }

            return ExitCode.UnknownError;
        }

        private enum ExitCode
        {
            Success = 0,
            FileNotFound = 2,
            AccessDenied = 5,
            BadArguments = 160,
            FileExists = 80,
            Canceled = 1223,
            UnknownError = int.MaxValue
        }
    }
}