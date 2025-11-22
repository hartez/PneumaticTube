using CommandLine;
using Dropbox.Api;
using Dropbox.Api.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PneumaticTube
{
	internal class Program
	{
		static bool ShowCancelHelp = true;

		private static async Task<int> Main(string[] args)
		{
			return await Parser.Default.ParseArguments<UploadOptions>(args)
			   .MapResult(RunAndReturnExitCode, BadArgs);
		}

		static async Task<int> RunAndReturnExitCode(UploadOptions options)
		{
			if (options.Reset)
			{
				DropboxClientFactory.ResetAuthentication();
			}

			var source = Path.GetFullPath(options.LocalPath);

			if (!File.Exists(source) && !Directory.Exists(source))
			{
				Console.WriteLine("Source does not exist.");
				return (int)ExitCode.FileNotFound;
			}

			// Fix up Dropbox path (fix Windows-style slashes)
			options.DropboxPath = options.DropboxPath.Replace(@"\", "/");

			var exitCode = ExitCode.UnknownError;

			var cts = new CancellationTokenSource();

			Console.CancelKeyPress += (s, e) =>
			{
				e.Cancel = true;
				cts.Cancel();
			};

			try
			{
				var files = GetFiles(source, options);

				var client = await DropboxClientFactory.CreateDropboxClient(options.TimeoutSeconds);
				await Upload(files, options, client, cts.Token);
				exitCode = ExitCode.Success;
			}
			catch (OperationCanceledException)
			{
				Output("\nUpload canceled", options);

				exitCode = ExitCode.Canceled;
			}
			catch (AggregateException ex)
			{
				foreach (var exception in ex.Flatten().InnerExceptions)
				{
					exitCode = exception switch
					{
						DropboxException dex => HandleDropboxException(dex),
						TaskCanceledException tex => HandleTimeoutError(tex),
						_ => HandleGenericError(ex),
					};
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("An error occurred and your upload was not completed.");
				Console.WriteLine(ex);
			}

			return (int)exitCode;
		}

		static FileToUpload[] GetFiles(string source, UploadOptions options)
		{
			// Determine whether source is a file or directory
			var attr = File.GetAttributes(source);
			if (attr.HasFlag(FileAttributes.Directory))
			{
				Output($"Uploading folder \"{source}\" to {(!string.IsNullOrEmpty(options.DropboxPath) ? options.DropboxPath : "Dropbox")}", options);
				Output("Ctrl-C to cancel", options);
				ShowCancelHelp = false;

				if(options.Recursive)
				{
					EnumerationOptions enumerationOptions = new()
					{
						RecurseSubdirectories = true
					};

					// Because we're recursing subdirectories, we may need to convert relative paths into relative paths in the Dropbox destination
					return [.. Directory.GetFiles(source, "*", enumerationOptions).Select(x => new FileToUpload(x, source))];
				}

				return [.. Directory.GetFiles(source).Select(x => new FileToUpload(x))];
			}

			return [new FileToUpload(source)];
		}

		private static async Task Upload(IEnumerable<FileToUpload> files, UploadOptions options, DropboxClient client,
			CancellationToken cancellationToken)
		{
			foreach (var file in files)
			{
				var destinationPath = string.IsNullOrWhiteSpace(file.Subfolder) ? options.DropboxPath : $"{options.DropboxPath}/{file.Subfolder}";
				await Upload(file.FullPath, file.Name, destinationPath, options, client, cancellationToken);
				if(cancellationToken.IsCancellationRequested)
				{
					break;
				}
			}
		}

		private static async Task Upload(string source, string filename, string destinationPath, UploadOptions options, DropboxClient client,
			CancellationToken cancellationToken)
		{
			Output($"Uploading {filename} to {destinationPath}", options);
			Console.Title = $"Uploading {filename} to {destinationPath}";

			if (ShowCancelHelp)
			{
				Output("Ctrl-C to cancel", options);
				ShowCancelHelp = false;
			}

			using var fs = new FileStream(source, FileMode.Open, FileAccess.Read);
			Metadata uploaded;

			var useChunked = options.Chunked;

			if (!useChunked && fs.Length >= DropboxClientExtensions.ChunkedThreshold)
			{
				Output("File is larger than 150MB, using chunked uploading for this file.", options);
				useChunked = true;
			}

			if (useChunked && fs.Length <= options.ChunkSize)
			{
				Output("File is smaller than the specified chunk size, disabling chunked uploading for this file.", options);
				useChunked = false;
			}

			if (useChunked)
			{
				var progress = ConfigureProgressHandler(options, fs.Length);
				uploaded = await client.UploadChunked(destinationPath, filename, fs, progress, options.ChunkSize, cancellationToken);
			}
			else
			{
				uploaded = await client.Upload(destinationPath, filename, fs);
			}
			
			Output("Whoosh...", options);
			Output($"Uploaded {uploaded.Name} to {uploaded.PathDisplay}; Revision {uploaded.AsFile.Rev}", options);
		}

		private static void Output(string message, UploadOptions options)
		{
			if (options.Quiet)
			{
				return;
			}

			Console.WriteLine(message);
		}

		private static IProgress<long> ConfigureProgressHandler(UploadOptions options, long fileSize)
		{
			if (options.NoProgress || options.Quiet)
			{
				return new NoProgressDisplay(fileSize, options.Quiet);
			}

			if (options.Bytes)
			{
				return new BytesProgressDisplay(fileSize);
			}

			return new PercentProgressDisplay(fileSize);
		}

		private static ExitCode HandleDropboxException(DropboxException ex)
		{
			Console.WriteLine("An error occurred and your file was not uploaded.");

			(ExitCode exitCode, string message) = ex switch
			{
				AuthException authException => (ExitCode.AccessDenied, $"An authentication error occurred: {authException}"),
				AccessException accessException => (ExitCode.AccessDenied, $"An access error occurred: {accessException}"),
				RateLimitException rateLimitException => (ExitCode.Canceled, $"A rate limit error occurred: {rateLimitException}"),
				BadInputException badInputException => (ExitCode.BadArguments, $"An error occurred: {badInputException}"),
				HttpException httpException => (ExitCode.BadArguments, $"An HTTP error occurred: {httpException}"),
				_ => (ExitCode.UnknownError, ex.Message)
			};

			Console.WriteLine(message);

			return exitCode;
		}

		private static ExitCode HandleGenericError(Exception ex)
		{
			Console.WriteLine("An error occurred and your file was not uploaded.");
			Console.WriteLine(ex);

			return ExitCode.UnknownError;
		}

		private static ExitCode HandleTimeoutError(TaskCanceledException ex)
		{
			Console.WriteLine("An HTTP operation timed out and your file was not uploaded.");
			Console.WriteLine(ex);

			return ExitCode.Canceled;
		}

		private static Task<int> BadArgs(IEnumerable<Error> errors)
		{
			return Task.FromResult((int)ExitCode.BadArguments);
		}
	}
}