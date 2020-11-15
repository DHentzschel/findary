using DotNet.Globbing;
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
        private Logger _logger;
        private Options _options;

        public void Run(Options options)
        {
            _options = options;
            _logger = new Logger(options);
            _gitUtil = new GitUtil(options);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ignoreGlobs = GetGlobs(options.Directory);
            _logger.PrintVerbosely("Found " + _ignoreGlobs.Count + " .gitignore globs");
            ProcessDirectory(options.Directory);
            _logger.PrintTimeElapsed("reading", stopwatch.ElapsedMilliseconds);

            _binaryFileExtensions.Sort();
            _binaryFiles.Sort();
            _binaryFileExtensions.ForEach(Console.WriteLine);

            stopwatch.Restart();
            _gitUtil.TrackFiles(_binaryFileExtensions, _binaryFiles);
            _logger.PrintTimeElapsed("tracking", stopwatch.ElapsedMilliseconds);

            _logger.PrintVerbosely(_statistics.Directories.ToString());
            _logger.PrintVerbosely(_statistics.Files.ToString());
            _logger.PrintVerbosely("Binaries: " + _binaryFileExtensions.Count + " types, " + _binaryFiles.Count + " files");
        }

        private static (string, string) GetFormattedFileExtension(string file)
        {
            var fileExtension = Path.GetExtension(file);
            return string.IsNullOrEmpty(fileExtension) ? (null, null) : (fileExtension.ToLower()[1..], fileExtension);
        }

        private List<Glob> GetGlobs(string directory)
        {
            var result = new List<Glob>();
            const string filename = ".gitignore";
            var filePath = Path.Combine(directory, filename);
            if (!File.Exists(filePath))
            {
                _logger.PrintVerbosely("Could not find file " + filename);
                return result;
            }

            string[] content;
            try
            {
                content = File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                _logger.PrintVerbosely("Could not read file " + filename + ": " + e.Message, true);
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
                _logger.PrintVerbosely("Could not read file " + filePath + ". " + e.Message);
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
                _logger.PrintVerbosely("Could not find directory: " + directory, true);
                return;
            }

            string[] directories;
            try
            {
                directories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (Exception e)
            {
                _logger.PrintVerbosely("Could not enumerate directories in directory " + directory + ". " + e.Message);
                return;
            }

            _statistics.Directories.Total += directories.Length;
            foreach (var dir in directories)
            {
                if (dir.EndsWith("\\.git") && dir.Replace(directory, string.Empty).Replace("\\.git", string.Empty) == string.Empty)
                {
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
                _logger.PrintVerbosely("Could not enumerate files in directory " + directory + ". " + e.Message);
                return;
            }

            _statistics.Files.Total += files.Length;
            foreach (var file in files)
            {
                var (formattedExtension, originalExtension) = GetFormattedFileExtension(file);
                if (IsIgnored(originalExtension))
                {
                    _logger.PrintVerbosely("Found .gitignore match for file: " + file);
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
                _logger.PrintVerbosely(message);
            }
        }

        private bool ShouldBeAdded(string fileExtension, string file)
            => fileExtension != null && !_binaryFileExtensions.Contains(fileExtension) && IsFileBinary(file);
    }
}