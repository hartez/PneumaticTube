using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace PneumaticTube
{
	internal static class DropboxClientExtensions
	{
		public const long ChunkSize = 10 * 1024 * 1024;
		public const long ChunkedThreshold = 150 * 1024 * 1024;

		private static string CombinePath(string folder, string fileName) 
		{
			// We can't use Path.Combine here because we'll end up with the Windows separator ("\") and 
			// we need the forward slash ("/")

			if (folder == "/") 
			{
				return $"/{fileName}";
			}
			
			return $"{folder}/{fileName}";
		}

		public static async Task<FileMetadata> Upload(this DropboxClient client, string folder, string fileName, Stream fs, DateTime? modified = default)
		{
			var fullDestinationPath = CombinePath(folder, fileName);

			return await client.Files.UploadAsync(fullDestinationPath, WriteMode.Overwrite.Instance, body: fs, clientModified: modified);
		}

		public static async Task<FileMetadata> UploadChunked(this DropboxClient client, 
			string folder, string fileName, Stream fs, CancellationToken cancellationToken, IProgress<long> progress, DateTime? modified = default)
		{
			int chunks = (int)Math.Ceiling((double)fs.Length / ChunkSize);

			byte[] buffer = new byte[ChunkSize];
			string sessionId = null;

			FileMetadata resultMetadata = null;
			var fullDestinationPath = CombinePath(folder, fileName);

			for (var i = 0; i < chunks; i++)
			{
				if(cancellationToken.IsCancellationRequested)
				{
					throw new OperationCanceledException(cancellationToken);
				}

				var bytesRead = fs.Read(buffer, 0, (int)ChunkSize);

				using(var memStream = new MemoryStream(buffer, 0, bytesRead))
				{
					if(i == 0)
					{
						var result = await client.Files.UploadSessionStartAsync(body: memStream);
						sessionId = result.SessionId;
					}
					else
					{
						UploadSessionCursor cursor = new UploadSessionCursor(sessionId, (ulong)(ChunkSize * i));

						if(i == chunks - 1)
                        {
                            var commitInfo = new CommitInfo(
                                path: fullDestinationPath, 
                                mode: WriteMode.Overwrite.Instance,
								clientModified: modified
                                );

							resultMetadata = await client.Files.UploadSessionFinishAsync(cursor, commitInfo, memStream);

							if(!cancellationToken.IsCancellationRequested)
							{
								progress.Report(fs.Length);
							}
						}
						else
						{
							await client.Files.UploadSessionAppendV2Async(cursor, body: memStream);
							if(!cancellationToken.IsCancellationRequested)
							{
								progress.Report(i * ChunkSize);
							}
						}
					}
				}
			}

			return resultMetadata;
		}
	}
}