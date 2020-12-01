using DotNet.Globbing;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Findary
{
    public class Findary
    {
        private readonly List<string> _binaryFileExtensions = new List<string>();
        private readonly List<string> _binaryFiles = new List<string>();
        private readonly GitUtil _gitUtil;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;
        private readonly StatisticsDao _statistics = new StatisticsDao();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private List<Glob> _attributesGlobs;
        private bool _hasReachedGitDir;
        private List<Glob> _ignoreGlobs;
        public Findary(Options options)
        {
            _options = options;
            _gitUtil = new GitUtil(options);
            InitLogConfig();
        }

        public void Run()
        {
            _stopwatch.Start();

            PrepareIgnoreGlobs();
            PrepareAttributesGlobs();
            ProcessDirectory(_options.Directory);

            PrintMeasuredTimeInSeconds("reading");
            HandleResults();
            _stopwatch.Restart();

            _gitUtil.TrackFiles(_binaryFileExtensions, _binaryFiles, _statistics);
            PrintMeasuredTimeInSeconds("tracking");
            PrintStatistics();
        }

        private static (string, string) GetFormattedFileExtension(string file)
        {
            var fileExtension = Path.GetExtension(file);
            return string.IsNullOrEmpty(fileExtension) ? (null, null) : (fileExtension.ToLower()[1..], fileExtension);
        }

        private string GetRelativePath(string filePath)
        {
            var result = Path.GetFullPath(filePath).Replace(Path.GetFullPath(_options.Directory), string.Empty);
            if (result.StartsWith('/') || result.StartsWith('\\'))
            {
                result = result.Substring(1);
            }
            return result;
        }

        private void HandleResults()
        {
            SortResults();
            PrintResults();
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

        private bool IsAlreadySupported(string file) => _options.Track && _attributesGlobs.Any(p => p.IsMatch(file));

        private bool IsFileBinary(string filePath)
        {
            FileStream fileStream;
            try
            {
                fileStream = File.OpenRead(filePath);
            }
            catch (Exception e)
            {
                _logger.Warn("Could not read file " + filePath + ": " + e.Message);
                if (e is UnauthorizedAccessException)
                {
                    ++_statistics.Files.AccessDenied;
                }
                return false;
            }

            var bytes = new byte[1024];
            int bytesRead;
            var isFirstBlock = true;
            while ((bytesRead = fileStream.Read(bytes, 0, bytes.Length)) > 0)
            {
                if (isFirstBlock)
                {
                    ++_statistics.Files.Processed;
                    if (bytes.HasBom())
                    {
                        return false;
                    }
                }

                var zeroIndex = Array.FindIndex(bytes, p => p == '\0');
                if (zeroIndex > -1 && zeroIndex < bytesRead - 1)
                {
                    return true;
                }

                isFirstBlock = false;
            }
            return false;
        }

        private bool IsIgnored(string file) => _options.IgnoreFiles && _ignoreGlobs.Any(p => p.IsMatch(file));
        private void LogGlobCount(int count, string filename)
        {
            if (count > 0)
            {
                _logger.Debug("Found " + count + ' ' + filename + " globs");
            }
        }

        private void PrepareAttributesGlobs()
        {
            if (!_options.Track)
            {
                return;
            }
            _attributesGlobs = _gitUtil.GetGitAttributesGlobs();
            LogGlobCount(_attributesGlobs.Count, ".gitattibutes");
        }

        private void PrepareIgnoreGlobs()
        {
            if (!_options.IgnoreFiles)
            {
                return;
            }
            _ignoreGlobs = _gitUtil.GetGitIgnoreGlobs();
            LogGlobCount(_ignoreGlobs.Count, ".gitignore");
        }

        private void PrintMeasuredTimeInSeconds(string task)
        {
            if (!_options.MeasureTime)
            {
                return;
            }
            var seconds = _stopwatch.ElapsedMilliseconds * 0.001F;
            _logger.Info("Time spent " + task + ": " + seconds + 's');
        }

        private void PrintResults()
        {
            _binaryFileExtensions.ForEach(_logger.Info);
            _binaryFiles.ForEach(_logger.Info);
        }

        private void PrintStatistics()
        {
            var logLevel = _options.Stats ? LogLevel.Info : LogLevel.Debug;
            _logger.Log(logLevel, _statistics.Directories.ToString());
            _logger.Log(logLevel, _statistics.Files.ToString());
            _logger.Log(logLevel, "Ignored files: " + _statistics.IgnoredFiles);
            var message = "Binaries: " + _binaryFileExtensions.Count + " types, " + _binaryFiles.Count
                          + " files (" + _statistics.TrackedFiles + " tracked new, " + _statistics.AlreadySupported +
                          " already supported)";
            _logger.Log(logLevel, message);
        }

        private void ProcessDirectoriesRecursively(string directory)
        {
            if (!_options.Recursive && _statistics.Directories.Total > 0)
            {
                return;
            }
            if (!Directory.Exists(directory))
            {
                _logger.Warn("Could not find directory: " + directory);
                return;
            }

            string[] directories;
            try
            {
                directories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (Exception e)
            {
                _logger.Warn("Could not enumerate directories in directory " + directory + ": " + e.Message);
                if (e is UnauthorizedAccessException)
                {
                    ++_statistics.Directories.AccessDenied;
                }
                return;
            }

            _statistics.Directories.Total += (uint)directories.Length;
            foreach (var dir in directories)
            {
                if (!_hasReachedGitDir && dir.EndsWith("\\.git"))
                {
                    _hasReachedGitDir = true;
                    continue;
                }
                ProcessDirectory(dir);
            }
        }

        private void ProcessDirectory(string directory)
        {
            ++_statistics.Directories.Processed;
            ProcessDirectoriesRecursively(directory);
            ProcessFiles(directory);
        }

        private void ProcessFiles(string directory)
        {
            string[] files;
            try
            {
                files = Directory.EnumerateFiles(directory).ToArray();
            }
            catch (Exception e)
            {
                _logger.Warn("Could not enumerate files in directory " + directory + ": " + e.Message);
                return;
            }

            _statistics.Files.Total += (uint)files.Length;
            foreach (var file in files)
            {

                var (formattedExtension, originalExtension) = GetFormattedFileExtension(file);
                var relativePath = GetRelativePath(file);
                if (IsIgnored(relativePath))
                {
                    ++_statistics.IgnoredFiles;
                    _logger.Debug("Found .gitignore match for file: " + file);
                    continue;
                }

                if (IsAlreadySupported(relativePath))
                {
                    ++_statistics.AlreadySupported;
                    _logger.Debug("Found .gitattributes match for file: " + file);
                    continue;
                }

                if (formattedExtension == null) // File has no extension
                {
                    if (IsFileBinary(file))
                    {
                        relativePath = relativePath.Replace('\\', '/');
                        relativePath = relativePath.StartsWith('/') ? relativePath[1..] : relativePath;
                        _binaryFiles.Add(relativePath);
                    }
                    continue;
                }

                if (!ShouldBeAdded(formattedExtension, file))
                {
                    continue;
                }

                _binaryFileExtensions.Add(formattedExtension);

                var message = "Added file type " + formattedExtension.ToUpper() + " from file path: " + file;
                _logger.Debug(message);
            }
        }

        private bool ShouldBeAdded(string fileExtension, string file)
            => fileExtension != null && !_binaryFileExtensions.Contains(fileExtension) && IsFileBinary(file);

        private void SortResults()
        {
            _binaryFileExtensions.Sort();
            _binaryFiles.Sort();
        }
    }
}