using DotNet.Globbing;
using Findary.FileScan;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Findary.Service
{
    public class ScanService : IService
    {
        private readonly IFileScan _fileScan;
        private readonly IFileSystem _fileSystem;
        private readonly GitUtil _gitUtil;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;
        private readonly StatisticsDao _statistics;

        private bool _hasReachedGitDir;

        public ScanService(Options options, StatisticsDao statistics = null, IFileSystem fileSystem = null, IFileScan fileScan = null)
        {
            _options = options;
            _statistics = statistics ?? new StatisticsDao();
            _fileScan = fileScan ?? new FileScan.FileScan();
            _fileScan.Statistics = _statistics;
            _fileSystem = fileSystem ?? new FileSystem();
            _gitUtil = new GitUtil(options, _fileSystem);
        }

        public static ConcurrentQueue<string> FileExtensionQueue { get; set; } = new();

        public static ConcurrentQueue<string> FileQueue { get; set; } = new();

        public List<Glob> AttributesGlobs { get; set; }

        public List<string> FinalFileExtensionList { get; } = new();

        public List<string> FinalFileList { get; } = new();

        public List<Glob> IgnoreGlobs { get; set; }

        public ThreadSafeBool IsRunning { get; set; } = new();

        public Stopwatch Stopwatch { get; set; } = new();

        public void PrintTimeSpent()
        {
            if (!_options.MeasureTime)
            {
                return;
            }
            var seconds = Stopwatch.ElapsedMilliseconds * 0.001F;
            _logger.Info("Time spent scanning: " + seconds + 's');
        }

        public void Run()
        {
            Stopwatch.Restart();
            _logger.Debug("Starting scan service at time " + DateTime.Now.ToString("hh:mm:ss.ffffff"));
            IsRunning.Value = true;
            PrepareIgnoreGlobs();
            PrepareAttributesGlobs();
            ProcessDirectory(_options.Directory);
            SortResults();
            PrintResults();
            _logger.Debug("Stopping scan service at time " + DateTime.Now.ToString("hh:mm:ss.ffffff"));
            PrintTimeSpent();
            IsRunning.Value = false;
        }
        private static (string, string) GetFormattedFileExtension(string file)
        {
            var fileExtension = Path.GetExtension(file);
            return string.IsNullOrEmpty(fileExtension) ? (null, null) : (fileExtension.ToLower()[1..], fileExtension);
        }

        private string GetRelativePath(string filePath)
        {
            var result = Path.GetFullPath(filePath).Replace(Path.GetFullPath(_options.Directory), string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (result.StartsWith('/') || result.StartsWith('\\'))
            {
                result = result[1..];
            }
            return result;
        }

        private bool IsAlreadySupported(string file) => _options.Track && AttributesGlobs.Any(p => p.IsMatch(file));

        private bool IsIgnored(string file) => _options.IgnoreFiles && IgnoreGlobs.Any(p => p.IsMatch(file));

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
            AttributesGlobs = _gitUtil.GetGitAttributesGlobs();
            LogGlobCount(AttributesGlobs.Count, ".gitattibutes");
        }
        private void PrepareIgnoreGlobs()
        {
            if (!_options.IgnoreFiles)
            {
                return;
            }
            IgnoreGlobs = _gitUtil.GetGitIgnoreGlobs();
            LogGlobCount(IgnoreGlobs.Count, ".gitignore");
        }

        private void PrintResults()
        {
            FinalFileExtensionList.ForEach(_logger.Info);
            FinalFileList.ForEach(_logger.Info);
        }

        private void ProcessDirectoriesRecursively(string directory)
        {
            if (!_options.Recursive && _statistics.Directories.Total.Value > 0)
            {
                return;
            }
            if (!_fileSystem.Directory.Exists(directory))
            {
                _logger.Warn("Could not find directory: " + directory);
                return;
            }

            string[] directories;
            try
            {
                directories = _fileSystem.Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (Exception e)
            {
                _logger.Warn("Could not enumerate directories in directory " + directory + ": " + e.Message);
                if (e is UnauthorizedAccessException)
                {
                    ++_statistics.Directories.AccessDenied.Value;
                }
                return;
            }

            _statistics.Directories.Total.Value += (uint)directories.Length;
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
            ++_statistics.Directories.Processed.Value;
            ProcessDirectoriesRecursively(directory);
            ProcessFiles(directory);
        }

        private void ProcessFiles(string directory)
        {
            string[] files;
            try
            {
                files = _fileSystem.Directory.EnumerateFiles(directory).ToArray();
            }
            catch (Exception e)
            {
                _logger.Warn("Could not enumerate files in directory " + directory + ": " + e.Message);
                return;
            }

            _statistics.Files.Total.Value += (uint)files.Length;
            foreach (var file in files)
            {

                var (formattedExtension, originalExtension) = GetFormattedFileExtension(file);
                var relativePath = GetRelativePath(file).Replace('\\', '/');
                if (IsIgnored(relativePath))
                {
                    ++_statistics.IgnoredFiles.Value;
                    _logger.Debug("Found .gitignore match for file: " + file);
                    continue;
                }

                if (IsAlreadySupported(relativePath))
                {
                    //++_statistics.AlreadySupported.Value;
                    _logger.Debug("Found .gitattributes match for file: " + file);
                    continue;
                }

                if (formattedExtension == null) // File has no extension
                {
                    if (_fileScan.IsFileBinary(file))
                    {
                        relativePath = relativePath.Replace('\\', '/');
                        relativePath = relativePath.StartsWith('/') ? relativePath[1..] : relativePath;
                        if (FinalFileList.Contains(relativePath))
                        {
                            continue;
                        }

                        var absolutePath = Path.Combine(_options.Directory, relativePath).Replace('\\', '/'); ;
                        FileQueue.Enqueue(relativePath);
                        FinalFileList.Add(absolutePath);
                    }
                    continue;
                }

                if (!ShouldBeAdded(formattedExtension, file))
                {
                    continue;
                }

                FileExtensionQueue.Enqueue(formattedExtension);
                FinalFileExtensionList.Add(formattedExtension);
                var message = "Added file type " + formattedExtension.ToUpper() + " from file path: " + file;
                _logger.Debug(message);
            }
        }

        private bool ShouldBeAdded(string fileExtension, string file)
            => fileExtension != null && !FinalFileExtensionList.Contains(fileExtension) && _fileScan.IsFileBinary(file);

        private void SortResults()
        {
            FinalFileExtensionList.Sort();
            FinalFileList.Sort();
        }
    }
}