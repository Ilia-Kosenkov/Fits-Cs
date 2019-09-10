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
using MemoryExtensions;

namespace FitsCs
{
    public sealed class StringFitsKey : FitsKey
    {
        private const string TypePrefix = @"[string]";

        private const int MaxStrLength = EntrySize - 11 - 2;
        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public override string ToString(bool prefixType)
        {
            if (prefixType)
            {
                var isCommentNull = string.IsNullOrWhiteSpace(Comment);
                char[] buffer = null;
                try
                {
                    var len = EqualsPos + 2 + 2 + RawValue.Length +
                              (!isCommentNull
                                  ? Comment.Length + 3
                                  : 0);
                    buffer = ArrayPool<char>.Shared.Rent(len + TypePrefix.Length);
                    var span = buffer.AsSpan(0, len + TypePrefix.Length);
                    if (!TryFormat(span.Slice(TypePrefix.Length), out _))
                        throw new FormatException("Failed to format keyword");
                    TypePrefix.AsSpan().CopyTo(span);
                    return span.TrimEnd().ToString();
                }
                finally
                {
                    if (!(buffer is null))
                        ArrayPool<char>.Shared.Return(buffer, true);
                }
            }

            return ToString();
        }

        public override string ToString()
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            char[] buffer = null;
            try
            {
                var len = EqualsPos + 2 + 2 + RawValue.Length +
                          (!isCommentNull
                              ? Comment.Length + 3
                              : 0);
                buffer = ArrayPool<char>.Shared.Rent(len);
                var span = buffer.AsSpan(0, len);
                if (!TryFormat(span, out _))
                    throw new FormatException("Failed to format keyword");
                return span.TrimEnd().ToString();
            }
            finally
            {
                if (!(buffer is null))
                    ArrayPool<char>.Shared.Return(buffer, true);
            }
        }

        public override bool TryFormat(Span<char> span, out int charsWritten)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            charsWritten = 0;
            var len = EqualsPos + 2 + 2 + RawValue.Length +
                      (!isCommentNull
                          ? Comment.Length + 3
                          : 0);
            if (span.Length < len)
                return false;

            span.Slice(0, len).Fill(' ');

            if (!Name.AsSpan().TryCopyTo(span))
                return false;
            span[EqualsPos] = '=';
            span[EqualsPos + 2] = '\"';
            if (!RawValue.AsSpan().TryCopyTo(span.Slice(EqualsPos + 3)))
                return false;
            var offset = EqualsPos + 3 + RawValue.Length ;
            span[offset] = '\"';

            if (!isCommentNull)
            {
                span[offset + 2] = '/';
                if(!Comment.AsSpan().TryCopyTo(span.Slice(offset + 4)))
                    return false;
            }

            return true;
        }

        public string RawValue { get; }

        public StringFitsKey(string name, string value, string comment = "")
            : base(name, comment)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length > MaxStrLength)
                throw new ArgumentException($"String value cannot be longer than {MaxStrLength} symbols.",
                    nameof(value));

            RawValue = value.Trim();
        }
    }
}
