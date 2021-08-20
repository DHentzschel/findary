using DotNet.Globbing;
using NUnit.Framework;
using System.Collections.Generic;

namespace Findary.Test
{
    public class ExtensionTest
    {
        private readonly IList<Glob> _list1 = new List<Glob> { Glob.Parse("**/*.txt"), Glob.Parse("test") };
        private readonly IList<Glob> _list2 = new List<Glob> { Glob.Parse("**/*.txt"), Glob.Parse("test") };
        private readonly IList<Glob> _list3 = new List<Glob> { Glob.Parse("*.txt"), Glob.Parse("test") };

        [Test]
        public void TestCount()
        {
            const string left = "abcAbcabc";
            const string word = "abc";

            var result = left.Count(word);
            const int expected = 2;
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestCountEmpty()
        {
            const string left = "";
            const string word = "abc";

            var result = left.Count(word);
            const int expected = 0;
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGlobsAreEqual()
        {
            Assert.IsTrue(_list1.Count == _list2.Count);
            for (var i = 0; i < _list1.Count; ++i)
            {
                var left = _list1[i];
                var right = _list2[i];
                Assert.IsTrue(left.AreEqual(right));
            }
        }

        [Test]
        public void TestIsBocu1()
        {
            var result = new byte[] { 0xFB, 0xEE, 0x28, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsGb18030()
        {
            var result = new byte[] { 0x84, 0x31, 0x95, 0x33, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsGlobComment()
        {
            const string result = "# this is a comment";
            Assert.True(result.IsGlobComment());
        }

        [Test]
        public void TestIsGlobCommentInvalid()
        {
            const string result = "string s = \"# this is not a comment\";";
            Assert.False(result.IsGlobComment());
        }

        [Test]
        public void TestIsGlobCommentTab()
        {
            const string result = "\t# this is a comment";
            Assert.True(result.IsGlobComment());
        }

        [Test]
        public void TestIsGlobCommentWithoutPrefix()
        {
            const string result = " # this is a comment";
            Assert.True(result.IsGlobComment());
        }

        [Test]
        public void TestIsScsu()
        {
            var result = new byte[] { 0x0E, 0xFE, 0xFF, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf1()
        {
            var result = new byte[] { 0xF7, 0x64, 0x4C, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf16Be()
        {
            var result = new byte[] { 0xFE, 0xFF, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf16Le()
        {
            var result = new byte[] { 0xFF, 0xFE, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf32Be()
        {
            var result = new byte[] { 0x00, 0x00, 0xFE, 0xFF };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf32Le()
        {
            var result = new byte[] { 0xFF, 0xFE, 0x00F, 0x00, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf7()
        {
            var result = new byte[] { 0x2B, 0x2F, 0x76, 0x38, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtf8()
        {
            var result = new byte[] { 0xEF, 0xBB, 0xBF, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestIsUtfEbcDic()
        {
            var result = new byte[] { 0xDD, 0x73, 0x66, 0x73, 0x00 };
            var bom = new Bom { InputArray = result };
            Assert.IsTrue(bom.HasBom());
        }

        [Test]
        public void TestListsAreEqual() => Assert.IsTrue(_list1.AreEqual(_list2));

        [Test]
        public void TestListsAreEqualBidirectional()
        {
            var left = new List<Glob>();
            left.AddRange(_list2);
            Assert.IsTrue(left.AreEqual(_list2), "list count attributes are not equal");
        }

        [Test]
        public void TestListsAreNotEqual() => Assert.IsFalse(_list2.AreEqual(_list3));

        [Test]
        public void TestListsAreNotEqualNotBidirectional()
        {
            var left = new List<Glob>();
            left.AddRange(_list2);
            left.Add(Glob.Parse("*.bak"));
            Assert.IsFalse(left.AreEqual(_list2), "list count attributes are not equal");
        }
    }
}
