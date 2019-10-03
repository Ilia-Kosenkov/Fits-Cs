using System;
using System.Text;
using MemoryExtensions;
using TextExtensions;

namespace FitsCs
{
    internal static class Extensions
    {
        private const int MinFixedStringSize = 10;
        public static int StringSizeWithQuoteReplacement(
            this ReadOnlySpan<char> s,
            int minLength = MinFixedStringSize)
        {
            var sum = 2;
            foreach (var item in s)
            {
                // Regular character 1-to-1
                sum += 1;
                // Single quote 2-to-1
                if(item == '\'')
                    sum += 1;
                // Double quote replaced by 4 single quotes
                if (item == '"')
                    sum += 3;
            }

            return sum < minLength ? minLength : sum;
        }

        public static bool TryGetCompatibleString(
            this ReadOnlySpan<char> source, 
            Span<char> target,
            int minLength = MinFixedStringSize)
        {
            if (source.IsEmpty)
                return true;

            if (target.Length < source.Length + 2)
                return false;

            target[0] = '\'';
            var srcInd = 0;
            var targetInd = 1;
            
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] == '\'')
                {
                    if (!source.Slice((srcInd, i + 1)).TryCopyTo(target.Slice(targetInd)))
                        return false;
                    targetInd += i - srcInd + 1;
                    target[targetInd++] = '\'';
                    srcInd = i + 1;
                }
                else if (source[i] == '"')
                {
                    if (!source.Slice((srcInd, i)).TryCopyTo(target.Slice(targetInd)))
                        return false;
                    targetInd += i - srcInd;
                    target.Slice(targetInd, 4).Fill('\'');
                    targetInd += 4;
                    srcInd = i + 1;
                }

            }

            if (srcInd < source.Length)
            {
                if (!source.Slice(srcInd).TryCopyTo(target.Slice(targetInd)))
                    return false;
                targetInd += source.Length - srcInd;
            }

            if (targetInd >= target.Length)
                return false;

            if (targetInd < minLength - 1)
            {
                target.Slice((targetInd, minLength - 1)).Fill(' ');
                targetInd = minLength - 1;
            }

            target[targetInd] = '\'';

            return true;
        }

        public static int SignificantDigitsCount(this int value)
        {
            if (value == 0)
                return 1;
            if (value < 0)
                value = -value;

            var n = 0;

            while (value != 0)
            {
                n++;
                value /= 10;
            }

            return n;
        }

        public static string FormatDouble(this double value, int decPos, int maxSize)
        {
            // Using straightforward two-attempt way
            var resultStr = string.Format($"{{0,{maxSize}:G{decPos}}}", value);
            if (resultStr.Length > maxSize)
                resultStr = string.Format($"{{0,{maxSize}:E{maxSize - 8}}}", value);
            return resultStr;
        }

        public static bool IsStringHduCompatible(this ReadOnlySpan<char> @string, Encoding enc = null)
        {
            if (enc is null)
                enc= Encoding.ASCII;

            var byteSize = enc.GetByteCount(@string);

            Span<byte> buff = stackalloc byte[byteSize];

            var n = enc.GetBytes(@string, buff);
            
            return n > 0 && buff.Slice(0, n).All(x => x >= 0x20 && x <= 0x7E);
        }
    }
}
