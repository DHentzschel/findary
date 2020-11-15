using System;

namespace Findary
{
    public class GitUtil
    {
        public static string GetGitFilename() => "git";

        public static string GetGitLfsFilename() => GetGitFilename() + (!OperatingSystem.IsWindows() ? "-lfs" : string.Empty);
    }
}
