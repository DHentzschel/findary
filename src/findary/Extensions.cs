using System;
using System.Collections.Generic;
using System.Linq;

namespace Findary
{
    public static class Extensions
    {
        private const int MaximumChars = 32767;

        public static List<string> Concat(this IEnumerable<string> input, string prefix, int commandLength)
        {
            var result = new List<string>();

            void AddPreparedLine(string line) => result.Add(line.TrimEnd());
            string GetAddendum(string item) => '"' + (prefix.Length > 0 ? prefix : string.Empty) + item + "\" ";

            var maximumChars = MaximumChars - commandLength;
            var call = string.Empty;
            foreach (var str in input)
            {
                var addendum = GetAddendum(str);
                if (call.Length + addendum.Length > maximumChars)
                {
                    AddPreparedLine(call);
                    call = string.Empty;
                }
                call += addendum;
            }
            AddPreparedLine(call);
            return result;
        }

        public static int Count(this string input, string word)
        {
            var result = 0;
            var searchString = 0;
            while ((searchString = input.IndexOf(word, searchString, StringComparison.Ordinal)) != -1)
            {
                searchString += word.Length;
                ++result;
            }
            return result;
        }
        public static bool HasBom(this IReadOnlyList<byte> input)
        {
            return input.IsUtf8()
                   || input.IsUtf16Be() || input.IsUtf16Le()
                   || input.IsUtf32Be() || input.IsUtf32Le()
                   || input.IsUtf7() || input.IsUtf1()
                   || input.IsUtfEbcDic() || input.IsScsu()
                   || input.IsBocu1() || input.IsGb18030();
        }

        private static bool HasBom(this IReadOnlyList<byte> input, IReadOnlyCollection<byte> bom, IEnumerable<byte> lastByte = null)
        {
            if (input.Count < bom.Count)
            {
                return false;
            }

            if (bom.Where((t, i) => input[i] != t).Any())
            {
                return false;
            }

            return lastByte?.Any(p =>
            {
                var lastBomByte = input[bom.Count];
                return p == lastBomByte;
            }) != false;
        }

        private static bool IsBocu1(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xFB, 0xEE, 0x28 });

        private static bool IsGb18030(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0x84, 0x31, 0x95, 0x33 });

        private static bool IsScsu(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0x0E, 0xFE, 0xFF });

        private static bool IsUtf1(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xF7, 0x64, 0x4C });

        private static bool IsUtf16Be(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xFE, 0xFF });

        private static bool IsUtf16Le(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xFF, 0xFE });

        private static bool IsUtf32Be(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0x00, 0x00, 0xFE, 0xFF });

        private static bool IsUtf32Le(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xFF, 0xFE, 0x00, 0x00 });

        private static bool IsUtf7(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0x2B, 0x2F, 0x76 }, new byte[] { 0x38, 0x39, 0x2B, 0x2F });

        private static bool IsUtf8(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xEF, 0xBB, 0xBF });

        private static bool IsUtfEbcDic(this IReadOnlyList<byte> input) => input.HasBom(new byte[] { 0xDD, 0x73, 0x66, 0x73 });
    }
}