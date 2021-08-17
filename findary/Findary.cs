using System;
using System.Diagnostics.Abstractions;
using System.Threading;
using Findary.Abstraction;
using Findary.Service;
using NLog;
using NLog.Config;
using NLog.Targets;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Findary
{
    public class Findary
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;
        private readonly StatisticsDao _statistics = new();
        private readonly Stopwatch _stopwatch = new();

        public Findary(Options options)
        {
            _options = options;
            InitLogConfig();
        }

        public void Run()
        {  
            var path = @"C:\Program Files\Git\cmd";
            var variable = Environment.GetEnvironmentVariable("path");

            if (variable.Contains(path, StringComparison.CurrentCultureIgnoreCase))
            {
                Environment.SetEnvironmentVariable("path", variable + ";" + path);
                Console.WriteLine("Added git to path variable: " + Environment.GetEnvironmentVariable("path"));
            }

            _stopwatch.Start();

            var scanService = new ScanService(_options, _statistics);
            var scanThread = new Thread(scanService.Run);
            scanThread.Start();

            _stopwatch.Restart();

            var trackFileService = new TrackService(_options, false, scanService, _statistics);
            var trackFileThread = new Thread(trackFileService.Run);
            trackFileThread.Start();

            var trackFileExtensionService = new TrackService(_options, true, scanService, _statistics);
            trackFileExtensionService.Run();

            TrackService trackSupportService;
            if (!ScanService.FileQueue.IsEmpty)
            {
                trackSupportService = new TrackService(_options, false, scanService, _statistics);
                trackSupportService.Run();
            }

            if (ScanService.FileExtensionQueue.IsEmpty)
            {
                trackSupportService = new TrackService(_options, false, scanService, _statistics);
                trackSupportService.Run();
            }

            while (scanService.IsRunning.Value || trackFileService.IsRunning.Value)
            {
            }
            PrintStatistics(scanService);
        }

        private void InitLogConfig()
        {
            var loggingConfiguration = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget
            {
                Name = "console",
                Layout = "${message}"
            };
            var logLevel = _options.Verbose ? LogLevel.Debug : LogLevel.Info;
            loggingConfiguration.AddRule(logLevel, LogLevel.Fatal, consoleTarget);
            LogManager.Configuration = loggingConfiguration;
        }

        private void PrintStatistics(ScanService scanService)
        {
            var logLevel = _options.Stats ? LogLevel.Info : LogLevel.Debug;
            _logger.Log(logLevel, _statistics.Directories.ToString());
            _logger.Log(logLevel, _statistics.Files.ToString());
            _logger.Log(logLevel, "Ignored files: " + _statistics.IgnoredFiles.Value);
            var message = "Binaries: " + scanService.FinalFileExtensionList.Count + " types, " + scanService.FinalFileList.Count
                          + " files (" + _statistics.TrackedFiles.Value + " tracked new, " + _statistics.AlreadySupported.Value +
                          " already supported)";
            _logger.Log(logLevel, message);
        }
    }
}