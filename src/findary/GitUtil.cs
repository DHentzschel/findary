using System;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;

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

        public void TrackFiles(List<string> fileExtensions, List<string> files)
        {
            var isGitAvailable = IsGitAvailable();
            if (!_options.Track || !isGitAvailable || !InitGitLfs())
            {
                _logger.Error("Could not track files" + (!isGitAvailable ? ", git is not available" : string.Empty));
                return;
            }

            var commandLength = "git lfs track -C ".Length + _options.Directory.Length;
            var concatArguments = fileExtensions.Concat("*.", commandLength);
            concatArguments.ForEach(TrackFiles);

            concatArguments = files.Concat(string.Empty, commandLength);
            concatArguments.ForEach(TrackFiles);
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

        private void TrackFiles(string arguments)
        {
            var output = GetNewProcessOutput(GetGitLfsFilename(), GetGitLfsArguments("track " + arguments, true));
            if (output == null)
            {
                return;
            }
            var lines = output.Split('\n');
            var penultimateLine = lines.Length > 1 ? lines[^2] : string.Empty;
            if (output.StartsWith("Tracking \"") || penultimateLine.EndsWith("already supported"))
            {
                return;
            }
            _logger.Error("Could not track files. Process output is: " + output);
        }
    }
}
