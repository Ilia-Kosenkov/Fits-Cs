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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FitsCs.Keys;
using Maybe;
using TextExtensions;

namespace FitsCs
{

    public enum KeyType : byte
    {
        Undefined = 0,
        Fixed = 1,
        Free = 2
    }

    // ReSharper disable once UseNameofExpression
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
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
            typeof(double),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(bool),
            typeof(System.Numerics.Complex)
        }.ToImmutableList();

        private protected virtual string TypePrefix => @"[  null]";

        private protected virtual string DebuggerDisplay => $"{TypePrefix}: {ToString()}";

        public string Name { get; }
        public string Comment { get; }
        public abstract object Value { get; }
        public virtual KeyType Type => KeyType.Undefined;

        public abstract  bool IsEmpty { get; }

        private protected FitsKey(string name, string comment)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Comment = comment ?? string.Empty;
        }

        public override string ToString()
        {
            var pool = ArrayPool<char>.Shared.Rent(EntrySize);
            try
            {
                var span = pool.AsSpan(0, EntrySize);
                return TryFormat(span)
                    ? span.Slice(0, EntrySize).ToString()
                    : base.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(pool, true);
            }
        }

        public string ToString(bool prefixType)
            => prefixType ? TypePrefix + ToString() : ToString();
        
        public abstract bool TryFormat(Span<char> span);

        public bool TryGetBytes(Span<byte> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;
            var charBuff = ArrayPool<char>.Shared.Rent(EntrySize);
            try
            {
                var charSpan = charBuff.AsSpan(0, EntrySize);
                if (!TryFormat(charSpan))
                    return false;

                var nBytes = Encoding.GetBytes(charSpan, span);
                return nBytes > 0 && nBytes <= EntrySizeInBytes;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuff, true);
            }
        }

        private protected bool TryFormat(Span<char> span, string value)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            var len = NameSize +
                      value.Length;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);
            value.AsSpan().CopyTo(span.Slice(NameSize));


            if (isCommentNull) return true;

            Comment.AsSpan().CopyTo(span.Slice(len + 2));
            span[len + 1] = '/';

            return true;
        }

        private protected static void ValidateInput(string name, string comment, int valueSize)
        {
            if(name.Length == 0)
                throw new ArgumentException(SR.KeyNameTooShort, nameof(name));
            if (name.Length > NameSize)
                throw new ArgumentException(SR.KeyValueTooLarge, nameof(name));

            if(!IsValidKeyName(name.AsSpan()))
                throw new ArgumentException(SR.HduStringIllegal, nameof(name));

            if((comment?.Length ?? 0) + valueSize > EntrySize - NameSize)
                throw new ArgumentException(SR.KeyValueTooLarge);
        }

        //private protected static void ValidateType<T>()
        //{
        //    if(!AllowedTypes.Contains(typeof(T)))
        //        throw new NotSupportedException(SR.KeyTypeNotSupported);
        //}

       private protected static bool IsValidKeyName(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty)
                return false;

            if (input.Length > NameSize)
                return false;

            foreach (var @char in input.TrimEnd())
            {
                if (!char.IsUpper(@char) && !char.IsDigit(@char) && @char != '-' && @char != '_') 
                    return false;
            }

            return true;
        }

        public static IFitsValue<T> Create<T>(string name, Maybe<T> value, string comment = null, KeyType type = KeyType.Fixed)
        {
            if(name is null)
                throw new ArgumentNullException(nameof(name));

            return type == KeyType.Free 
                ? FreeFitsKey.Create(name, value, comment) 
                : FixedFitsKey.Create(name, value, comment);
        }

        public static IFitsValue Create(string name, Maybe.Maybe value, string comment = null,
            KeyType type = KeyType.Fixed)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            return type == KeyType.Free
                ? FreeFitsKey.Create(name, value, comment)
                : FixedFitsKey.Create(name, value, comment);
        }

        public static IFitsValue CreateBlank() => BlankKey.Blank;
        public static IFitsValue Create(string content) => new ArbitraryKey(content);
        public static IFitsValue CreateComment(string comment) => new ArbitraryKey(@"COMMENT " + comment);
        public static IFitsValue CreateHistory(string history) => new ArbitraryKey(@"HISTORY " + history);

    }
}
