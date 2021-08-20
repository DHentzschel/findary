using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Findary.Test
{
    public class FileScanTest
    {
        [Test]
        public void TestIsFileBinary()
        {
            var input = new byte[] { 0x1, 0x0, 0, 2 };
            var memoryStream = new MemoryStream(input);
            var fileScan = new FileScan.FileScan();
            Assert.IsTrue(fileScan.IsFileBinary(memoryStream));
        }

        [Test]
        public void TestIsFileBinaryEmpty()
        {
            var input = Array.Empty<byte>();
            var memoryStream = new MemoryStream(input);
            var fileScan = new FileScan.FileScan();
            Assert.IsFalse(fileScan.IsFileBinary(memoryStream));
        }

        [Test]
        public void TestIsFileBinaryNonPrintable()
        {
            byte[] ConvertIntToByte(IEnumerable<int> src) => src.Select(t => (byte)t).ToArray();
            var input = ConvertIntToByte(Enumerable.Range(1, 31).ToArray());
            var memoryStream = new MemoryStream(input);
            var fileScan = new FileScan.FileScan();
            Assert.IsFalse(fileScan.IsFileBinary(memoryStream));
        }

        [Test]
        public void TestIsFileBinaryText()
        {
            var input = Encoding.ASCII.GetBytes("hello");
            var memoryStream = new MemoryStream(input);
            var fileScan = new FileScan.FileScan();
            Assert.IsFalse(fileScan.IsFileBinary(memoryStream));
        }

        [Test]
        public void TestIsFileBinaryUtf()
        {
            var input = new byte[] { 0x48, 0x0, 0x64, 0x0, 0x6c, 0x0, 0x6c, 0x0, 0x6f, 0x0 };
            var memoryStream = new MemoryStream(input);
            var fileScan = new FileScan.FileScan();
            Assert.IsTrue(fileScan.IsFileBinary(memoryStream));
        }
    }
}