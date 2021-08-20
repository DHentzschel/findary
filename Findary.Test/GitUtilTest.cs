using DotNet.Globbing;
using Findary.Abstraction;
using Moq;
using NUnit.Framework;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace Findary.Test
{
    public class GitUtilTest
    {
        private GitUtil _gitUtil;
        private Mock<IOperatingSystem> _moqOperatingSystem;
        private Mock<IOperatingSystem> _moqOperatingSystemWindows;

        [SetUp]
        public void Setup()
        {
            _moqOperatingSystem = new Mock<IOperatingSystem>();
            _moqOperatingSystem.Setup(p => p.IsWindows()).Returns(false);

            _moqOperatingSystemWindows = new Mock<IOperatingSystem>();
            _moqOperatingSystemWindows.Setup(p => p.IsWindows()).Returns(true);
        }

        [Test]
        public void TestGetGitAttributesGlobs()
        {
            const string lfsSuffix = " filter=lfs diff=lfs merge=lfs -text";
            var resultGlobs = new[] { "*.txt", "test" };
            var resultArray = new[] { resultGlobs[0] + lfsSuffix, resultGlobs[1] + lfsSuffix };
            resultGlobs[0] = "**/" + resultGlobs[0];
            const string path = ".gitattributes";

            var moqFileSystem = new Mock<IFileSystem>();
            moqFileSystem.Setup(p => p.File.Exists(path)).Returns(true);
            moqFileSystem.Setup(p => p.File.ReadAllLines(path)).Returns(resultArray);
            _gitUtil = new GitUtil(GetDefaultOptions(), moqFileSystem.Object);

            var result = _gitUtil.GetGitAttributesGlobs();
            var expected = resultGlobs.Select(Glob.Parse).ToList();

            Assert.IsTrue(expected.AreEqual(result));
        }

        [Test]
        public void TestGetGitAttributesGlobsComments()
        {
            const string lfsSuffix = " filter=lfs diff=lfs merge=lfs -text";
            const string globString = "**/*.txt";
            var resultGlobs = new[] { globString };
            var resultArray = new[] { "#*.txt", globString + lfsSuffix, " # test" };
            const string path = ".gitattributes";

            var moqFileSystem = new Mock<IFileSystem>();
            moqFileSystem.Setup(p => p.File.Exists(path)).Returns(true);
            moqFileSystem.Setup(p => p.File.ReadAllLines(path)).Returns(resultArray);
            _gitUtil = new GitUtil(GetDefaultOptions(), moqFileSystem.Object);

            var result = _gitUtil.GetGitAttributesGlobs();
            var expected = resultGlobs.Select(Glob.Parse).ToList();

            Assert.IsTrue(expected.AreEqual(result));
        }

        [Test]
        public void TestGetGitFilenameOther()
        {
            var result = GitUtil.GetGitFilename(_moqOperatingSystem.Object);
            const string expected = "git";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitFilenameWindows()
        {
            var result = GitUtil.GetGitFilename(_moqOperatingSystemWindows.Object);
            const string expected = "git.exe";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitIgnoreGlobs()
        {
            var resultGlobs = new[] { "*.txt", "test" };
            var resultArray = new[] { "**/" + resultGlobs[0], resultGlobs[1] };
            const string path = ".gitignore";

            var moqFileSystem = new Mock<IFileSystem>();
            moqFileSystem.Setup(p => p.File.Exists(path)).Returns(true);
            moqFileSystem.Setup(p => p.File.ReadAllLines(path)).Returns(resultArray);
            _gitUtil = new GitUtil(GetDefaultOptions(), moqFileSystem.Object);

            var result = _gitUtil.GetGitIgnoreGlobs();
            var expected = resultArray.Select(Glob.Parse).ToList();

            Assert.IsTrue(expected.AreEqual(result));
        }

        [Test]
        public void TestGetGitIgnoreGlobsComments()
        {
            const string globString = "**/*.txt";
            var resultGlobs = new[] { globString };
            var resultArray = new[] { "#*.txt", globString, " # test" };
            const string path = ".gitignore";

            var moqFileSystem = new Mock<IFileSystem>();
            moqFileSystem.Setup(p => p.File.Exists(path)).Returns(true);
            moqFileSystem.Setup(p => p.File.ReadAllLines(path)).Returns(resultArray);
            _gitUtil = new GitUtil(GetDefaultOptions(), moqFileSystem.Object);

            var result = _gitUtil.GetGitIgnoreGlobs();
            var expected = resultGlobs.Select(Glob.Parse).ToList();

            Assert.IsTrue(expected.AreEqual(result));
        }

        [Test]
        public void TestGetGitLfsArgumentsExecuteInDirectory()
        {
            _gitUtil = new GitUtil(GetDefaultOptions(), null, _moqOperatingSystemWindows.Object);
            var result = _gitUtil.GetGitLfsArguments("test", _moqOperatingSystemWindows.Object, true);
            var commandStringBuilder = new StringBuilder("-C  ");
            if (_moqOperatingSystemWindows.Object.IsWindows())
            {
                commandStringBuilder.Append("lfs ");
            }
            commandStringBuilder.Append("test");
            Assert.AreEqual(commandStringBuilder.ToString(), result);
        }

        [Test]
        public void TestGetGitLfsArgumentsOther()
        {
            _gitUtil = new GitUtil(GetDefaultOptions(), null, _moqOperatingSystem.Object);
            var result = _gitUtil.GetGitLfsArguments("test", _moqOperatingSystem.Object);
            const string expected = "test";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitLfsArgumentsWindows()
        {
            _gitUtil = new GitUtil(GetDefaultOptions(), null, _moqOperatingSystemWindows.Object);
            var result = _gitUtil.GetGitLfsArguments("test", _moqOperatingSystemWindows.Object);
            var commandStringBuilder = new StringBuilder();

            commandStringBuilder.Append("lfs ");
            commandStringBuilder.Append("test");
            Assert.AreEqual(commandStringBuilder.ToString(), result);
        }

        [Test]
        public void TestGetGitLfsFilenameOther()
        {
            var result = GitUtil.GetGitLfsFilename(_moqOperatingSystem.Object);
            const string expected = "git-lfs";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitLfsFilenameWindows()
        {
            var result = GitUtil.GetGitLfsFilename(_moqOperatingSystemWindows.Object);
            const string expected = "git.exe";
            Assert.AreEqual(expected, result);
        }

        private static Options GetDefaultOptions()
        {
            return new Options
            {
                Directory = string.Empty,
                IgnoreFiles = true,
                MeasureTime = false,
                Recursive = true,
                Stats = true,
                Track = true,
                Verbose = false
            };
        }
    }
}