using System;

namespace PneumaticTube
{
    internal class NoProgressDisplay(long fileSize, bool quiet) : IProgress<long>
    {
		public void Report(long value)
        {
            if(value >= fileSize)
            {
                if(!quiet)
                {
                    Console.Write("Finished\n");
                }
            }
        }
    }
}