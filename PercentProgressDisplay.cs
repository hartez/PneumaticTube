using System;
using System.IO;
using System.Security;

namespace PneumaticTube
{
    internal class PercentProgressDisplay : IProgress<long>
    {
        private readonly bool _consoleCanReportProgress;
        private readonly long _fileSize;

        public PercentProgressDisplay(long fileSize)
        {
            _fileSize = fileSize;
            _consoleCanReportProgress = true;

            try
            {
                // This will throw an exception if we're running in ISE
                var top = Console.CursorTop;
            }
            catch(SecurityException)
            {
                // No permission to mess with the console, 
                _consoleCanReportProgress = false;
            }
            catch(IOException)
            {
                // This console doesn't allow position setting
                _consoleCanReportProgress = false;
            }
        }

        public void Report(long value)
        {
            long percent = 0;

            if(_fileSize > 0)
            {
                percent = 100*value/_fileSize;
            }

            if(_consoleCanReportProgress)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{percent}% complete.");
            }

            if(percent >= 100)
            {
                if(!_consoleCanReportProgress)
                {
                    Console.Write($"{percent}% complete.");
                }

                Console.Write("\n");
            }
        }
    }
}