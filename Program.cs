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
        private static void Main(string[] args)
        {
            var options = new UploadOptions();

            if (!Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return;
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

                // Wait f
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
                return;
            }

            Console.WriteLine("Uploading {0} to {1}", filename, options.DropboxPath);

            using(var fs = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                var uploaded = client.UploadFile(options.DropboxPath, filename, fs);

                Console.WriteLine("Whoosh...");
                Console.WriteLine("Uploaded {0} to {1}; Revision {2}", uploaded.Name, uploaded.Path, uploaded.Revision);
            }
        }
    }
}