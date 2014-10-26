using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using DropNet.Models;
using PneumaticTube.Properties;

namespace PneumaticTube
{
    internal class Program
    {
        private enum ExitCode : int
        {
            Success = 0,
            FileNotFound = 2,
            BadArguments = 160,
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

                // Pop open the authorization page in the default browser
                var url = client.GetTokenAndBuildUrl();
                Process.Start(url);

                // Wait for the user to hit Enter
                Console.ReadLine();

                var accessToken = client.GetAccessToken();

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

            Console.WriteLine("Uploading {0} to {1}", filename, options.DropboxPath);

            using(var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                var uploaded = client.UploadFile(options.DropboxPath, filename, fs);

                // Sadly, the synchronous version of UploadFile in DropNet doesn't give us 
                // any error data, the meta data returned is simply empty when the upload
                // doesn't work. It might be worth moving to the async version for
                // better error handling (and progress info)
                if(String.IsNullOrEmpty(uploaded.Name))
                {
                    Console.WriteLine("An error occurred and your file was not uploaded. Your target path may be invalid.");
                    return (int)ExitCode.UnknownError;
                }

                Console.WriteLine("Whoosh...");
                Console.WriteLine("Uploaded {0} to {1}; Revision {2}", uploaded.Name, uploaded.Path, uploaded.Revision);
            }

            return (int) ExitCode.Success;
        }
    }
}