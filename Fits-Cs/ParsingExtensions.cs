#nullable enable

using System;
using System.Globalization;
using System.Numerics;
using FitsCs.Keys;
using MemoryExtensions;
using TextExtensions;

namespace FitsCs
{
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
            var trimmedInput = quotedString.TrimEnd();
            // If input is empty or exceeds one entry size;
            if (trimmedInput.Length > FitsKey.EntrySize)
                return false;

            if (trimmedInput.IsEmpty)
            {
                // Preserves spaces-only strings
                @string = quotedString.Length > 0 ? quotedString.ToString() : string.Empty;
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
                trimmedInput.Slice(start..).TryCopyTo(resultSpan.Slice(offset)))
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

                if (!trimmed[..columnPos].TryParseRaw(out double real) 
                    || !trimmed[(columnPos + 1)..].TryParseRaw(out double image)) 
                    return false;
                
                number = new Complex(real, image);
                return true;

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