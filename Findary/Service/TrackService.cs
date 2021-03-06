using Findary.Abstraction;
using NLog;
using System;
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
        private readonly IOperatingSystem _operatingSystem;
        private readonly Options _options;
        private readonly ScanService _scanService;
        private readonly StatisticsDao _statistics;
        private readonly Stopwatch _triggerStopwatch = new();

        private int _counterTrackGlobs;
        private int _counterTrackLater;

        public TrackService(Options options, bool isExtension, IOperatingSystem operatingSystem, ScanService scanService = null, StatisticsDao statistics = null,
                            IFileSystem fileSystem = null)
        {
            _options = options;
            _isExtension = isExtension;
            var fileSystemObject = fileSystem ?? new FileSystem();
            _gitUtil = new GitUtil(options, fileSystemObject);
            _statistics = statistics ?? new StatisticsDao();
            _scanService = scanService ?? new ScanService(options, _statistics, fileSystemObject);
            _operatingSystem = operatingSystem;
        }

        public ThreadSafeBool IsRunning { get; set; } = new();
        public IProcess Process { get; set; } = new ProcessWrapper();
        public Stopwatch Stopwatch { get; set; } = new();
        public void PrintTimeSpent()
        {
            if (!_options.MeasureTime)
            {
                return;
            }
            var seconds = Stopwatch.ElapsedMilliseconds * 0.001F;
            _logger.Info("Time spent tracking: " + seconds + 's');
        }

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
            while (_scanService.IsRunning.Value || !queue.IsEmpty || items.Count > 0)
            {
                if (!queue.IsEmpty)
                {
                    var isRunning = _scanService.IsRunning.Value;
                    queue.TryDequeue(out var result);
                    if (result == null)
                    {
                        continue;
                    }

                    var newCommandLength = CalculateCommandLength(commandLength, result);
                    bool TrackLater() => isRunning && newCommandLength < Extensions.MaximumChars;

                    if (TrackLater() || items.Count == 0)
                    {
                        commandLength = newCommandLength;
                        ++_counterTrackLater;
                        _logger.Debug("Executing " + nameof(TrackLater) + ": " + _counterTrackLater + " - current command length: " + commandLength);
                    }
                    else
                    {
                        ++_counterTrackGlobs;
                        _logger.Debug("Executing " + nameof(TrackGlobs) + ": " + _counterTrackGlobs + " - current command length: " + commandLength);

                        _triggerStopwatch.Restart();
                        TrackGlobs(items, lfsCommand.Length);
                        commandLength = lfsCommand.Length;
                        commandLength = CalculateCommandLength(commandLength, result);
                    }
                    items.Add(result);
                }

                if (!_scanService.IsRunning.Value && (commandLength >= Extensions.MaximumChars || queue.IsEmpty))
                {
                    TrackGlobs(items, lfsCommand.Length);
                    commandLength = lfsCommand.Length;
                }
            }
            _logger.Debug("Stopping track service at time " + DateTime.Now.ToString("hh:mm:ss.ffffff"));
            PrintTimeSpent();
            IsRunning.Value = false;
        }

        private static int CalculateCommandLength(int commandLength, string result)
                            => commandLength + result.Length + 3;
        private string GetLfsCommand() => Path.Combine(GitUtil.GitDirectory ?? GitUtil.GitBareFileName, GitUtil.GetGitFilename(_operatingSystem)) +
                                          " lfs track -C " + Path.GetFullPath(_options.Directory);

        private void TrackGlobs(List<string> items, int commandPrefixLength)
        {
            _gitUtil.TrackGlobs(_isExtension, items, _statistics, commandPrefixLength, Process);
            items.Clear();
        }
    }
}
