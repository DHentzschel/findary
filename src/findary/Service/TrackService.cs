using System;
using Findary.Abstraction;
using NLog;
using System.Collections.Generic;
using System.Diagnostics.Abstractions;
using System.IO;
using System.IO.Abstractions;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Findary.Service
{
    public class TrackService : IService
    {
        private readonly GitUtil _gitUtil;
        private readonly bool _isExtension;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;
        private readonly ScanService _scanService;
        private readonly StatisticsDao _statistics;
        private readonly Stopwatch _triggerStopwatch = new Stopwatch();

        public TrackService(Options options, bool isExtension, ScanService scanService = null, StatisticsDao statistics = null,
            IFileSystem fileSystem = null)
        {
            _options = options;
            _isExtension = isExtension;
            var fileSystemObject = fileSystem ?? new FileSystem();
            _gitUtil = new GitUtil(options, fileSystemObject);
            _statistics = statistics ?? new StatisticsDao();
            _scanService = scanService ?? new ScanService(options, _statistics, fileSystemObject);
        }

        public IProcess Process { get; set; } = new ProcessWrapper();

        public ThreadSafeBool IsRunning { get; set; } = new ThreadSafeBool();

        public Stopwatch Stopwatch { get; set; } = new Stopwatch();

        private int _counterTrackLater = 0;

        private int _counterTrackGlobs = 0;

        private int CalculateCommandLength(int commandLength, string result)
            => commandLength + result.Length + 3;

        public void Run()
        {
            _triggerStopwatch.Restart();
            Stopwatch.Restart();
            IsRunning.Value = true;

            _logger.Debug("Starting track service at time " + DateTime.Now.ToString("hh:mm:ss.ffffff"));
            var items = new List<string>();
            var queue = _isExtension ? ScanService.FileExtensionQueue : ScanService.FileQueue;
            var lfsCommand = GetLfsCommand();
            var commandLength = lfsCommand.Length;
            {
                while (!queue.IsEmpty && commandLength < Extensions.MaximumChars)
                {
                    queue.TryDequeue(out var result);
                    if (result == null)
                    {
                        continue;
                    }
                    items.Add(result);
                    commandLength += (isFirstRun ? 2 : 3) + result.Length;
                    isFirstRun = false;
                }
                _gitUtil.TrackGlobs(_isExtension, items, _statistics, commandLength);
                items.Clear();
                // Reset variables
                if (commandLength < Extensions.MaximumChars)
                {
                    commandLength = GetLfsCommand().Length;
                    isFirstRun = true;
                }
            }
            _logger.Debug("Stopping track service at time " + DateTime.Now.ToString("hh:mm:ss.ffffff"));
            PrintTimeSpent();
        }

        public void PrintTimeSpent()
        {
            if (!_options.MeasureTime)
            {
                return;
            }
            var seconds = Stopwatch.ElapsedMilliseconds * 0.001F;
            _logger.Info("Time spent tracking: " + seconds + 's');
        }

        private string GetLfsCommand() => Path.Combine(_gitUtil.GetGitDirectory(), _gitUtil.GetGitFilename()) +
                                          " lfs track -C " + Path.GetFullPath(_options.Directory);
    }
}
