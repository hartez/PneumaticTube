using System;

namespace PneumaticTube
{
    internal class PercentProgressDisplay : IProgress<long>
    {
        private readonly long _fileSize;

        public PercentProgressDisplay(long fileSize)
        {
            _fileSize = fileSize;
        }

        public void Report(long value)
        {
            long percent = 0;

            if (_fileSize > 0)
            {
                percent = 100 * value / _fileSize;
            }

            Console.SetCursorPosition(0, Console.CursorTop);

            Console.Write("{0}% complete.", percent);

            if (percent >= 100)
            {
                Console.Write("\n");
            }
        }
    }
}