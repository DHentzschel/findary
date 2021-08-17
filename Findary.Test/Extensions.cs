using DotNet.Globbing;
using System.Collections.Generic;
using System.Linq;

namespace Findary.Test
{
    public static class Extensions
    {
        public static bool AreEqual<T>(this IList<T> input, IList<T> right)
            => input.Count == right.Count && input.All(p => right.Any(q => q.AreEqual(p)));

        public static bool AreEqual<T>(this T input, T right) => input.ToString().Equals(right.ToString());
    }
}
