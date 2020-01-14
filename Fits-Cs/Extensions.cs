#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using FitsCs.Keys;
using IndexRange;
using MemoryExtensions;
using TextExtensions;

namespace FitsCs
{
    internal static class Extensions
    {
        private const int MinFixedStringSize = FixedFitsKey.FixedFieldSize - FitsKey.ValueStart;
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

        public static int SignificantDigitsCount(this long value)
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

        public static bool IsStringHduCompatible(this ReadOnlySpan<char> @string, Encoding? enc = null)
        {
            if (enc is null)
                enc= Encoding.ASCII;
            
            if(@string.IsEmpty)
                return true;

            var byteSize = enc.GetByteCount(@string);

            var buff = @string.Length < 512 ? stackalloc byte[byteSize] : new byte[byteSize];

            var n = enc.GetBytes(@string, buff);
            
            return n > 0 && buff.Slice(0, n).All(x => x >= 0x20 && x <= 0x7E);
        }

        public static IFitsValue? With<T>(
            this IFitsValue<T> @this, 
            Action<KeyUpdater> updateAction)
        {
            if(@this is null)
                throw new ArgumentNullException(nameof(@this), SR.NullArgument);
            if (updateAction is null)
                throw new ArgumentNullException(nameof(updateAction), SR.NullArgument);

            var updater = new KeyUpdater()
            {
                Name = @this.Name,
                Comment = @this.Comment,
                Value = @this.RawValue,
                Type = @this.Type
            };

            updateAction(updater);

            // If it creation fails, return null
            try
            {
                return FitsKey.Create(updater.Name, updater.Value, updater.Comment, updater.Type);
            }
            catch
            {
                return null;
            }
        }


        public static IFitsValue GetFirstByName(this IReadOnlyList<IFitsValue> keys, string name)
        {
            if (keys is null)
                throw new ArgumentNullException(nameof(keys), SR.NullArgument);

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), SR.NullArgument);

            return keys.FirstOrDefault(item => item?.Name == name);
        }
        public static bool IsEnd(this IReadOnlyList<IFitsValue> keys)
        {
            if (keys is null)
                throw new ArgumentNullException(nameof(keys), SR.NullArgument);

            for(var i = keys.Count - 1; i >= 0; i--)
                if (keys[i]?.Name == @"END")
                    return true;

            return false;
        }

        public static void FlipEndianess(this Span<byte> span, int itemSizeInBytes)
        {
            if(span.IsEmpty)
                return;
            
            var length = span.Length / itemSizeInBytes;

            switch (itemSizeInBytes)
            {
                case 2:
                {
                    for (var i = 0; i < length; i++)
                    {
                        var offset = 2 * i;
                        var temp = span[offset];
                        span[offset] = span[offset + 1];
                        span[offset + 1] = temp;
                    }

                    break;
                }
                case 4:
                {
                    for (var i = 0; i < length; i++)
                    {
                        var offset = 4 * i;

                        var temp = span[offset];
                        span[offset] = span[offset + 3];
                        span[offset + 3] = temp;

                        temp = span[offset + 1];
                        span[offset + 1] = span[offset + 2];
                        span[offset + 2] = temp;
                    }

                    break;
                }
                case 8:
                {
                    for (var i = 0; i < length; i++)
                    {
                        var offset = 8 * i;

                        var temp = span[offset];
                        span[offset] = span[offset + 7];
                        span[offset + 7] = temp;

                        temp = span[offset + 1];
                        span[offset + 1] = span[offset + 6];
                        span[offset + 6] = temp;

                        temp = span[offset + 2];
                        span[offset + 2] = span[offset + 5];
                        span[offset + 5] = temp;

                        temp = span[offset + 3];
                        span[offset + 3] = span[offset + 4];
                        span[offset + 4] = temp;
                    }

                    break;
                }
            }
        }
       
    }

    public static class ParsingExtensions
    {
        private static readonly RecycledString Rc = 
            new RecycledString(FitsKey.EntrySize);

        public static ExtensionType FitsExtensionTypeFromString(string? extension = null)
            => extension?.ToLowerInvariant() switch
            {
                { } x when x.StartsWith(@"bintable") => ExtensionType.BinTable,
                { } x when x.StartsWith(@"image") => ExtensionType.Image,
                { } x when x.StartsWith(@"table") => ExtensionType.Table,
                _ => ExtensionType.Primary,
            };
        public static bool TryParseRaw(
            this ReadOnlySpan<char> quotedString, 
            out string? @string)
        {
            @string = null;
            var trimmedInput = quotedString.Trim();
            // If input is empty or exceeds one entry size;
            if (trimmedInput.Length > FitsKey.EntrySize)
                return false;

            if (trimmedInput.IsEmpty)
            {
                @string = string.Empty;
                return true;
            }
            

            Span<char> resultSpan = stackalloc char[trimmedInput.Length];

            var start = 0;
            var offset = 0;
            for (var i = 0; i < trimmedInput.Length - 1; i++)
            {
                if (trimmedInput[i] != '\'') continue;

                if (trimmedInput[i + 1] == '\'')
                {
                    var len = i + 1 - start;
                    if (!trimmedInput.Slice(start, len).TryCopyTo(resultSpan.Slice(offset)))
                        return false;
                    start += len + 1;
                    offset += len;
                    i += 1;
                }
                else
                    return false;
            }

            if (start <= trimmedInput.Length - 1 &&
                trimmedInput.Slice((start, Index.End)).TryCopyTo(resultSpan.Slice(offset)))
            {
                @string = resultSpan.Slice(0, offset + trimmedInput.Length - start).ToString();
                return true;
            }

            return false;
        }

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out int number) =>
            int.TryParse(
                Rc.ProxyAsString(numberString),
                NumberStyles.Integer,
                NumberFormatInfo.InvariantInfo, out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out long number) =>
            long.TryParse(
                Rc.ProxyAsString(numberString),
                NumberStyles.Integer,
                NumberFormatInfo.InvariantInfo, out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out float number)
            => float.TryParse(
                Rc.ProxyAsString(numberString),
                NumberStyles.Float, 
                NumberFormatInfo.InvariantInfo, 
                out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out double number)
            => double.TryParse(
                ProxyDoubleWithCorrectExponent(numberString),
                NumberStyles.Float, 
                NumberFormatInfo.InvariantInfo,
                out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out Complex number,
            KeyType type = KeyType.Fixed)
        {
            if (type == KeyType.Fixed)
            {
                var result = numberString.Slice(0, FixedFitsKey.FixedFieldSize).TryParseRaw(out double real)
                             & numberString.Slice(FixedFitsKey.FixedFieldSize).TryParseRaw(out double image);

                number = new Complex(real, image);
                return result;
            }
            else
            {
                number = default;
                var trimmed = numberString.Trim();
                var columnPos = trimmed.IndexOf(':');
                if (columnPos == -1)
                    return false;

                var result =
                    numberString.Slice(0, columnPos).TryParseRaw(out double real)
                    & numberString.Slice(columnPos + 1).TryParseRaw(out double image);

                number = new Complex(real, image);
                return result;
            }
        }


        private static string ProxyDoubleWithCorrectExponent(ReadOnlySpan<char> input)
        {
            var id = -1;
            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] != 'D' && input[i] != 'd') continue;
                id = i;
                break;
            }
            
            Rc.Clear();
            Rc.TryCopy(input);
            if (id != -1)
                Rc.TryCopy("E", id);

            return Rc.StringView;
        }
    }
}
