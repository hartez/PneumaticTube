using System;

namespace PneumaticTube
{
    internal class BytesProgressDisplay : IProgress<long>
    {
        private readonly long _fileSize;

        public BytesProgressDisplay(long fileSize)
        {
            _fileSize = fileSize;
        }

        public void Report(long value)
        {
            Console.SetCursorPosition(0, Console.CursorTop);

            Console.Write("{0} of {1} uploaded.", value, _fileSize);

            if(value >= _fileSize)
            {
                Console.Write("\n");
            }
        }
    }
}