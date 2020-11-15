using System;

namespace Findary
{
    public class GitUtil
    {
        private readonly Options _options;

        public GitUtil(Options options)
        {
            _options = options;
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

    }
}
