using DotNet.Globbing;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Abstractions;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Findary.Abstractions;

namespace Findary
{
    public class GitUtil
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;

        private readonly IFileSystem _fileSystem;
        private readonly IProcess _process;
        private readonly IOperatingSystem _operatingSystem;

        public GitUtil(Options options, IFileSystem fileSystem = null, IProcess process = null, IOperatingSystem operatingSystem = null)
        {
            _options = options;
            _fileSystem = fileSystem ?? new FileSystem();
            _process = process ?? new ProcessWrapper();
            _operatingSystem = operatingSystem ?? new OperatingSystemWrapper();
        }

        public string GetGitFilename() => "git" + GetPlatformSpecific(".exe", string.Empty);

        public string GetGitLfsFilename() => GetGitFilename() + GetPlatformSpecific(string.Empty, "-lfs");

        public List<Glob> GetGitAttributesGlobs()
        {
            var result = new List<Glob>();
            foreach (var line in GetGitAttributesLines())
            {
                var lineTrimmed = line.TrimStart();
                if (string.IsNullOrEmpty(lineTrimmed) || lineTrimmed.IsGlobComment())
                {
                    continue;
                }

                var parsedGlob = ParseGlob(lineTrimmed, false);
                if (parsedGlob != null)
                {
                    result.Add(parsedGlob);
                }
            }
            return result;
        }

        public List<Glob> GetGitIgnoreGlobs()
        {
            var result = new List<Glob>();
            foreach (var line in GetGitIgnoreLines())
            {
                var lineTrimmed = line.TrimStart();
                if (string.IsNullOrEmpty(lineTrimmed) || lineTrimmed.IsGlobComment())
                {
                    continue;
                }

                var parsedGlob = ParseGlob(lineTrimmed, true);
                if (parsedGlob != null)
                {
                    result.Add(parsedGlob);
                }
            }
            return result;
        }

        public string GetGitLfsArguments(string args, bool executeInRepository = false)
        {
            var result = string.Empty;
            if (executeInRepository)
            {
                result = "-C " + _options.Directory + ' ';
            }

            result += GetPlatformSpecific("lfs ", string.Empty) + args;
            return result;
        }

        public void TrackFiles(List<string> fileExtensions, List<string> files, StatisticsDao statistics)
        {
            var isGitAvailable = IsGitAvailable();

            if (!_options.Track || !isGitAvailable || !InitGitLfs())
            {
                if (_options.Track)
                {
                    var addendum = !isGitAvailable ? ", git is not available" : "lfs could not be initialized";
                    _logger.Error("Could not track files" + addendum);
                }
                return;
            }

            var command = Path.Combine(GetGitDirectory(), GetGitFilename()) + " lfs track -C " + Path.GetFullPath(_options.Directory);
            if (fileExtensions.Count == 0)
            {
                var concatArguments = fileExtensions.Concat("*.", command.Length);
                concatArguments.ForEach(p => TrackFiles(p, statistics));
            }
            if (files.Count > 0)
            {
                var concatArguments = files.Concat(string.Empty, command.Length);
                concatArguments.ForEach(p => TrackFiles(p, statistics));
            }
        }

        private string GetGitDirectory()
        {
            var pathVariable = Environment.GetEnvironmentVariable("path");
            if (pathVariable == null)
            {
                return null;
            }

            var directories = pathVariable.Split(';');
            foreach (var directory in directories)
            {
                var filePath = Path.Combine(directory, GetGitFilename());
                if (_fileSystem.File.Exists(filePath))
                {
                    return directory;
                }
            }
            return null;
        }

        private string GetPlatformSpecific(string windows, string other) => _operatingSystem.IsWindows() ? windows : other;

        private List<string> GetFileLines(string directory, string filename)
        {
            var result = new List<string>();
            var filePath = Path.Combine(directory, filename);
            if (!_fileSystem.File.Exists(filePath))
            {
                _logger.Debug("Could not find file " + filename);
                return result;
            }

            try
            {
                result = _fileSystem.File.ReadAllLines(filePath).ToList();
            }
            catch (Exception e)
            {
                _logger.Warn("Could not read file " + filename + ": " + e.Message);
                return result;
            }

            return result;
        }

        private List<string> GetGitAttributesLines()
            => !_options.Track ? new List<string>() : GetFileLines(_options.Directory, ".gitattributes");

        private List<string> GetGitIgnoreLines()
            => !_options.IgnoreFiles ? new List<string>() : GetFileLines(_options.Directory, ".gitignore");

        private string GetNewProcessOutput(string filename, string arguments)
        {
            _process.StartInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                _process.Start();
            }
            catch (Exception e)
            {
                _logger.Error("Could not start process " + filename + ". " + e.Message);
                return null;
            }


            string output = null;
            while (!_process.StandardOutput.EndOfStream)
            {
                try
                {
                    output += (output == null ? string.Empty : "\n") + _process.StandardOutput.ReadLine();
                }
                catch (Exception e)
                {
                    _logger.Error("Could not redirect standard output. " + e.Message);
                }
            }

            if (_process.ExitCode != 2)
            {
                return output;
            }
            _logger.Error("Access is probably denied. Exit code " + _process.ExitCode + " (try again with admin privileges)");
            return null;
        }

        private bool InitGitLfs()
        {
            var output = GetNewProcessOutput(GetGitLfsFilename(), GetGitLfsArguments("install", true));
            return output?.EndsWith("Git LFS initialized.") == true;
        }

        private bool IsGitAvailable()
        {
            const string arguments = "version";
            return IsGitInstalled(arguments) && IsGitLfsInstalled(arguments);
        }

        private bool IsGitInstalled(string arguments) => IsInstalled(GetGitFilename(), arguments, "git version");

        private bool IsGitLfsInstalled(string arguments) => IsInstalled(GetGitLfsFilename(), GetGitLfsArguments(arguments), "git-lfs/");

        private bool IsInstalled(string filename, string arguments, string outputPrefix)
        {
            var output = GetNewProcessOutput(filename, arguments);
            if (output?.StartsWith(outputPrefix) == true)
            {
                return true;
            }
            _logger.Warn("Could not detect a installed version of " + filename);
            return false;
        }

        private Glob ParseGlob(string line, bool isGitIgnore)
        {
            var lineSplit = line.Split(' ');
            if (!isGitIgnore && lineSplit.Length < 5)
            {
                return null;
            }

            var resultString = string.Empty;
            for (var i = 0; i < lineSplit.Length - 4 || isGitIgnore && i == 0; ++i)
            {
                resultString += lineSplit[i] + (i < lineSplit.Length - 5 ? " " : string.Empty);
            }

            if (resultString.StartsWith("*."))
            {
                resultString = "**/" + resultString;
            }

            return Glob.Parse(resultString);
        }

        private void TrackFiles(string arguments, StatisticsDao statistics)
        {
            if (arguments.Length == 0)
            {
                return;
            }

            var output = GetNewProcessOutput(GetGitLfsFilename(), GetGitLfsArguments("track " + arguments, true));
            if (output == null)
            {
                return;
            }

            var trackingCount = output.Count("Tracking \"");
            var alreadySupportedCount = output.Count("already supported");

            if (trackingCount > 0 || alreadySupportedCount > 0)
            {
                statistics.TrackedFiles += (uint)trackingCount;
                statistics.AlreadySupported += (uint)alreadySupportedCount;
                return;
            }
            _logger.Error("Could not track files. Process output is: " + output);
        }
    }
}
