using System;

namespace Findary
{
    public class Logger
    {
        private readonly Options _options;

        public Logger(Options options)
        {
            _options = options;
        }

        public void PrintTimeElapsed(string task, long deltaMillis)
        {
            if (!_options.MeasureTime)
            {
                return;
            }
            var seconds = deltaMillis * 0.001F;
            Console.WriteLine("Time elapsed " + task + ": " + seconds + "s");
        }

        public void PrintVerbosely(string message, bool isError = false)
        {
            if (!_options.Verbose)
            {
                return;
            }
            var textWriter = isError ? Console.Error : Console.Out;
            textWriter.WriteLine(message);
        }
    }
}
