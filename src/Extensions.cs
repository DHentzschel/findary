using System;
using System.Collections.Generic;
using System.Linq;

namespace Findary
{
    public static class Extensions
    {
        public const int MaximumChars = 32767; // 2^15-1

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

        public static bool IsGlobComment(this string input) => input.TrimStart().StartsWith('#');

        public static List<string> Split(this List<string> input, int commandPrefixLength)
        {
            var commandLength = commandPrefixLength;
            var line = string.Empty;
            var result = new List<string>();
            foreach (var item in input)
            {
                var newCommandLength = commandLength + item.Length;
                if (newCommandLength < MaximumChars)
                {
                    line += " " + item;
                    commandLength += item.Length;
                }
                else
                {
                    result.Add(line);
                    line = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(line))
            {
                result.Add(line);
            }
            return result;
        }

        public static List<string> ToParamList(this IEnumerable<string> input, string prefix/*, int commandLength*/)
        {
            string GetAddendum(string item) => '"' + (prefix.Length > 0 ? prefix : string.Empty) + item + '"';
            return input.Select(GetAddendum).ToList();
        }
    }
}