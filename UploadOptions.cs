using CommandLine;
using CommandLine.Text;

namespace PneumaticTube
{
    internal class UploadOptions
    {
        [Option('f', "file", Required = true, HelpText = "The location of the file to upload")]
        public string LocalPath { get; set; }

        [Option('p', "path", Required = true, HelpText = "The destination path in Dropbox")]
        public string DropboxPath { get; set; }

        [Option('r', "reset", Required = false, HelpText = "Force PneumaticTube to re-authorize with Dropbox")]
        public bool Reset { get; set; }


        public string GetUsage()
        {
            var help = new HelpText
            {
                AddDashesToOption = true
            };

            help.AddPreOptionsLine("pneumatictube -f <file> -p <path>");
            help.AddOptions(this);

            return help.ToString();
        }
    }
}