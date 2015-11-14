using System;
using System.IO;
using System.Security;

namespace PneumaticTube
{
	internal class BytesProgressDisplay : IProgress<long>
	{
		private readonly long _fileSize;
		private readonly bool _consoleCanReportProgress;

		public BytesProgressDisplay(long fileSize)
		{
			_fileSize = fileSize;
			_consoleCanReportProgress = true;

			try
			{
				// This will throw an exception if we're running in ISE
				var top = Console.CursorTop;
			}
			catch (SecurityException)
			{
				// No permission to mess with the console, 
				_consoleCanReportProgress = false;
			}
			catch (IOException)
			{
				// This console doesn't allow position setting
				_consoleCanReportProgress = false;
			}
		}

		public void Report(long value)
		{
			if (_consoleCanReportProgress)
			{
				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write("{0} of {1} uploaded.", value, _fileSize);
			}

			if (value >= _fileSize)
			{
				if (!_consoleCanReportProgress)
				{
					Console.Write("{0} of {1} uploaded.", value, _fileSize);
				}

				Console.Write("\n");
			}
		}
	}
}