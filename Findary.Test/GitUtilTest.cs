using System;
using System.Collections.Generic;
using System.Diagnostics.Abstractions;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using DotNet.Globbing;
using Findary.Abstractions;
using Moq;
using NUnit.Framework;

namespace Findary.Test
{
    public class GitUtilTest
    {
        private GitUtil _gitUtil;

        [SetUp]
        public void Setup()
        {
        }

        private Options GetDefaultOptions()
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
        public void TestGetGitLfsArgumentsWindows()
        {
            var mockOperatingSystem = new Mock<IOperatingSystem>();
            mockOperatingSystem.Setup(p => p.IsWindows()).Returns(true);

            _gitUtil = new GitUtil(GetDefaultOptions(), null, null, mockOperatingSystem.Object);
            var result = _gitUtil.GetGitLfsArguments("test");
            var commandStringBuilder = new StringBuilder();

            commandStringBuilder.Append("lfs ");
            commandStringBuilder.Append("test");
            Assert.AreEqual(commandStringBuilder.ToString(), result);
        }

        [Test]
        public void TestGetGitLfsArgumentsOther()
        {
            var mockOperatingSystem = new Mock<IOperatingSystem>();
            mockOperatingSystem.Setup(p => p.IsWindows()).Returns(false);
            _gitUtil = new GitUtil(GetDefaultOptions(), null, null, mockOperatingSystem.Object);
            var result = _gitUtil.GetGitLfsArguments("test");
            const string expected = "test";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitLfsArgumentsExecuteInDirectory()
        {
            var result = _gitUtil.GetGitLfsArguments("test", true);
            var commandStringBuilder = new StringBuilder("-C  ");
            if (OperatingSystem.IsWindows())
            {
                commandStringBuilder.Append("lfs ");
            }
            commandStringBuilder.Append("test");
            Assert.AreEqual(commandStringBuilder.ToString(), result);
        }

        [Test]
        public void TestGetGitFilenameWindows()
        {
            var mockOs = new Mock<IOperatingSystem>();
            mockOs.Setup(p => p.IsWindows()).Returns(true);
            _gitUtil = new GitUtil(GetDefaultOptions(), null, null, mockOs.Object);
            var result = _gitUtil.GetGitFilename();
            const string expected = "git.exe";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitFilenameOther()
        {
            var mockOs = new Mock<IOperatingSystem>();
            mockOs.Setup(p => p.IsWindows()).Returns(false);
            _gitUtil = new GitUtil(GetDefaultOptions(), null, null, mockOs.Object);
            var result = _gitUtil.GetGitFilename();
            const string expected = "git";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitLfsFilenameWindows()
        {
            var mockOs = new Mock<IOperatingSystem>();
            mockOs.Setup(p => p.IsWindows()).Returns(true);
            _gitUtil = new GitUtil(GetDefaultOptions(), null, null, mockOs.Object);
            var result = _gitUtil.GetGitLfsFilename();
            const string expected = "git.exe";
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetGitLfsFilenameOther()
        {
            var mockOs = new Mock<IOperatingSystem>();
            mockOs.Setup(p => p.IsWindows()).Returns(false);
            _gitUtil = new GitUtil(GetDefaultOptions(), null, null, mockOs.Object);
            var result = _gitUtil.GetGitLfsFilename();
            const string expected = "git-lfs";
            Assert.AreEqual(expected, result);
        }
    }
}