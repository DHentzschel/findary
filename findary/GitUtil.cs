using DotNet.Globbing;
using Findary.Abstraction;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Abstractions;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Findary
{
    public class GitUtil
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly Options _options;

        private readonly IFileSystem _fileSystem;
        private IProcess _process;
        private readonly IOperatingSystem _operatingSystem;
        private bool _isGitAvailable;
        private bool _isFirstCall = true;

        public GitUtil(Options options, IFileSystem fileSystem = null, /*IProcess process = null,*/ IOperatingSystem operatingSystem = null)
        {
            _options = options;
            _fileSystem = fileSystem ?? new FileSystem();
            //_process = process ?? new ProcessWrapper();
            _operatingSystem = operatingSystem ?? new OperatingSystemWrapper();
            //_isGitAvailable = IsGitAvailable();
        }

        public static string GitDirectory { get; } = GetGitDirectory(Logger, new FileSystem(), new OperatingSystemWrapper());

        public static string GetGitFilename(IOperatingSystem operatingSystem) => "git" + GetPlatformSpecific(".exe", string.Empty, operatingSystem);

        public static string GetGitLfsFilename(IOperatingSystem operatingSystem) => GetGitFilename(operatingSystem) + GetPlatformSpecific(string.Empty, "-lfs", operatingSystem);

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
            var lines = GetGitIgnoreLines();
            foreach (var line in lines)
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

        public string GetGitLfsArguments(string args, IOperatingSystem operatingSystem, bool executeInRepository = false)
        {
            var result = string.Empty;
            if (executeInRepository)
            {
                result = "-C " + _options.Directory + ' ';
            }

            result += GetPlatformSpecific("lfs ", string.Empty, operatingSystem) + args;
            return result;
        }

        public void TrackGlobs(bool isExtension, List<string> globs, StatisticsDao statistics, int commandPrefixLength, IProcess process)
        {
            if (_process != process)
            {
                _process = process;
                _isGitAvailable = IsGitAvailable(process, _operatingSystem);
            }
            if (!_options.Track || !_isGitAvailable || !InitGitLfs(process, _operatingSystem))
            {
                if (_options.Track && _isFirstCall)
                {
                    _isFirstCall = false;
                    var addendum = !_isGitAvailable ? "git is not available" : "lfs could not be initialized";
                    Logger.Error("Could not track files - " + addendum);
                }
                return;
            }

            if (globs.Count < 1)
            {
                return;
            }

            var prefix = isExtension ? "*." : string.Empty;
            var parameters = globs.ToParamList(prefix);
            var parameterLists = parameters.Split(commandPrefixLength);
            parameterLists.ForEach(p => TrackFiles(p, statistics, _operatingSystem));
        }

        public static string GetGitDirectory(ILogger logger, IFileSystem fileSystem, IOperatingSystem operatingSystem)
        {
            var pathVariable = Environment.GetEnvironmentVariable("path");
            if (pathVariable == null)
            {
                return null;
            }

            var directories = pathVariable.Split(';');
            foreach (var directory in directories)
            {
                var filePath = Path.Combine(directory, GetGitFilename(operatingSystem));
                if (fileSystem.File.Exists(filePath))
                {
                    return directory;
                }
            }
            return null;
        }

        private static string GetPlatformSpecific(string windows, string other, IOperatingSystem operatingSystem) => operatingSystem.IsWindows() ? windows : other;

        private List<string> GetFileLines(string directory, string filename)
        {
            var result = new List<string>();
            var filePath = Path.Combine(directory, filename);
            if (!_fileSystem.File.Exists(filePath))
            {
                Logger.Info("Could not find file " + filename);
                return result;
            }

            try
            {
                result = _fileSystem.File.ReadAllLines(filePath).ToList();
            }
            catch (Exception e)
            {
                Logger.Warn("Could not read file " + filename + ": " + e.Message);
                return result;
            }

            return result;
        }

        private List<string> GetGitAttributesLines()
            => !_options.Track ? new List<string>() : GetFileLines(_options.Directory, ".gitattributes");

        private List<string> GetGitIgnoreLines()
            => !_options.IgnoreFiles ? new List<string>() : GetFileLines(_options.Directory, ".gitignore");

        private static string GetNewProcessOutput(string filename, string arguments, IProcess process = null)
        {
            process ??= new ProcessWrapper();
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            process.StartInfo = startInfo;
            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Could not start process " + filename + ". " + e.Message);
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
                    Logger.Error("Could not redirect standard output. " + e.Message);
                }
            }

            var exitCode = process.ExitCode;
            process.Close();
            if (exitCode != 2)
            {
                return output;
            }
            Logger.Error("Access is probably denied. Exit code " + process.ExitCode + " (try again with admin privileges)");
            return null;
        }

        private bool InitGitLfs(IProcess process, IOperatingSystem operatingSystem)
        {
            var output = GetNewProcessOutput(GetGitLfsFilename(operatingSystem), GetGitLfsArguments("install", operatingSystem, true), process);
            return output?.EndsWith("Git LFS initialized.") == true;
        }

        private bool IsGitAvailable(IProcess process, IOperatingSystem operatingSystem)
        {
            const string arguments = "version";
            return IsGitInstalled(arguments, process, operatingSystem) && IsGitLfsInstalled(arguments, process, operatingSystem);
        }

        private bool IsGitInstalled(string arguments, IProcess process, IOperatingSystem operatingSystem) => IsInstalled(GetGitFilename(operatingSystem), arguments, "git version", process);

        private bool IsGitLfsInstalled(string arguments, IProcess process, IOperatingSystem operatingSystem) => IsInstalled(GetGitLfsFilename(operatingSystem), GetGitLfsArguments(arguments, operatingSystem), "git-lfs/", process);

        private static bool IsInstalled(string filename, string arguments, string outputPrefix, IProcess process)
        {
            var output = GetNewProcessOutput(filename, arguments, process);
            if (output?.StartsWith(outputPrefix) == true)
            {
                return true;
            }
            Logger.Warn("Could not detect a installed version of " + filename);
            return false;
        }

        private static Glob ParseGlob(string line, bool isGitIgnore)
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

            var isFolder = resultString.EndsWith('/');
            if (resultString.StartsWith("*.") || isFolder)
            {
                resultString = "**/" + resultString;
                if (isFolder)
                {
                    resultString += "**";
                }
            }
            return Glob.Parse(resultString);
        }

        private void TrackFiles(string arguments, StatisticsDao statistics, IOperatingSystem operatingSystem)
        {
            if (arguments.Length == 0)
            {
                return;
            }

            var output = GetNewProcessOutput(GetGitLfsFilename(operatingSystem), GetGitLfsArguments("track " + arguments, operatingSystem, true));
            if (output == null)
            {
                return;
            }

            var trackingCount = output.Count("Tracking \"");
            var alreadySupportedCount = output.Count("already supported");

            if (trackingCount > 0 || alreadySupportedCount > 0)
            {
                statistics.TrackedFiles.Value += (uint)trackingCount;
                statistics.AlreadySupported.Value += (uint)alreadySupportedCount;
                return;
            }
            Logger.Error("Could not track files. Process output is: " + output);
        }
    }
}
