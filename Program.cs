using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dropbox.Api;
using Dropbox.Api.Files;

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
				DropboxClientFactory.ResetAuthentication();
            }

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

            try
            {
				var client = DropboxClientFactory.CreateDropboxClient().Result;
				var task = Task.Run(() => Upload(files, options, client, cts.Token), cts.Token);
				task.Wait(cts.Token);
                exitCode = ExitCode.Success;
            }
            catch(OperationCanceledException)
            {
                Output("\nUpload canceled", options);

                exitCode = ExitCode.Canceled;
            }
            catch(AggregateException ex)
            {
                foreach(var exception in ex.Flatten().InnerExceptions)
                {
                    if(exception is DropboxException)
                    {
                        exitCode = HandleDropboxException(exception as DropboxException);
                    }
                    else
                    {
	                    exitCode = HandleGenericError(ex);
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

	    private static async Task Upload(IEnumerable<string> paths, UploadOptions options, DropboxClient client,
            CancellationToken cancellationToken)
        {
            foreach(var path in paths)
            {
                var source = Path.GetFullPath(path);
                var filename = Path.GetFileName(source);

                await Upload(source, filename, options, client, cancellationToken);
            }
        }

        private static async Task Upload(string source, string filename, UploadOptions options, DropboxClient client,
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

				if(!options.Chunked && fs.Length >= 150 * 1024 * 1024)
				{
					Output("File is larger than 150MB, using chunked uploading.", options);
					options.Chunked = true;
				}

				if(options.Chunked)
				{
					var progress = ConfigureProgressHandler(options, fs.Length);
					uploaded = await client.UploadChunked(options.DropboxPath, filename, fs, cancellationToken, progress);
				}
				else
                {
                    uploaded = await client.Upload(options.DropboxPath, filename, fs);
                }

                Output("Whoosh...", options);
                Output($"Uploaded {uploaded.Name} to {uploaded.PathDisplay}; Revision {uploaded.AsFile.Rev}", options);
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

        private static ExitCode HandleDropboxException(DropboxException ex)
        {
            Console.WriteLine("An error occurred and your file was not uploaded.");

            var exitCode = ex.HandleAuthException() 
				?? ex.HandleAccessException()
				?? ex.HandleRateLimitException()
				?? ex.HandleBadInputException()
				?? ex.HandleHttpException()
				?? ExitCode.UnknownError;

	        if (exitCode == ExitCode.UnknownError)
	        {
				Console.WriteLine(ex.Message);
	        }

	        return exitCode;
        }

	    private static ExitCode HandleGenericError(Exception ex)
	    {
			Console.WriteLine("An error occurred and your file was not uploaded.");
			Console.WriteLine(ex);

			return ExitCode.UnknownError;
	    }
    }
}