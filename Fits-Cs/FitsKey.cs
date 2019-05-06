//     MIT License
//     
//     Copyright(c) 2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Buffers;
using System.ComponentModel.Design;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Compatibility.Bridge;

namespace FitsCs
{
    public abstract class FitsKey
    {
        public const int NameSize = 8;
        public const int EqualsPos = 8;
        public const int ValueStart = 10;
        public const int EntrySize = 80;

        private static readonly int AsciiCharSize = Encoding.ASCII.GetMaxCharCount(1);
        public static readonly int EntrySizeInBytes = EntrySize * AsciiCharSize;

        public string Name { get; }
        public string Comment { get; }
        public abstract object Value { get; }

        public abstract  bool IsEmpty { get; }

        private protected FitsKey(string name, string comment)
        {
            Name = name;
            Comment = comment;
        }

        public abstract string ToString(bool prefixType);

        public abstract bool TryFormat(Span<char> span, out int charsWritten);

        private static int FindCommentStart(ReadOnlySpan<char> input, char sep = '/')
        {
            var inQuotes = false;
            var i = 0;
            for(i = 0;  i < input.Length; i++)
                if (input[i] == '\'')
                    inQuotes = !inQuotes;
                else if (!inQuotes && input[i] == '/')
                    break;

            return i == input.Length - 1
                ? input.Length
                : i;
        }

        protected static (string Name, string Value, string Comment) ParseRawData(ReadOnlySpan<byte> data)
        {
            if (data.Length != EntrySize * AsciiCharSize)
                throw new ArgumentException("Size of data is incorrect.", nameof(data));
            if (!IsValidKeyName(data.Slice(0, NameSize * AsciiCharSize)))
                throw new ArgumentException("Provided input is not a valid keyword", nameof(data));

            var buffer = ArrayPool<char>.Shared.Rent(EntrySize);
            var rwMem = buffer.AsMemory(0, EntrySize);
            var roMem = buffer.AsReadOnlyMemory(0, EntrySize);
            try
            {
                var n = Encoding.ASCII.GetChars(data, rwMem.Span);
                if (n != EntrySize)
                    throw new ArgumentException("Provided input is not a valid keyword", nameof(data));

                roMem.Span.Slice(0, NameSize).ToUpperInvariant(rwMem.Span);

                var name = roMem.Slice(0, NameSize).Span.Trim().ToString();

                var bodySpan = roMem.Slice(NameSize);
                if (bodySpan.Span[EqualsPos - NameSize] == '=')
                {
                    var commentInd = FindCommentStart(bodySpan.Span);

                    var value = bodySpan.Slice((EqualsPos - NameSize, commentInd)).Trim().ToString();

                    Range commRange = (commentInd + 1, Index.End);
                    var comment = commRange.IsValidRange(bodySpan.Length)
                        ? bodySpan.Slice(commRange).Trim().ToString()
                        : string.Empty;
                    return (name, value, comment);
                }
                // Must be comment or some empty key
                if (string.IsNullOrWhiteSpace(name))
                    return (string.Empty, string.Empty, bodySpan.Trim().ToString());

                if (name == @"COMMENT" || name == @"HISTORY" || name == @"REFERENCE")
                    return (name, string.Empty, bodySpan.Trim().ToString());

                if (name == @"END")
                    return (name, string.Empty, string.Empty);

                throw new NotSupportedException(@"Keyword format is not supported");
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
        public static bool IsValidKeyName(ReadOnlySpan<byte> input)
        {
            bool IsAllowed(char c)
            {
                return char.IsUpper(c)
                       || char.IsDigit(c)
                       || c == ' '
                       || c == '-'
                       || c == '_';
            }

            if (input.Length == NameSize * AsciiCharSize)
            {
                var buffer = ArrayPool<char>.Shared.Rent(NameSize * AsciiCharSize);
                var charSpan = buffer.AsSpan(0, NameSize * AsciiCharSize);

                Encoding.ASCII.GetChars(input, charSpan);
                try
                {
                    return charSpan.All(IsAllowed);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }

            return false;
        }

        public static FitsKey Create(ReadOnlySpan<byte> input)
        {
            var (name, value, comment) = ParseRawData(input);

            if(string.IsNullOrEmpty(value))
                return new MetaFitsKey(name, comment);

            var span = value.AsSpan();
            if (span[0] == '=')
            {
                var lastSymb = char.ToUpper(span.Get(-1));
                if(lastSymb == 'T')
                    return new BoolFitsKey(name, true, comment);
                if(lastSymb == 'F')
                    return new BoolFitsKey(name, false, comment);

                var valSpan = span.Slice(1).Trim();
                if (valSpan[0] == '\'' && valSpan.Get(-1) == '\'')
                    return new StringFitsKey(name, valSpan.Slice((1, -1)).ToString().Replace("\'\'", "\'"), comment);

                var valStr = valSpan.ToString();

                if (int.TryParse(valStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var iVal))
                    return new IntFitsKey(name, iVal, comment);
                else if (float.TryParse(valStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var fVal))
                    return new FloatFitsKey(name, iVal, comment);
            }
            // TODO : Treat special cases
            return null;
        }
    }
}
