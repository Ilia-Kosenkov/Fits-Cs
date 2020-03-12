#nullable enable

using System;
using System.Buffers;
using System.Globalization;
using System.Numerics;
using FitsCs.Keys;
using MemoryExtensions;

namespace FitsCs
{
    public static class ParsingExtensions
    {
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

            if (start > trimmedInput.Length - 1 ||
                !trimmedInput.Slice(start..).TryCopyTo(resultSpan.Slice(offset))) 
                return false;
            @string = resultSpan.Slice(0, offset + trimmedInput.Length - start).ToString();
            return true;

        }

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out int number) =>
            int.TryParse(
                numberString,
                NumberStyles.Integer,
                NumberFormatInfo.InvariantInfo, out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out long number) =>
            long.TryParse(
                numberString,
                NumberStyles.Integer,
                NumberFormatInfo.InvariantInfo, out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out float number)
            => float.TryParse(
                numberString,
                NumberStyles.Float, 
                NumberFormatInfo.InvariantInfo, 
                out number);

        public static bool TryParseRaw(
            this ReadOnlySpan<char> numberString,
            out double number)
        {
            char[]? arrayBuff = null;

            var trimmedInput = numberString.Trim();

            try
            {
                var buff =
                    trimmedInput.Length > 64
                        ? (arrayBuff = ArrayPool<char>.Shared.Rent(trimmedInput.Length))[..trimmedInput.Length]
                        : stackalloc char[trimmedInput.Length];

                buff.Fill('\0');
                trimmedInput.CopyTo(buff);
                FixDoubleExponent(buff);

                return double.TryParse(
                    buff,
                    NumberStyles.Float,
                    NumberFormatInfo.InvariantInfo,
                    out number);
            }
            finally
            {
                if (arrayBuff is { })
                    ArrayPool<char>.Shared.Return(arrayBuff);
            }
        }

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


        private static void FixDoubleExponent(Span<char> input)
        {
            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] != 'D' && input[i] != 'd') continue;
                input[i] = 'E';
                return;
            }
            
        }
    }
}