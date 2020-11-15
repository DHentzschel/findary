using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Findary
{
    public class GitUtil
    {
        private readonly Logger _logger;
        private readonly Options _options;

        public GitUtil(Options options)
        {
            _options = options;
            _logger = new Logger(options);
        }

        public static string GetGitFilename() => "git";

        public static string GetGitLfsFilename() => GetGitFilename() + (!OperatingSystem.IsWindows() ? "-lfs" : string.Empty);

        public string GetGitLfsArguments(string args, bool executeInRepository = false)
        {
            var result = "";
            if (executeInRepository)
            {
                result = "-C " + _options.Directory + ' ';
            }
            result += (OperatingSystem.IsWindows() ? "lfs " : string.Empty) + args;
            return result;
        }

        public void TrackFiles(List<string> fileExtensions, List<string> files)
        {
            if (!_options.Track || !IsGitAvailable() || !InitGitLfs())
            {
                Console.Error.WriteLine("Could not track files");
                return;
            }

            var commandLength = "git lfs track -C ".Length + _options.Directory.Length;

            var concatArguments = fileExtensions.Concat("*.", commandLength);
            Console.WriteLine("Tracking extensions: " + string.Join("\n", concatArguments));
            concatArguments.ForEach(TrackFiles);
            Console.WriteLine("Tracked extensions");

            concatArguments = files.Concat(string.Empty, commandLength);
            Console.WriteLine("Tracking files: " + string.Join("\n", concatArguments));
            concatArguments.ForEach(TrackFiles);
            Console.WriteLine("Tracked files");
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
                _logger.PrintVerbosely("Could not start process " + filename + ". " + e.Message, true);
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
                    _logger.PrintVerbosely("Could not redirect standard output. " + e.Message, true);
                }
            }
            return output;
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
            _logger.PrintVerbosely("Could not detect a installed version of " + filename, true);
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
            _logger.PrintVerbosely("Could not track files. Process output is: " + output, true);
        }
    }
}
