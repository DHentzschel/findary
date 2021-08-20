using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Findary.Service;
using Moq;
using NUnit.Framework;
using System.IO.Abstractions;
using System.Linq;
using Findary.FileScan;

namespace Findary.Test
{
    public class ScanServiceTest
    {
        private Options _options;
        private readonly Mock<IFileSystem> _moqFileSystem = new();

        [SetUp]
        public void Setup()
        {
            _options = new Options { Directory = @"C:\temp\" };

            var gitignoreGlobs = new[] { "*.a", "b" };
            var gitignorePath = Path.Combine(_options.Directory, ".gitignore");
            _moqFileSystem.Setup(p => p.File.Exists(gitignorePath)).Returns(true);
            _moqFileSystem.Setup(p => p.File.ReadAllLines(gitignorePath)).Returns(gitignoreGlobs);

            var gitattributesGlobs = new[] { "*.mp3", "b" };
            var gitattributesPath = Path.Combine(_options.Directory, ".gitattributes");
            _moqFileSystem.Setup(p => p.File.Exists(gitattributesPath)).Returns(true);
            _moqFileSystem.Setup(p => p.File.ReadAllLines(gitattributesPath)).Returns(gitattributesGlobs);
        }

        [Test]
        public void TestRunBinaryAndText()
        {
            var files = new[] { "source.c", "typical.mp3", "findary" };
            _moqFileSystem.Setup(p => p.Directory.Exists(_options.Directory)).Returns(true);
            _moqFileSystem.Setup(p => p.Directory.EnumerateDirectories(_options.Directory))
                .Returns(Array.Empty<string>());
            _moqFileSystem.Setup(p => p.Directory.EnumerateFiles(_options.Directory)).Returns(files);

            var moqFileScan = new Mock<IFileScan>();
            moqFileScan.Setup(p => p.IsFileBinary(files[1])).Returns(true);
            moqFileScan.Setup(p => p.IsFileBinary(files[2])).Returns(true);
            var scanService = new ScanService(_options, null, _moqFileSystem.Object, moqFileScan.Object);
            scanService.Run();

            // Check list sizes initially
            Assert.AreEqual(1, ScanService.FileQueue.Count);
            Assert.AreEqual(1, ScanService.FileExtensionQueue.Count);

            // Check file
            var path = Path.Combine(Directory.GetCurrentDirectory(), files[2]);
            path = path.Replace("\\", "/");
            ScanService.FileQueue.TryPeek(out var file);
            Assert.AreEqual(path, file);

            // Check file extensions
            ScanService.FileExtensionQueue.TryPeek(out var fileExtension);
            Assert.AreEqual(files[1].Split('.')[1], fileExtension);
        }


        [Test]
        public void TestRunBinaryAndTextInSubDirectories()
        {
            var directories = new[] { "source", "bin" };
            _options.Recursive = true;

            var sourceFiles = new[] { "source.c", "source.cpp", "source.h" };
            var binFiles = new[] { "findary", "findary.exe", "findary.dll", "findary.so" };

            // Setup
            _moqFileSystem.Setup(p => p.Directory.Exists(_options.Directory)).Returns(true);

            var paths = directories.Select(file => Path.Combine(_options.Directory, file)).ToArray();
            var emptyArray = Array.Empty<string>();
            _moqFileSystem.Setup(p => p.Directory.EnumerateDirectories(_options.Directory)).Returns(paths);

            // Mock source file related
            var expectedPath = Path.Combine(_options.Directory, directories[0]);
            paths = sourceFiles.Select(file => Path.Combine(_options.Directory, directories[0], file)).ToArray();
            _moqFileSystem.Setup(p => p.Directory.EnumerateDirectories(expectedPath)).Returns(emptyArray);
            _moqFileSystem.Setup(p => p.Directory.EnumerateFiles(expectedPath)).Returns(paths);

            // Mock bin file related
            expectedPath = Path.Combine(_options.Directory, directories[1]);
            paths = binFiles.Select(file => Path.Combine(_options.Directory, directories[1], file)).ToArray();
            _moqFileSystem.Setup(p => p.Directory.EnumerateDirectories(expectedPath)).Returns(emptyArray);
            _moqFileSystem.Setup(p => p.Directory.EnumerateFiles(expectedPath)).Returns(paths);

            // Mock bin files to IsFindaryBinary() => true
            var moqFileScan = new Mock<IFileScan>();
            foreach (var binFile in binFiles)
            {
                expectedPath = Path.Combine(_options.Directory, directories[1], binFile);
                moqFileScan.Setup(p => p.IsFileBinary(expectedPath)).Returns(true);
            }

            // Start service

            var scanService = new ScanService(_options, null, _moqFileSystem.Object, moqFileScan.Object);
            scanService.Run();

            // Check list sizes initially
            Assert.AreEqual(1, scanService.FinalFileList.Count);
            Assert.AreEqual(3, scanService.FinalFileExtensionList.Count);

            // Check file
            expectedPath = Path.Combine(_options.Directory, directories[1], binFiles[0]).Replace('\\', '/');
            var actualFile = scanService.FinalFileList.First();
            Assert.AreEqual(expectedPath, actualFile);

            // Check file extensions
            var expectedFileExtensions = new List<string>();
            binFiles.ToList().ForEach(p =>
            {
                const string c = ".";
                if (p.Contains(c))
                {
                    expectedFileExtensions.Add(p.Split(c)[^1]);
                }
            });
            expectedFileExtensions.Sort();
            var actualFileExtensions = new List<string>(scanService.FinalFileExtensionList);
            actualFileExtensions.Sort();
            for (var i = 0; i < scanService.FinalFileExtensionList.Count; ++i)
            {
                var actualFileExtension = scanService.FinalFileExtensionList[i];
                var expectedFileExtension = expectedFileExtensions[i];
                Assert.AreEqual(expectedFileExtension, actualFileExtension);
            }
        }
    }
}