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
        private readonly StatisticsDao _statistics = new StatisticsDao();

        private GitUtil _gitUtil;
        private List<Glob> _ignoreGlobs;
        private Options _options;
        private bool _hasReachedGitDir;

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public void Run(Options options)
        {
            _options = options;

            InitLogConfig();
            _gitUtil = new GitUtil(options);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ignoreGlobs = GetGlobs(options.Directory);
            _logger.Debug("Found " + _ignoreGlobs.Count + " .gitignore globs");
            ProcessDirectory(options.Directory);

            if (_options.MeasureTime)
            {
                var seconds = stopwatch.ElapsedMilliseconds * 0.001F;
                _logger.Info("Time spent reading:" + seconds + "s");
            }

            // Sort results
            _binaryFileExtensions.Sort();
            _binaryFiles.Sort();

            // Print results
            _binaryFileExtensions.ForEach(_logger.Info);
            _binaryFiles.ForEach(_logger.Info);

            stopwatch.Restart();
            _gitUtil.TrackFiles(_binaryFileExtensions, _binaryFiles);

            if (_options.MeasureTime)
            {
                var seconds = stopwatch.ElapsedMilliseconds * 0.001F;
                _logger.Info("Time spent tracking: " + seconds + "s");
            }

            _logger.Debug(_statistics.Directories.ToString());
            _logger.Debug(_statistics.Files.ToString());
            _logger.Debug("Ignored files: " + _statistics.IgnoredFiles);
            _logger.Debug("Binaries: " + _binaryFileExtensions.Count + " types, " + _binaryFiles.Count + " files");
        }

        private static (string, string) GetFormattedFileExtension(string file)
        {
            var fileExtension = Path.GetExtension(file);
            return string.IsNullOrEmpty(fileExtension) ? (null, null) : (fileExtension.ToLower()[1..], fileExtension);
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

        private List<Glob> GetGlobs(string directory)
        {
            var result = new List<Glob>();
            const string filename = ".gitignore";
            var filePath = Path.Combine(directory, filename);
            if (!File.Exists(filePath))
            {
                _logger.Debug("Could not find file " + filename);
                return result;
            }

            string[] content;
            try
            {
                content = File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                _logger.Warn("Could not read file " + filename + ": " + e.Message);
                return result;
            }

            foreach (var line in content)
            {
                var lineTrimmed = line.TrimStart();
                if (!lineTrimmed.StartsWith('#') && !string.IsNullOrEmpty(lineTrimmed))
                {
                    result.Add(Glob.Parse(lineTrimmed));
                }
            }
            return result;
        }

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

        private bool IsIgnored(string file) => _options.ExcludeGitignore && _ignoreGlobs.Any(p => p.IsMatch(file));

        private void ProcessDirectoriesRecursively(string directory)
        {
            if (!_options.Recursive)
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
                if (IsIgnored(originalExtension))
                {
                    ++_statistics.IgnoredFiles;
                    _logger.Debug("Found .gitignore match for file: " + file);
                    continue;
                }
                if (formattedExtension == null) // File has no extension
                {
                    if (IsFileBinary(file))
                    {
                        var relativePath = Path.GetFullPath(file).Replace(Path.GetFullPath(_options.Directory), string.Empty);
                        while (relativePath.StartsWith('\\'))
                        {
                            if (relativePath.Length <= 1)
                            {
                                continue;
                            }
                            relativePath = relativePath[1..].Replace('\\', '/');
                            _binaryFiles.Add(relativePath);
                        }
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
    }
}