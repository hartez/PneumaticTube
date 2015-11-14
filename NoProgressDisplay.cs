using System;

namespace PneumaticTube
{
	internal class NoProgressDisplay : IProgress<long>
	{
		private readonly long _fileSize;
		private readonly bool _quiet;
		
		public NoProgressDisplay(long fileSize, bool quiet)
		{
			_fileSize = fileSize;
			_quiet = quiet;
		}

		public void Report(long value)
		{
			if (value >= _fileSize)
			{
				if (!_quiet)
				{
					Console.Write("Finished\n");
				}
			}
		}
	}
}