using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace PneumaticTube
{
	internal class UploadOptions
    {
		// Default to the root path
	    private string _dropboxPath = "/";

	    [Option('f', "file", Required = true, HelpText = "The location of the file to upload")]
        public string LocalPath { get; set; }

	    [Option('p', "path", Required = false, HelpText = "The destination path in Dropbox")]
	    public string DropboxPath
	    {
		    get { return _dropboxPath; }
		    set
		    {
				if (!value.StartsWith("/"))
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

        public string GetUsage()
        {
            var help = new HelpText
            {
                AddDashesToOption = true
            };

			help.AddPreOptionsLine(GetVersionInfo());
			help.AddPreOptionsLine("");
			help.AddPreOptionsLine("pneumatictube -f <file> -p <path>");
            help.AddOptions(this);

            return help.ToString();
        }

		private static string GetVersionInfo()
		{
			var assembly = typeof(UploadOptions).GetTypeInfo().Assembly;
			var assemblyName = new AssemblyName(assembly.FullName);
			var ver = assemblyName.FullName.Split('=')[1].Split(',')[0];

			return $"PneumaticTube version {ver}";
		}
	}
}