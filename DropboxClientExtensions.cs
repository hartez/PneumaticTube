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
			if (!folder.StartsWith("/"))
			{
				folder = $"/{folder}";
			}

			var fullDestinationPath = $"{folder}/{fileName}";

			return await client.Files.UploadAsync(fullDestinationPath, WriteMode.Overwrite.Instance, body: fs);
		}

		public static async Task<FileMetadata> UploadChunked(this DropboxClient client, 
			string folder, string fileName, Stream fs, CancellationToken cancellationToken, IProgress<long> progress)
		{
			const int chunkSize = 128 * 1024;
			int numChunks = (int)Math.Ceiling((double)fs.Length / chunkSize);

			byte[] buffer = new byte[chunkSize];
			string sessionId = null;

			FileMetadata resultMetadata = null;

			for (var idx = 0; idx < numChunks; idx++)
			{
				//if(cancellationToken.)

				var byteRead = fs.Read(buffer, 0, chunkSize);

				using (MemoryStream memStream = new MemoryStream(buffer, 0, byteRead))
				{
					if (idx == 0)
					{
						var result = await client.Files.UploadSessionStartAsync(body: memStream);
						sessionId = result.SessionId;
					}

					else
					{
						UploadSessionCursor cursor = new UploadSessionCursor(sessionId, (ulong)(chunkSize * idx));

						if (idx == numChunks - 1)
						{
							resultMetadata = await client.Files.UploadSessionFinishAsync(cursor, new CommitInfo(folder + "/" + fileName), memStream);
						}
						else
						{
							await client.Files.UploadSessionAppendV2Async(cursor, body: memStream);
						}

						progress.Report(idx * chunkSize);
					}
				}
			}

			return resultMetadata;
		}
	}
}