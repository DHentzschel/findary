using System.Diagnostics;

namespace Findary.Service
{
    public interface IService
    {
        public ThreadSafeBool IsRunning { get; set; }

        public Stopwatch Stopwatch { get; set; }

        void Run();

        void PrintTimeSpent();
    }
}