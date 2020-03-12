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
#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FitsCs.Keys;
using System.Numerics;

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
        public const int KeysPerUnit = DataBlob.SizeInBytes / EntrySize;
        private protected static Encoding Encoding { get; } = Encoding.ASCII;
        public static int CharSizeInBytes { get; } = Encoding.GetMaxCharCount(1);
        public static int EntrySizeInBytes { get; } = EntrySize * CharSizeInBytes;

        private protected virtual string TypePrefix => @"null";

        private protected virtual string DebuggerDisplay => $"{TypePrefix}: {ToString()}";

        public string Name { get; }
        public string Comment { get; }
        public abstract object? Value { get; }
        public virtual KeyType Type => KeyType.Undefined;

        public abstract bool IsEmpty { get; }

        private protected FitsKey(string name, string? comment, int size)
        {
            ValidateInput(name, size, comment?.Length ?? 0);
            Name = name;
            Comment = comment switch
            {
                { } when comment.Length >0 && comment[0] != ' ' && TryValidateInput(name, size, comment.Length + 1)
                => " " + comment,
                { } => comment,
                _ => string.Empty
            };
        }

        public override string ToString()
        {
            Span<char> span = stackalloc char[EntrySize];
            return TryFormat(span)
                ? span.ToString()
                : base.ToString();
        }

        public string ToString(bool prefixType)
        {
            if (!prefixType) return ToString();

            var frmtStr = Type switch
            {
                KeyType.Fixed => @"fix",
                KeyType.Free => @"fre",
                _ => @"udf"
            };
            return $"[{frmtStr,-3}|{TypePrefix,3}]: {ToString()}";

        }
        
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

        private protected bool TryFormat(Span<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            var len = NameSize +
                      value.Length;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);
            value.CopyTo(span.Slice(NameSize));


            if (isCommentNull) return true;

            // Comment padding if it can be fit in the entry
            if (len < FixedFitsKey.FixedFieldSize + ValueStart &&
                Comment.Length <= EntrySize - FixedFitsKey.FixedFieldSize - ValueStart - 2)
                len = ValueStart + FixedFitsKey.FixedFieldSize;

            Comment.AsSpan().CopyTo(span.Slice(len + 2));
            span[len + 1] = '/';

            return true;
        }

        private protected static void ValidateInput(
            string? name,
            int valueSize,
            int commentSize = 0)
        {
            if (name is null)
                throw new ArgumentNullException(SR.NullArgument, nameof(name));
            if (name.Length == 0 && valueSize != 0)
                throw new ArgumentException(SR.KeyNameTooShort, nameof(name));
            if (name.Length > NameSize)
                throw new ArgumentException(SR.KeyValueTooLarge, nameof(name));

            if (name.Length > 0 && !IsValidKeyName(name.AsSpan()))
                throw new ArgumentException(SR.HduStringIllegal, nameof(name));

            if (valueSize < 0)
                throw new ArgumentException(SR.InvalidArgument, nameof(valueSize));

            // It was +2 to account for `= `, but in general case it is allowed to have
            // even larger comments if it is e.g. `HISTORY`
            if (commentSize + valueSize > EntrySize - NameSize)
                throw new ArgumentException(SR.KeyValueTooLarge);
        }

        private protected static bool TryValidateInput(
            string? name,
            int valueSize,
            int commentSize = 0)
        {
            if (name is null)
                return false;
            if (name.Length == 0 && valueSize != 0)
                return false;

            if (name.Length > NameSize)
                return false;


            if (name.Length > 0 && !IsValidKeyName(name.AsSpan()))
                return false;


            if (valueSize < 0)
                return false;

            // It was +2 to account for `= `, but in general case it is allowed to have
            // even larger comments if it is e.g. `HISTORY`
            if (commentSize + valueSize > EntrySize - NameSize)
                return false;

            return true;

        }

        public static bool IsValidKeyName(ReadOnlySpan<char> input, bool allowBlank = false)
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

        internal static bool IsValidKeyName(ReadOnlySpan<byte> input, bool allowBlank = false)
        {
            if (input.IsEmpty)
                return false;
            if (input.Length != NameSize * CharSizeInBytes)
                return false;

            Span<char> parsed = stackalloc char[NameSize * CharSizeInBytes];

            return Encoding.GetChars(input, parsed) == NameSize && IsValidKeyName(parsed, allowBlank);
        }

        private protected static int FindComment(ReadOnlySpan<char> input)
        {
            //for(var i = input.Length - 1; i >= 0; i--)
            for (var i = 0; i < input.Length; i++)
                if (input[i] == '/')
                    return i;

            return input.Length;
        }

        private protected static int FindLastQuote(ReadOnlySpan<char> input)
        {
            var inQuotes = false;
            var lastQuotePos = 0;
            for (var i = 0; i < input.Length - 1; i++)
            {
                if (input[i] == '\'')
                {
                    lastQuotePos = i;
                    if (input[i + 1] != '\'')
                    {
                        if (inQuotes)
                            return i;

                        inQuotes = true;
                    }
                    else
                    {
                        i++;
                        lastQuotePos = i;
                    }
                }
            }

            if (input.Length >= 2 && input[^2] != '\'' && input[^1] == '\'')
                return input.Length - 1;

            //return input.Length;
            return lastQuotePos;
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

            var trimmedStr = input.TrimEnd();

            switch (trimmedStr.Length)
            {
                case FixedFitsKey.FixedFieldSize:
                {
                    layoutType = KeyType.Fixed;
                    // Check for mandatory decimal separator
                    foreach (var item in input)
                        if (item == '.')
                        {
                            numericType = NumericType.Float;
                            break;
                        }

                    break;
                }
                case 2 * FixedFitsKey.FixedFieldSize when trimmedStr.Length >= FixedFitsKey.FixedFieldSize:
                    // Fixed-format complex number
                    layoutType = KeyType.Fixed;
                    numericType = NumericType.Complex;
                    break;
                default:
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
                        if (item != ':') continue;

                        isComplex = true;
                        //break;
                    }

                    numericType = isComplex
                        ? NumericType.Complex
                        : (nDots > 0
                            ? NumericType.Float
                            : NumericType.Integer);
                    break;
                }
            }

            return true;

        }

        private protected static IFitsValue? ParseIntoInteger(
            ReadOnlySpan<char> value,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> comment)
        {
            if (value.TryParseRaw(out int iVal))
                return Create(name.ToString(), iVal, comment.IsEmpty ? null : comment.ToString());
            if (value.TryParseRaw(out long lVal))
                return Create(name.ToString(), lVal, comment.IsEmpty ? null : comment.ToString());
            return null;
        }

        private protected static IFitsValue? ParseIntoFloat(
            ReadOnlySpan<char> value,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> comment)
        {

            if (!value.TryParseRaw(out double dVal))
                return null;

            var fVal = (float) dVal;

            if (Internal.UnsafeNumerics.MathOps.AlmostEqual(dVal, fVal))
                return Create(name.ToString(), fVal, comment.IsEmpty ? null : comment.ToString());
            
            return Create(name.ToString(), dVal, comment.IsEmpty ? null : comment.ToString());
        }


        private protected static IFitsValue? ParseIntoSpecial(
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> content)
        {
            if (!name.StartsWith(@"CONTINUE".AsSpan()))
                return CreateSpecial(name.ToString(), content.ToString());
            var ind = FindLastQuote(content);
            if (ind <= 0)
                return null;

            var commentStart = FindComment(content.Slice(ind + 1));
            if (content.Slice(1, ind - 1).TryParseRaw(out string? strRep))
            {
                return new ContinueSpecialKey(
                    strRep!,
                    commentStart < content.Length - ind - 1
                        ? content.Slice(commentStart + 2 + ind).TrimEnd().ToString()
                        : null);
            }

            return null;

        }

        public static IFitsValue? ParseRawData(ReadOnlySpan<byte> input)
        {
            if (input.Length < EntrySizeInBytes)
                return null;
            // Keyword is text-encoded, so convert whole input to chars and do checks

            Span<char> charRep = stackalloc char[EntrySize];

            // Byte array should be exactly convertible to char array, especially when default encoding is ASCII
            if (Encoding.GetChars(input.Slice(0, EntrySizeInBytes), charRep) != EntrySize ||
                !((ReadOnlySpan<char>) charRep).IsStringHduCompatible(Encoding))
                return null;


            var name = ((ReadOnlySpan<char>) charRep.Slice(0, NameSize)).TrimEnd();
            if (!IsValidKeyName(name, true))
            {
                // If name is invalid, it can be a blank key
                if (BlankKey.IsBlank(charRep))
                    return CreateBlank();
                return ArbitraryKey.Create(charRep) is { } val ? val : null;
                // Possible other cases?
            }

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
                foreach (var symb in contentSpan)
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

                        return innerStrSpan.TryParseRaw(out string? str)
                            ? Create(name.ToString(),
                                str,
                                commentStart < contentSpan.Length - 1 - quoteEnd
                                    ? MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 2 + quoteEnd))
                                        .ToString()
                                    : null,
                                quoteEnd <= FixedFitsKey.FixedFieldSize
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
                                ? MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1)).ToString()
                                : null,
                            pos == FixedFitsKey.FixedFieldSize - 1
                                ? KeyType.Fixed
                                : KeyType.Free);
                    }
                    default:
                    {
                        var commentStart = FindComment(contentSpan);
                        var innerStrSpan = contentSpan.Slice(0, commentStart);
                        var isNumber = DetectNumericFormat(innerStrSpan, out var nType, out var keyType);
                        if (!isNumber)
                            return null;
                        var roSpan = MemoryExtensions.TrimEnd(innerStrSpan);


                        return nType switch
                        {
                            NumericType.Integer =>
                            ParseIntoInteger(roSpan, name,
                                commentStart < contentSpan.Length - 1
                                    ? MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1))
                                    : ReadOnlySpan<char>.Empty),
                            NumericType.Float =>
                            ParseIntoFloat(roSpan, name,
                                commentStart < contentSpan.Length - 1
                                    ? MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1))
                                    : ReadOnlySpan<char>.Empty),
                            NumericType.Complex when roSpan.TryParseRaw(out Complex cVal, keyType) =>
                            Create(
                                name.ToString(),
                                cVal,
                                commentStart < contentSpan.Length - 1
                                    ? MemoryExtensions.TrimEnd(contentSpan.Slice(commentStart + 1))
                                        .ToString()
                                    : null,
                                keyType),

                            _ => null
                        };
                    }

                }
            }

            // At this point, keyword has a valid name and HDU-compatible content
            var content = ((ReadOnlySpan<char>) charRep).Slice(EqualsPos).Trim();
            //return CreateSpecial(name.ToString(), content.ToString());
            return ParseIntoSpecial(name, content);
        }

        public static IFitsValue<T> Create<T>(string name, T value, string? comment = null,
            KeyType type = KeyType.Fixed)
            => type == KeyType.Free
                ? FreeFitsKey.Create(name, value, comment)
                : FixedFitsKey.Create(name, value, comment);

        public static IFitsValue Create(string name, object? value, string? comment = null,
            KeyType type = KeyType.Fixed)
            => type == KeyType.Free
                ? FreeFitsKey.Create(name, value, comment)
                : FixedFitsKey.Create(name, value, comment);

        public static IFitsValue CreateContinuation(string data, string? comment) =>
            new ContinueSpecialKey(data, comment);

        public static IFitsValue CreateBlank() => BlankKey.Blank;

        public static IFitsValue Create(string content) => new ArbitraryKey(content);

        public static IFitsValue CreateSpecial(string name, string data) => new SpecialKey(name, data);

        public static IFitsValue CreateEnd() => new SpecialKey("END", string.Empty);

        public static IFitsValue CreateComment(string comment) => new SpecialKey("COMMENT", comment);

        public static IFitsValue CreateHistory(string history) => new SpecialKey("HISTORY", history);

        public static ImmutableArray<IFitsValue> ToComments(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty || !text.IsStringHduCompatible())
                return ImmutableArray<IFitsValue>.Empty;


            var maxCommentSize = EntrySize - ValueStart;

            var n = (text!.Length + maxCommentSize - 1) / maxCommentSize;

            var builder = ImmutableArray.CreateBuilder<IFitsValue>(n);

            var i = 0;
            for (; i < n - 1; i++)
                builder.Add(CreateComment(text.Slice(i * maxCommentSize, maxCommentSize).ToString()));

            builder.Add(CreateComment(text.Slice(i * maxCommentSize).ToString()));

            return builder.ToImmutable();
        }

        public static ImmutableArray<IFitsValue> ToComments(string? text)
            => ToComments(string.IsNullOrEmpty(text) ? ReadOnlySpan<char>.Empty : text.AsSpan());

        public static ImmutableArray<IFitsValue> ToContinueKeys(
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> comment,
            string keyName)
        {

            if (!IsValidKeyName((keyName ?? string.Empty).AsSpan()))
                throw new ArgumentException(SR.InvalidArgument, nameof(keyName));

            if (!text.IsStringHduCompatible()
                || !comment.IsStringHduCompatible())
                throw new ArgumentException(SR.InvalidArgument);

            if (text.IsEmpty && comment.IsEmpty)
                return ImmutableArray<IFitsValue>.Empty;

            var strLen = text.StringSizeWithQuoteReplacement();

            // Accounting for `&` symbol
            const int singleStrSize = EntrySize - ValueStart - 1;
            // Accounting for two single-quotes, space, '/', space
            const int maxCommentLength = singleStrSize - 5;


            if (TryValidateInput(keyName, strLen, comment.Length))
            {

                string? commentStr = null;
                if (!comment.IsEmpty)
                {
                    commentStr = comment[0] != ' ' && strLen + comment.Length < EntrySize - ValueStart - 1
                        ? ' ' + comment.ToString()
                        : comment.ToString();
                }

                // Input is small enough to fit into one key
                return new IFitsValue[]
                    {
                        Create(
                            keyName!,
                            text.ToString(),
                            commentStr,
                            KeyType.Free)
                    }
                    .ToImmutableArray();
            }


            var n =
                // Extra space for convenience
                2
                // Predicting number of text keys
                + (strLen + singleStrSize - 1) / singleStrSize
                // And number of comment keys
                + (comment.Length + maxCommentLength - 1) / maxCommentLength;

            var builder = ImmutableArray.CreateBuilder<IFitsValue>(n);

            var offset = 0;
            string? lastKeyStr = null;


            for (var i = 0;; i++)
            {
                var currentChunk = text[offset..];
                var (numSrcSymb, _) = currentChunk.MaxCompatibleStringSize(singleStrSize);
                if (i != 0)
                {
                    builder.Add(i == 1
                        ? Create(keyName!, lastKeyStr + "&", null, KeyType.Free)
                        : CreateContinuation(lastKeyStr + "&", null));
                }

                if (numSrcSymb > 0)
                    lastKeyStr = currentChunk[..numSrcSymb].ToString();

                offset += numSrcSymb;
                if (offset >= text.Length)
                    break;
            }

            // At this point, `lastKeyStr` can be null if source text is empty
            // If it is null, there is a non-null comment, but this check is still needed

            string? lastCommentStr = null;
            if (!comment.IsEmpty)
            {
                // Comments are not "escaped" so we can copy as-is, fixed-length
                // CONTINUE=_'&' / *rest of the comment*
                // Size of *rest of the comment*
                offset = 0;
                for (var i = 0;; i++)
                {
                    var currentChunk = comment[offset..];

                    if (i == 0)
                    {
                        if (lastKeyStr is { })
                        {
                            // The content is enough to fill only one keyword, which has not been added yet
                            var strSize = lastKeyStr.AsSpan().StringSizeWithQuoteReplacement(0);
                            if (strSize < singleStrSize - 5)
                            {
                                // Room for some comment
                                var commSize = Math.Min(singleStrSize - strSize - 3, currentChunk.Length);

                                lastCommentStr = " " + currentChunk[..commSize].ToString();
                                offset += commSize;
                            }
                        }
                        else
                        {
                            var commSize = Math.Min(currentChunk.Length, maxCommentLength);
                            lastKeyStr = string.Empty;
                            lastCommentStr = " " + currentChunk[..commSize].ToString();


                            offset += commSize;
                        }
                    }
                    else
                    {
                        builder.Add(builder.Count == 0
                            ? Create(keyName!,
                                lastKeyStr + "&",
                                lastCommentStr,
                                KeyType.Free)
                            : CreateContinuation(
                                lastKeyStr + "&",
                                lastCommentStr));
                        var commSize = Math.Min(currentChunk.Length, maxCommentLength);
                        lastKeyStr = string.Empty;
                        lastCommentStr = " " + currentChunk[..commSize].ToString();
                        offset += commSize;
                    }


                    if (offset >= comment.Length)
                        break;
                }

            }

            if (lastKeyStr is null)
                throw new InvalidOperationException(SR.InvalidOperation);

            builder.Add(
                builder.Count == 0
                    ? Create(keyName!, lastKeyStr, lastCommentStr, KeyType.Free)
                    : CreateContinuation(lastKeyStr, lastCommentStr));

            return builder.ToImmutable();
        }


        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        public static (string Text, string Comment) ParseContinuedString(
            IEnumerable<IFitsValue> keys,
            bool commentSpacePrefixed = false)
        {
            
            using var textSb = new TextExtensions.SimpleStringBuilder(4 * EntrySize);
            using var commSb = new TextExtensions.SimpleStringBuilder(4 * EntrySize);

            var index = 0;
            var textWritten = 0;
            foreach (var key in keys)
            {
                if (index++ == 0)
                {
                    if (key is IFitsValue<string> firstKey)
                    {
                        textSb.Append(firstKey.RawValue);
                        if (commentSpacePrefixed
                            && !string.IsNullOrEmpty(firstKey.Comment)
                            && firstKey.Comment[0] == ' ')
                            commSb.Append(firstKey.Comment.AsSpan(1));
                        else
                            commSb.Append(firstKey.Comment);

                        textWritten = firstKey.RawValue.Length;
                    }
                    else
                        throw new InvalidOperationException(SR.InvalidOperation);
                }
                else if (key is IStringLikeValue continueKey)
                {
                    if (textWritten > 0 && textSb.View()[^1] == '&')
                        textSb.DeleteBack();

                    textSb.Append(continueKey.RawValue ?? string.Empty);

                    if (commentSpacePrefixed
                        && !string.IsNullOrEmpty(continueKey.Comment)
                        && continueKey.Comment[0] == ' ')
                        commSb.Append(continueKey.Comment.AsSpan(1));
                    else
                        commSb.Append(continueKey.Comment);

                    textWritten = continueKey.RawValue?.Length ?? 0;
                }
                else
                    break;
            }

            return (Text: textSb.ToString(), Comment: commSb.ToString());
        }

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        public static string ParseCommentString(
            IEnumerable<IFitsValue> keys)
        {
            using var textSb = new TextExtensions.SimpleStringBuilder(4 * EntrySize);

            foreach (var key in keys)
            {
                if (key is ISpecialKey specKey && specKey.Name == @"COMMENT")
                    textSb.Append(key.Comment);
                else break;
            }

            return textSb.ToString();
        }

        public virtual bool Equals(IFitsValue? other)
            => other is { }
               && Name == other.Name
               //&& Type == other.Type
               && Comment == other.Comment;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is IFitsValue other && Equals(other);
        }

        public override int GetHashCode()
            => unchecked((Name.GetHashCode() * 397) ^ Comment.GetHashCode());
    }
}