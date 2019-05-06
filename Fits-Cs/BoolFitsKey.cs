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
using Compatibility.Bridge;

namespace FitsCs
{
    public sealed class BoolFitsKey : FitsKey
    {
        public class Updater
        {
            private string _name;
            private string _comment;

            public string Name
            {
                get => _name;
                set
                {
                    if (string.IsNullOrWhiteSpace(value))
                        throw new ArgumentNullException(nameof(value));
                    _name = value;
                }
            }

            public string Comment
            {
                get => _comment;
                set => _comment = value ?? string.Empty;
            }
            public bool Value;
        }

        private const string TypePrefix = @"[bool ]";
        public const int ValuePositionFixed = 30;

        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public bool RawValue { get; }

        public BoolFitsKey(string name, bool value, string comment = "")
            : base(name, comment)
        {
            RawValue = value;
        }

        public BoolFitsKey WithUpdates(Action<Updater> modifier)
        {
            if (modifier is null)
                throw new ArgumentNullException(nameof(modifier));

            var upd = new Updater()
            {
                Comment = Comment,
                Name = Name,
                Value = RawValue
            };
            modifier(upd);

            return new BoolFitsKey(upd.Name, upd.Value, upd.Comment);
        }

        public override string ToString()
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            char[] buffer = null;
            try
            {
                var len = (Comment?.Length ?? 0) + ValuePositionFixed + 4;
                if (len > EntrySize)
                    len = EqualsPos + 2 + (isCommentNull ? 0 : 3 + Comment.Length);
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

        public override string ToString(bool prefixType)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            char[] buffer = null;
            try
            {
                var len = (Comment?.Length ?? 0) + ValuePositionFixed + 4;
                if (len > EntrySize)
                    len = EqualsPos + 2 + (isCommentNull ? 0 : 3 + Comment.Length);
                buffer = ArrayPool<char>.Shared.Rent(len + TypePrefix.Length);
                var span = buffer.AsSpan(0, len + TypePrefix.Length);
                if (!TryFormat(span.Slice(TypePrefix.Length), out _))
                    throw new FormatException("Failed to format keyword");
                TypePrefix.AsSpan().CopyTo(buffer);
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
            var len = (Comment?.Length ?? 0) + ValuePositionFixed + 4;
            charsWritten = 0;
            if (len <= EntrySize)
            {
                if (len > span.Length)
                    return false;
                span.Fill(' ');

                // Fits into fixed-form field
                Name.AsSpan().CopyTo(span);
                span[EqualsPos] = '=';
                span[EqualsPos + 1] = ' ';
                span[ValuePositionFixed] = RawValue ? 'T' : 'F';
                if (!isCommentNull)
                {
                    span[ValuePositionFixed + 1] = ' ';
                    span[ValuePositionFixed + 2] = '/';
                    span[ValuePositionFixed + 3] = ' ';
                    Comment.AsSpan().CopyTo(span.Slice(ValuePositionFixed + 4));
                }
            }
            else
            {
                len = EqualsPos + 2 + (isCommentNull ? 0 : 3 + Comment.Length);
                if (len > span.Length)
                    return false;
                span.Fill(' ');

                Name.AsSpan().CopyTo(span);
                span[EqualsPos] = '=';
                span[EqualsPos + 1] = ' ';
                span[EqualsPos + 2] = RawValue ? 'T' : 'F';
                if (!isCommentNull)
                {
                    span[EqualsPos + 3] = ' ';
                    span[EqualsPos + 4] = '/';
                    span[EqualsPos + 5] = ' ';
                    Comment.AsSpan().CopyTo(span.Slice(EqualsPos + 6));
                }
            }

            charsWritten = len;
            return true;
        }
    }
}
