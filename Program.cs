using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using CommandLine;
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
            if(String.IsNullOrEmpty(Settings.Default.USER_TOKEN) || String.IsNullOrEmpty(Settings.Default.USER_SECRET))
            {
                Console.WriteLine(
                    "You'll need to authorize this account with PneumaticTube; a browser window will now open asking you to log into dropbox and allow the app. When you've done that, hit Enter.");

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

            client.UserLogin = new UserLogin
            {
                Token = Settings.Default.USER_TOKEN,
                Secret = Settings.Default.USER_SECRET
            };

            var source = Path.GetFullPath(options.LocalPath);
            var filename = Path.GetFileName(source);

            if(!File.Exists(source))
            {
                Console.WriteLine("Source file does not exist.");
                return (int)ExitCode.FileNotFound;
            }

            // Fix up Dropbox path (fix Windows-style slashes)
            options.DropboxPath = options.DropboxPath.Replace(@"\", "/");

            Console.WriteLine("Uploading {0} to {1}", filename, options.DropboxPath);

            var exitCode = ExitCode.UnknownError;

            using(var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    var uploaded = client.Upload(options.DropboxPath, filename, fs).Result;

                    Console.WriteLine("Whoosh...");
                    Console.WriteLine("Uploaded {0} to {1}; Revision {2}", uploaded.Name, uploaded.Path,
                        uploaded.Revision);

                    exitCode = ExitCode.Success;
                }
                catch(AggregateException ex)
                {
                    foreach(var exception in ex.InnerExceptions)
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
            }

            Console.ReadLine();

            return (int)exitCode;
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
            else if(ex.StatusCode == HttpStatusCode.Conflict)
            {
                // Shouldn't happen with the DropNet defaults (overwrite = true), but just in case 
                return ExitCode.FileExists;
            }

            return ExitCode.UnknownError;
        }
    }
}