using System;
using System.Buffers;
using System.Text;
using TextExtensions;
using MemoryExtensions;

namespace FitsCs
{
    public static class Extensions
    {    
        public static int StringSizeWithQuoteReplacement(this ReadOnlySpan<char> s)
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

            return sum;
        }

        public static bool TryGetCompatibleString(this ReadOnlySpan<char> source, Span<char> target)
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

            target[targetInd] = '\'';

            return true;
        }
    }
}
