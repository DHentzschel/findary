using System.Diagnostics;

namespace Findary.Service
{
    public interface IService
    {
        public Stopwatch Stopwatch { get; set; }

        void Run();

        void PrintTimeSpent();
    }
}