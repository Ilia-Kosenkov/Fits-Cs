﻿//     MIT License
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
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using IndexRange;
using MemoryExtensions;
using TextExtensions;

namespace FitsCs
{

    public enum KeyType : byte
    {
        Fixed,
        Free
    }
    public abstract class FitsKey : IFitsValue
    {
        public const int NameSize = 8;
        public const int EqualsPos = 8;
        public const int ValueStart = 10;
        public const int EntrySize = 80;

        private protected static Encoding Encoding { get; } = Encoding.ASCII;
        private static  int AsciiCharSize { get; } = Encoding.GetMaxCharCount(1);
        public static int EntrySizeInBytes { get; }= EntrySize * AsciiCharSize;
        public static IImmutableList<Type> AllowedTypes { get; } = new[]
        {
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(bool),
            typeof(System.Numerics.Complex)
        }.ToImmutableList();

        private protected virtual string TypePrefix => @"[  null]";

        public string Name { get; }
        public string Comment { get; }
        public abstract object Value { get; }
        public virtual KeyType Type => throw new NotImplementedException();

        public abstract  bool IsEmpty { get; }

        private protected FitsKey(string name, string comment)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length > NameSize)
                throw new ArgumentException($"Name should be a string of (1, {NameSize}] symbols.", nameof(name));

            Name = name;
            Comment = comment ?? string.Empty;
        }

        public override string ToString()
        {
            var pool = ArrayPool<char>.Shared.Rent(EntrySize);
            try
            {
                var span = pool.AsSpan(0, EntrySize);
                return TryFormat(span, out var chars)
                    ? span.Slice(0, chars).ToString()
                    : base.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(pool, true);
            }
        }

        public string ToString(bool prefixType)
            => prefixType ? TypePrefix + ToString() : ToString();
        
        public abstract bool TryFormat(Span<char> span, out int charsWritten);
        public bool TryGetBytes(Span<byte> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;
            var charBuff = ArrayPool<char>.Shared.Rent(EntrySize);
            try
            {
                var charSpan = charBuff.AsSpan(0, EntrySize);
                charSpan.Fill(' ');
                if (!TryFormat(charSpan, out _))
                    return false;

                var nBytes = Encoding.GetBytes(charSpan, span);
                return nBytes > 0 && nBytes < EntrySizeInBytes;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuff, true);
            }
        }

        private static int FindCommentStart(ReadOnlySpan<char> input, char sep = '/')
        {
            var inQuotes = false;
            int i;
            for(i = 0;  i < input.Length; i++)
                if (input[i] == '\'')
                    inQuotes = !inQuotes;
                else if (!inQuotes && input[i] == sep)
                    break;

            return i == input.Length - 1
                ? input.Length
                : i;
        }

        private protected static (string Name, string Value, string Comment) ParseRawData(ReadOnlySpan<byte> data)
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

        private protected static void ValidateInput(string name, string comment, int valueSize)
        {
            if(name.Length == 0)
                throw new ArgumentException("Keyword name is too short.", nameof(name));
            if (name.Length > NameSize)
                throw new ArgumentException("Keyword name is too long.", nameof(name));

            if(comment.Length + valueSize > EntrySize - EqualsPos - 4)
                throw new ArgumentException("Keyword content is too long.");
        }

        private protected static void ValidateType<T>()
        {
            if(!AllowedTypes.Contains(typeof(T)))
                throw new NotSupportedException(@"Type is not supported.");
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

        public static IFitsValue<T> Create<T>(string name, T value, string comment = null, KeyType type = KeyType.Fixed)
        {
            if (type == KeyType.Free)
                throw new NotImplementedException();

            return FixedFitsKey.Create(name, value, comment);
        }
    }

    public abstract class FixedFitsKey : FitsKey
    {
        public override KeyType Type => KeyType.Fixed;
        private protected FixedFitsKey(string name, string comment) : base(name, comment)
        {
        }

        private protected bool FormatFixed(Span<char> span, string value, out int charsWritten)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            charsWritten = 0;
            var len = EqualsPos + 2 +
                      value.Length +
                      (!isCommentNull
                          ? Comment.Length + 2
                          : 0);

            if (span.Length < len)
                return false;

            span.Slice(0, len).Fill(' ');
            Name.AsSpan().CopyTo(span);
            span[EqualsPos] = '=';
            value.AsSpan().CopyTo(span.Slice(EqualsPos + 2));

            charsWritten = value.Length + NameSize + 2;

            if (!isCommentNull)
            {
                Comment.AsSpan().CopyTo(span.Slice(charsWritten + 2));
                span[charsWritten + 1] = '/';
                charsWritten += 2 + Comment.Length;
            }

            return true;
        }

        public static IFitsValue<T> Create<T>(string name, T value, string comment = null)
        {
            ValidateType<T>();

            switch (value)
            {
                case float fVal:
                    return new FixedFloatKey(name, fVal, comment) as IFitsValue<T>;
                case int iVal:
                    return new FixedIntKey(name, iVal, comment) as IFitsValue<T>;
                case bool bVal:
                    return new FixedBoolKey(name, bVal, comment) as IFitsValue<T>;
            }
           throw new NotSupportedException();
        }
    }
}
