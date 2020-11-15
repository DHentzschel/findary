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
    }
}
