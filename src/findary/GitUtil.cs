using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Findary
{
    public class GitUtil
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;

        public GitUtil(Options options)
        {
            _options = options;
        }

        public static string GetGitFilename() => "git" + GetPlatformSpecific(".exe", string.Empty);

        private static string GetPlatformSpecific(string windows, string other) => OperatingSystem.IsWindows() ? windows : other;

        public static string GetGitLfsFilename() => GetGitFilename() + GetPlatformSpecific(string.Empty, "-lfs");

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

        private static string GetGitDirectory()
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
                if (File.Exists(filePath))
                {
                    return directory;
                }
            }
            return null;
        }

        public void TrackFiles(List<string> fileExtensions, List<string> files, StatisticsDao statistics)
        {
            var isGitAvailable = IsGitAvailable();
            if (!_options.Track || !isGitAvailable || !InitGitLfs())
            {
                _logger.Error("Could not track files" + (!isGitAvailable ? ", git is not available" : string.Empty));
                return;
            }

            var command = Path.Combine(GetGitDirectory(), GetGitFilename()) + " lfs track -C " + Path.GetFullPath(_options.Directory);
            var concatArguments = fileExtensions.Concat("*.", command.Length);
            concatArguments.ForEach(p => TrackFiles(p, statistics));

            concatArguments = files.Concat(string.Empty, command.Length);
            concatArguments.ForEach(p => TrackFiles(p, statistics));
        }

        private string GetNewProcessOutput(string filename, string arguments)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                _logger.Error("Could not start process " + filename + ". " + e.Message);
                return null;
            }


            string output = null;
            while (!process.StandardOutput.EndOfStream)
            {
                try
                {
                    output += (output == null ? string.Empty : "\n") + process.StandardOutput.ReadLine();
                }
                catch (Exception e)
                {
                    _logger.Error("Could not redirect standard output. " + e.Message);
                }
            }

            if (process.ExitCode != 2)
            {
                return output;
            }
            _logger.Error("Access is probably denied. Exit code " + process.ExitCode + " (try again with admin privileges)");
            return null;
        }

        private bool InitGitLfs()
        {
            var output = GetNewProcessOutput(GetGitLfsFilename(), GetGitLfsArguments("install", true));
            return output?.EndsWith("Git LFS initialized.") == true;
        }

        private bool IsGitInstalled(string arguments) => IsInstalled(GetGitFilename(), arguments, "git version");

        private bool IsGitLfsInstalled(string arguments) => IsInstalled(GetGitLfsFilename(), GetGitLfsArguments(arguments), "git-lfs/");

        private bool IsGitAvailable()
        {
            const string arguments = "version";
            return IsGitInstalled(arguments) && IsGitLfsInstalled(arguments);
        }

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

        private List<string> GetGitIgnoreLines()
            => !_options.IgnoreFiles ? new List<string>() : GetFileLines(_options.Directory, ".gitignore");

        private List<string> GetGitAttributesLines()
            => !_options.Track ? new List<string>() : GetFileLines(_options.Directory, ".gitattributes");

        private List<string> GetFileLines(string directory, string filename)
        {
            var result = new List<string>();
            var filePath = Path.Combine(directory, filename);
            if (!File.Exists(filePath))
            {
                _logger.Debug("Could not find file " + filename);
                return result;
            }

            try
            {
                result = File.ReadAllLines(filePath).ToList();
            }
            catch (Exception e)
            {
                _logger.Warn("Could not read file " + filename + ": " + e.Message);
                return result;
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
                result.Add(Glob.Parse(lineTrimmed));
            }
            return result;
        }

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

                var parsedGlob = ParseGlob(lineTrimmed);
                if (parsedGlob != null)
                {
                    result.Add(parsedGlob);
                }
            }
            return result;
        }

        private Glob ParseGlob(string line)
        {
            var lineSplit = line.Split(' ');
            if (lineSplit.Length < 5)
            {
                return null;
            }

            var resultString = string.Empty;
            for (var i = 0; i < lineSplit.Length - 4; ++i)
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
