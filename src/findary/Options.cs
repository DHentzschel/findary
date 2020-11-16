using CommandLine;

namespace Findary
{
    public class Options
    {
        [Option('d', "directory", HelpText = "Set directory to process", Required = true)]
        public string Directory { get; set; }

        [Option('e', "excludeGitignore", HelpText = "Set whether .gitignore should be excluded. The directory should contain a file .gitignore")]
        public bool ExcludeGitignore { get; set; }

        [Option('m', "measure", HelpText = "Set whether measured time should be printed")]
        public bool MeasureTime { get; set; }

        [Option('r', "recursive", HelpText = "Set whether directory should be processed recursively")]
        public bool Recursive { get; set; }

        [Option('s', "stats", HelpText = "Set whether statistics should be printed")]
        public bool Stats { get; set; }

        [Option('t', "track", HelpText = "Set whether files should be tracked in LFS automatically")]
        public bool Track { get; set; }

        [Option('v', "verbose", HelpText = "Set whether the output should be verbose")]
        public bool Verbose { get; set; }
    }
}