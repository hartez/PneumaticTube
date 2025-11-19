using System.IO;

namespace PneumaticTube
{
	internal class FileToUpload(string path)
	{
		public string FullPath { get; } = Path.GetFullPath(path);
		public string Name { get; } = Path.GetFileName(path);
		public string Subfolder { get; }

		public FileToUpload(string path, string source) : this(path)
		{
			Subfolder = Path.GetDirectoryName(Path.GetRelativePath(source, path)).Replace("\\", "/");
		}
	} 
}