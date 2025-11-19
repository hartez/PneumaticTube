using CommandLine;
using System;

namespace PneumaticTube
{
    internal class UploadOptions 
    {
		// Default to the root path
	    private string _dropboxPath = "/";

	    [Option('f', "file", Required = true, HelpText = "The path of the local file to upload. If this is a folder, the immediate contents of the folder (non-recursive) will be uploaded to the destination.")]
        public string LocalPath { get; set; }

	    [Option('p', "path", Required = false, HelpText = "The destination folder path in Dropbox")]
	    public string DropboxPath
	    {
		    get { return _dropboxPath; }
		    set
		    {
				if (!value.StartsWith('/'))
				{
					value = $"/{value}";
				}

				_dropboxPath = value;
		    }
	    }

	    [Option('r', "reset", Required = false, HelpText = "Force PneumaticTube to re-authorize with Dropbox")]
        public bool Reset { get; set; }

        [Option('b', "bytes", Required = false,
            HelpText = "Display progress in bytes instead of percentage when using chunked uploading")]
        public bool Bytes { get; set; }

        [Option('c', "chunked", Required = false, HelpText = "Force chunked uploading")]
        public bool Chunked { get; set; }

        [Option('q', "quiet", Required = false, HelpText = "Suppress all output")]
        public bool Quiet { get; set; }

        [Option('n', "noprogress", Required = false, HelpText = "Suppress progress output when using chunked uploading")]
        public bool NoProgress { get; set; }

		[Option('k', "chunksize", Required = false, HelpText = "Chunk size (in kilobytes) to use during chunked uploading. Defaults to 1024, minimum is 128.")]
		public int ChunkSizeInKilobytes { get; set; } = DropboxClientExtensions.DefaultChunkSizeInKilobytes;

		public int ChunkSize => Math.Max(ChunkSizeInKilobytes * 1024, DropboxClientExtensions.MinimumChunkSize);

		[Option('t', "timeout", Required = false, HelpText = "HTTP Timeout (in seconds) for upload operations. Defaults to 100.")]
		public int TimeoutSeconds { get; set; } = DropboxClientExtensions.DefaultTimeoutInSeconds;
	}
}