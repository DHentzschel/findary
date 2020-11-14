using DotNet.Globbing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace findary
{
    public class Findary
    {
        private readonly List<string> _binaryFileExtensions = new List<string>();
        private readonly List<string> _binaryFiles = new List<string>();

        private List<Glob> _ignoreGlobs;

        private Options _options;

        public void Run(Options options)
        {
            _options = options;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ignoreGlobs = GetGlobs(options.Directory);
            //todo read gitattributes
            ProcessDirectory(options.Directory);
            PrintVerbosely("Time elapsed reading: " + stopwatch.ElapsedMilliseconds + "ms");

            _binaryFileExtensions.Sort();
            _binaryFiles.Sort();
            _binaryFileExtensions.ForEach(Console.WriteLine);

            stopwatch.Restart();
            TrackFiles();
            PrintVerbosely("Time elapsed tracking: " + stopwatch.ElapsedMilliseconds + "ms");
        }

        private void TrackFiles()
        {
            if (!_options.Track || !IsGitAvailable() || !InstallGitLfs())
            {
                return;
            }

            TrackFiles(_binaryFileExtensions.Concat("*."));
            TrackFiles(_binaryFiles.Concat(string.Empty));
        }

        private void TrackFiles(string arguments)
        {
            var output = GetNewProcessOutput(GetGitLfsFilename(), GetGitLfsArguments("track " + arguments, true));
            var lines = output.Split('\n');
            var penultimateLine = lines.Length > 1 ? lines[^2] : string.Empty;
            if (output.StartsWith("Tracking \"") || penultimateLine.EndsWith("already supported"))
            {
                return;
            }
            PrintVerbosely("Could not track files. Process output is: " + output, true);
        }

        private static string GetFormattedFileExtension(string file)
        {
            var fileExtension = Path.GetExtension(file);
            return string.IsNullOrEmpty(fileExtension) ? null : fileExtension.ToLower()[1..];
        }

        private static string GetGitFilename() => "git";

        private string GetGitLfsArguments(string args, bool executeInRepository = false)
        {
            var result = "";
            if (executeInRepository)
            {
                result = "-C " + _options.Directory + ' ';
            }
            result += (OperatingSystem.IsWindows() ? "lfs " : string.Empty) + args;
            return result;
        }

        private static string GetGitLfsFilename() => GetGitFilename() + (!OperatingSystem.IsWindows() ? "-lfs" : string.Empty);

        private static bool IsFileBinary(string filePath)
        {
            FileStream fileStream;
            try
            {
                fileStream = File.OpenRead(filePath);
            }
            catch (Exception e)
            {
                PrintVerbosely("Could not read file " + filePath + ". " + e.Message);
                return false;
            }

            var bytes = new byte[1024];
            int bytesRead;
            var isFirstBlock = true;
            while ((bytesRead = fileStream.Read(bytes, 0, bytes.Length)) > 0)
            {
                if (isFirstBlock && bytes.HasBom())
                {
                    return false;
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

        private List<Glob> GetGlobs(string directory)
        {
            var result = new List<Glob>();
            var filePath = Path.Combine(directory, ".gitignore");
            if (!File.Exists(filePath))
            {
                PrintVerbosely("Could not find file .gitignore");
                return result;
            }

            string[] content;
            try
            {
                content = File.ReadAllLines(Path.Combine(directory, ".gitignore"));
            }
            catch (Exception e)
            {
                PrintVerbosely("Could not read file .gitignore: " + e.Message, true);
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
                PrintVerbosely("Could not start process " + filename + ". " + e.Message, true);
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
                    PrintVerbosely("Could not redirect standard output. " + e.Message, true);
                }
            }
            return output;
        }

        private bool InstallGitLfs()
        {
            var output = GetNewProcessOutput(GetGitLfsFilename(), GetGitLfsArguments("install", true));
            return output.EndsWith("Git LFS initialized.");
        }

        private bool IsGitAvailable()
        {
            const string arguments = "version";
            var gitInstalled = IsInstalled(GetGitFilename(), arguments, "git version");
            return gitInstalled && IsInstalled(GetGitLfsFilename(), GetGitLfsArguments(arguments), "git-lfs/");
        }
        private bool IsIgnored(string file) => _options.ExcludeGitignore && _ignoreGlobs.Any(p => p.IsMatch(file));

        private bool IsInstalled(string filename, string arguments, string outputPrefix)
        {
            var output = GetNewProcessOutput(filename, arguments);
            if (output?.StartsWith(outputPrefix) == true)
            {
                return true;
            }
            PrintVerbosely("Could not detect a installed version of " + filename, true);
            return false;
        }
        private void PrintVerbosely(string message, bool isError = false)
        {
            if (!_options.Verbose)
            {
                return;
            }
            var textWriter = isError ? Console.Error : Console.Out;
            textWriter.WriteLine(message);
        }

        private void ProcessDirectoriesRecursively(string directory)
        {
            if (!_options.Recursive)
            {
                return;
            }
            if (!Directory.Exists(directory))
            {
                PrintVerbosely("Could not find directory: " + directory, true);
                return;
            }

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(directory);
            }
            catch (Exception e)
            {
                PrintVerbosely("Could not enumerate directories in directory " + directory + ". " + e.Message);
                return;
            }

            foreach (var dir in directories)
            {
                ProcessDirectory(dir);
            }
        }

        private void ProcessDirectory(string directory)
        {
            ProcessDirectoriesRecursively(directory);
            ProcessFiles(directory);
        }

        private void ProcessFiles(string directory)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory);
            }
            catch (Exception e)
            {
                PrintVerbosely("Could not enumerate files in directory " + directory + ". " + e.Message);
                return;
            }

            foreach (var file in files)
            {
                if (IsIgnored(file))
                {
                    PrintVerbosely("Found .gitignore match. Continuing..");
                    continue;
                }

                var fileExtension = GetFormattedFileExtension(file);
                
                if (fileExtension == null) // File has no extension
                {
                    if (IsFileBinary(file))
                    {
                        var relativePath = Path.GetFullPath(file).Replace(Path.GetFullPath(_options.Directory), string.Empty);
                        while (relativePath.StartsWith('\\'))
                        {
                            if (relativePath.Length > 1)
                            {
                                relativePath = relativePath[1..].Replace('\\', '/');
                                _binaryFiles.Add(relativePath);
                                // TODO
                            }
                        }
                    }
                    continue;
                }

                if (!ShouldBeAdded(fileExtension, file))
                {
                    continue;
                }

                _binaryFileExtensions.Add(fileExtension);

                var message = "Added file type " + fileExtension.ToUpper() + " from file path: " + file;
                PrintVerbosely(message);
            }
        }
        private bool ShouldBeAdded(string fileExtension, string file)
        {
            if (fileExtension == null)
            {
                return false;
            }
            return !_binaryFileExtensions.Contains(fileExtension) && IsFileBinary(file);
        }
    }
}