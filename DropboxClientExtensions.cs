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
		public static async Task<FileMetadata> Upload(this DropboxClient client, string folder, string fileName, Stream fs)
		{
			var fullDestinationPath = Path.Combine(folder, fileName);

			return await client.Files.UploadAsync(fullDestinationPath, WriteMode.Overwrite.Instance, body: fs);
		}

		public static async Task<FileMetadata> UploadChunked(this DropboxClient client, 
			string folder, string fileName, Stream fs, CancellationToken cancellationToken, IProgress<long> progress)
		{
			const int chunkSize = 128 * 1024;
			int chunks = (int)Math.Ceiling((double)fs.Length / chunkSize);

			byte[] buffer = new byte[chunkSize];
			string sessionId = null;

			FileMetadata resultMetadata = null;
			var fullDestinationPath = Path.Combine(folder, fileName);

			for(var i = 0; i < chunks; i++)
			{
				if(cancellationToken.IsCancellationRequested)
				{
					throw new OperationCanceledException(cancellationToken);
				}

				var byteRead = fs.Read(buffer, 0, chunkSize);

				using(var memStream = new MemoryStream(buffer, 0, byteRead))
				{
					if(i == 0)
					{
						var result = await client.Files.UploadSessionStartAsync(body: memStream);
						sessionId = result.SessionId;
					}
					else
					{
						UploadSessionCursor cursor = new UploadSessionCursor(sessionId, (ulong)(chunkSize * i));

						if(i == chunks - 1)
						{
							resultMetadata = await client.Files.UploadSessionFinishAsync(cursor, new CommitInfo(fullDestinationPath, WriteMode.Overwrite.Instance), memStream);

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
								progress.Report(i * chunkSize);
							}
						}
					}
				}
			}

			return resultMetadata;
		}
	}
}