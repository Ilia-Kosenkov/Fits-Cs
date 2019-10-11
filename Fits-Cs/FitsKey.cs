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
using System.Text;
using FitsCs.Keys;
using JetBrains.Annotations;
using TextExtensions;
using MemoryExtensions;

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
        public static  int CharSizeInBytes { get; } = Encoding.GetMaxCharCount(1);
        public static int EntrySizeInBytes { get; }= EntrySize * CharSizeInBytes;
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

        [ContractAnnotation("name:null => halt")]
        private protected static void ValidateInput(
            [CanBeNull] string name,
            [CanBeNull] string comment, 
            int valueSize)
        {
            if (name is null)
                throw new ArgumentNullException(SR.NullArgument, nameof(name));
            if(name.Length == 0)
                throw new ArgumentException(SR.KeyNameTooShort, nameof(name));
            if (name.Length > NameSize)
                throw new ArgumentException(SR.KeyValueTooLarge, nameof(name));

            if(!IsValidKeyName(name.AsSpan()))
                throw new ArgumentException(SR.HduStringIllegal, nameof(name));

            if (valueSize < 0)
                throw new ArgumentException(SR.InvalidArgument, nameof(valueSize));

            if((comment?.Length ?? 0) + valueSize > EntrySize - NameSize)
                throw new ArgumentException(SR.KeyValueTooLarge);
        }

        [Pure]
        private protected static bool IsValidKeyName(ReadOnlySpan<char> input, bool allowBlank = false)
        {
            if (input.IsEmpty)
                return false;

            if (input.Length > NameSize)
                return false;

            foreach (var @char in input.TrimEnd())
            {
                if (!char.IsUpper(@char) 
                    && !char.IsDigit(@char) 
                    && @char != '-' && @char != '_'
                    && !(allowBlank && @char == ' ')) 
                    return false;
            }

            return true;
        }

        [Pure]
        internal static bool IsValidKeyName(ReadOnlySpan<byte> input, bool allowBlank = false)
        {
            if (input.IsEmpty)
                return false;
            if (input.Length != NameSize * CharSizeInBytes)
                return false;

            Span<char> parsed = stackalloc char[NameSize * CharSizeInBytes];
            
            return Encoding.GetChars(input, parsed) == NameSize && IsValidKeyName(parsed, allowBlank);
        }

        [CanBeNull]
        public static IFitsValue ParseRawData(ReadOnlySpan<byte> input)
        {
            if (input.Length < EntrySizeInBytes)
                return null;
            // Keyword is text-encoded, so convert whole input to chars and do checks

            Span<char> charRep = stackalloc char[EntrySize];

            // Byte array should be exactly convertible to char array, especially when default encoding is ASCII
            if (Encoding.GetChars(input.Slice(0, EntrySizeInBytes), charRep) != EntrySize)
                return null;
            
            // Here, `charRep` contains character representation and contents can be parsed

            // Key name is not valid
            var name = ((ReadOnlySpan<char>) charRep.Slice(0, NameSize)).TrimEnd();
            if (!IsValidKeyName(name, true))
                return null;


            // If keyword has value, it has an '=' symbol at 8 and ' ' at 9
            if (charRep[EqualsPos] == '=' && charRep[EqualsPos + 1] == ' ')
            {
                // Keyword has value
                var contentSpan = charRep.Slice(EqualsPos + 2);
                
                // Look for first non-space symbol. The scheme is smth. like this:
                // Quote -> string
                //      Look for last quote then comment
                // T/F -> bool, look for comment
                // Everything else is treated as number, look for comment

                var firstSymb = '\0';
                var pos = 0;
                foreach(var symb in contentSpan)
                { 
                    if (symb != ' ')
                    {
                        firstSymb = char.ToUpperInvariant(symb);
                        break;
                    }
                    pos++;
                }

                
                switch (firstSymb)
                {
                    case '\'':
                    {
                        // Do string
                        var quoteEnd = FindLastQuote(contentSpan);
                        // Incorrectly formed keyword
                        if (quoteEnd == contentSpan.Length)
                            return null;
                        
                        ReadOnlySpan<char> innerStrSpan = contentSpan.Slice(pos + 1, quoteEnd - pos - 1);

                        var commentStart = FindComment(contentSpan.Slice(quoteEnd + 1));
                        return innerStrSpan.TryParseRaw(out string str)
                            ? Create(name.ToString(),
                                str,
                                commentStart < contentSpan.Length - 1 - quoteEnd
                                    ? System.MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 2 + quoteEnd)).ToString()
                                    : null,
                                innerStrSpan.Length <= FixedFitsKey.FixedFieldSize - ValueStart - 2
                                ? KeyType.Free
                                : KeyType.Fixed)
                            // Returns null if cannot parse string
                            : null;
                    }
                    case 'T':
                    case 'F':
                    {
                        // Do bool
                        var commentStart = FindComment(contentSpan);
                        return Create(name.ToString(),
                            (firstSymb == 'T'),
                            commentStart < contentSpan.Length - 1
                                ? System.MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1)).ToString()
                                : null,
                            pos == FixedFitsKey.FixedFieldSize - 1
                                ? KeyType.Fixed
                                : KeyType.Free);
                    }
                    default:
                    {
                        var commentStart = FindComment(contentSpan);
                        var innerStrSpan = ((ReadOnlySpan<char>)contentSpan.Slice(0, commentStart)).TrimEnd();
                        var isNumber = DetectNumericFormat(innerStrSpan, out var nType, out var keyType);
                        if(!isNumber)
                            return null;

                        switch (nType)
                        {
                                case NumericType.Integer when innerStrSpan.TryParseRaw(out int iVal):
                                    return Create(
                                        name.ToString(),
                                        iVal,
                                        commentStart < contentSpan.Length - 1
                                            ? System.MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1))
                                                .ToString()
                                            : null,
                                        keyType);
                                case NumericType.Float when innerStrSpan.TryParseRaw(out double dVal):
                                {
                                        //return Create(
                                        //    name.ToString(),
                                        //    iVal.Some(),
                                        //    commentStart < contentSpan.Length - 1
                                        //        ? System.MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1))
                                        //            .ToString()
                                        //        : null,
                                        //    keyType);
                                        return null;
                                }

                        }
                        return null;
                    }

                }
            }


            return null;
        }

        private protected static int FindComment(ReadOnlySpan<char> input)
        {
            for(var i = input.Length - 1; i >= 0; i--)
                if (input[i] == '/')
                    return i;

            return input.Length;
        }

        private protected static int FindLastQuote(ReadOnlySpan<char> input)
        {
            var inQuotes = false;
            for (var i = 0; i < input.Length - 1; i++)
            {
                if (input[i] == '\'')
                {
                    if (input[i + 1] != '\'')
                    {
                        if (inQuotes)
                            return i;

                        inQuotes = true;
                    }
                    else
                        i++;
                }
            }

            if (input.Length >= 2 && input[input.Length - 2] != '\'' && input[input.Length - 1] == '\'')
                return input.Length - 1;

            return input.Length;
        }

        private protected static bool DetectNumericFormat(
            ReadOnlySpan<char> input,
            out NumericType numericType,
            out KeyType layoutType)
        {
            numericType = NumericType.Integer;
            layoutType = KeyType.Free;

            if (input.IsEmpty)
                return false;

            var trimmedStr = input.Trim();

            if (input.Length == FixedFitsKey.FixedFieldSize)
            {
                layoutType = KeyType.Fixed;
                // Check for mandatory decimal separator
                foreach (var item in input)
                    if (item == '.')
                    {
                        numericType = NumericType.Float;
                        break;
                    }
            }
            else if (input.Length == 2 * FixedFitsKey.FixedFieldSize && trimmedStr.Length >= FixedFitsKey.FixedFieldSize)
            {
                // Fixed-format complex number
                layoutType = KeyType.Fixed;
                numericType = NumericType.Complex;
            }
            else
            {
                var nDots = 0;
                var isComplex = false;
                foreach (var item in trimmedStr)
                {
                    if (item == '.')
                        nDots++;
                    
                    // Cannot be more than 2 decimal separators in the whole line
                    if (nDots > 2)
                        return false;
                    if (item == ':')
                    {
                        isComplex = true;
                        break;
                    }
                }

                numericType = isComplex 
                    ? NumericType.Complex 
                    : (nDots > 0 
                        ? NumericType.Float 
                        : NumericType.Integer);
            }
            return true;

        }

        public static IFitsValue<T> Create<T>(string name, T value, string comment = null, KeyType type = KeyType.Fixed)
        {
            if(name is null)
                throw new ArgumentNullException(nameof(name));

            return type == KeyType.Free 
                ? FreeFitsKey.Create(name, value, comment) 
                : FixedFitsKey.Create(name, value, comment);
        }

        public static IFitsValue Create(string name, object value, string comment = null,
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
