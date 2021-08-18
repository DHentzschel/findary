using System.Collections.Generic;
using System.Linq;

namespace Findary
{
    public class Bom
    {
        public IReadOnlyList<byte> InputArray { get; set; }

        public bool HasBom() =>
            IsUtf8() || IsUtf16Be() || IsUtf16Le()
            || IsUtf32Be() || IsUtf32Le() || IsUtf7() || IsUtf1()
            || IsUtfEbcDic() || IsScsu() || IsBocu1() || IsGb18030();

        private bool HasBom(IReadOnlyCollection<byte> bom, IEnumerable<byte> lastByte = default)
        {
            if (InputArray.Count < bom.Count)
            {
                return false;
            }

            if (bom.Where((t, i) => InputArray[i] != t).Any())
            {
                return false;
            }

            return lastByte?.Any(p =>
            {
                var lastBomByte = InputArray[bom.Count];
                return p == lastBomByte;
            }) != false;
        }

        private bool IsBocu1() => HasBom(new byte[] { 0xFB, 0xEE, 0x28 });

        private bool IsGb18030() => HasBom(new byte[] { 0x84, 0x31, 0x95, 0x33 });

        private bool IsScsu() => HasBom(new byte[] { 0x0E, 0xFE, 0xFF });

        private bool IsUtf1() => HasBom(new byte[] { 0xF7, 0x64, 0x4C });

        private bool IsUtf16Be() => HasBom(new byte[] { 0xFE, 0xFF });

        private bool IsUtf16Le() => HasBom(new byte[] { 0xFF, 0xFE });

        private bool IsUtf32Be() => HasBom(new byte[] { 0x00, 0x00, 0xFE, 0xFF });

        private bool IsUtf32Le() => HasBom(new byte[] { 0xFF, 0xFE, 0x00, 0x00 });

        private bool IsUtf7() => HasBom(new byte[] { 0x2B, 0x2F, 0x76 }, new byte[] { 0x38, 0x39, 0x2B, 0x2F });

        private bool IsUtf8() => HasBom(new byte[] { 0xEF, 0xBB, 0xBF });

        private bool IsUtfEbcDic() => HasBom(new byte[] { 0xDD, 0x73, 0x66, 0x73 });
    }
}
