using CommandLine;
using Findary.Abstraction;
using Findary.Service;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Reflection;
using System.Threading;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Findary
{
    public class Findary
    {
        private const string VersionSuffix = "-pre1";

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IOperatingSystem _operatingSystem = new OperatingSystemWrapper();
        private readonly Options _options;
        private readonly StatisticsDao _statistics = new();
        private readonly Stopwatch _stopwatch = new();

        private LogLevel _logLevel;

        public Findary(Options options)
        {
            _options = options;
            InitLogConfig();
        }

        public void Run()
        {
            if (_options.PrintVersion)
            {
                PrintVersion(_options);
                return;
            }

            _options.Directory ??= AppDomain.CurrentDomain.BaseDirectory;

            StartServices();
        }
        
        private static void WaitUntilEnd(IService scanService, IService trackService)
        {
            while (scanService.IsRunning.Value || trackService.IsRunning.Value)
            {
            }
        }

        private void InitLogConfig()
        {
            var loggingConfiguration = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget
            {
                Name = "console",
                Layout = "${message}"
            };
            _logLevel = _options.Verbose ? LogLevel.Debug : LogLevel.Info;
            loggingConfiguration.AddRule(_logLevel, LogLevel.Fatal, consoleTarget);
            LogManager.Configuration = loggingConfiguration;
        }

        private void PrintStatistics(ScanService scanService)
        {
            var logLevel = _options.Stats ? LogLevel.Info : LogLevel.Debug;
            _logger.Log(logLevel, _statistics.Directories.ToString());
            _logger.Log(logLevel, _statistics.Files.ToString());
            _logger.Log(logLevel, "Ignored files: " + _statistics.IgnoredFiles.Value);
            var message = "Binaries: " + scanService.FinalFileExtensionList.Count + " types, " +
                          scanService.FinalFileList.Count
                          + " files (" + _statistics.TrackedFiles.Value + " tracked new, " +
                          _statistics.AlreadySupported.Value +
                          " already supported)";
            _logger.Log(logLevel, message);
        }

        private void PrintVersion(Options options)
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var appName = GetType().Assembly.GetName();
            var message = appName.Name + ' ' + version + VersionSuffix;
            if (options.Verbose || _logLevel.Name == LogLevel.Debug.Name)
            {
                _logger.Debug(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        private ScanService StartScanService()
        {
            _stopwatch.Start();
            var result = new ScanService(_options, _statistics);
            var scanThread = new Thread(result.Run);
            scanThread.Start();
            return result;
        }

        private void StartServices()
        {
            var scanService = StartScanService();
            var trackFileService = StartTrackService(scanService);

            StartTrackService(scanService, true, false);

            if (!ScanService.FileQueue.IsEmpty)
            {
                StartTrackService(scanService, false, false);
            }

            // Just to be sure
            if (!ScanService.FileExtensionQueue.IsEmpty)
            {
                StartTrackService(scanService, true, false);
            }

            WaitUntilEnd(scanService, trackFileService);
            PrintStatistics(scanService);
        }

        private TrackService StartTrackService(ScanService scanService, bool isExtensionService = false, bool startThread = true)
        {
            _stopwatch.Restart();
            var result = new TrackService(_options, isExtensionService, _operatingSystem, scanService, _statistics);

            if (startThread)
            {
                var trackFileThread = new Thread(result.Run);
                trackFileThread.Start();
            }
            else
            {
                result.Run();
            }
            return result;
        }
    }
}