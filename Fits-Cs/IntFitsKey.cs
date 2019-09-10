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
using System.Runtime.CompilerServices;


[assembly:InternalsVisibleTo("Sandbox")]
namespace FitsCs
{
    public class FixedIntKey : FixedFitsKey, IFitsValue<int>
    {
        private const int FieldSize = 20;
        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public override string ToString(bool prefixType)
        {
            return ToString();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool TryFormat(Span<char> span, out int charsWritten)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            charsWritten = 0;
            var len = EqualsPos + 2 +
                      FieldSize +
                      (!isCommentNull
                          ? Comment.Length + 2
                          : 0);

            if (span.Length < len)
                return false;
            
            span.Slice(0, len).Fill(' ');
            Name.AsSpan().CopyTo(span);
            span[EqualsPos] = '=';
            string.Format($"{{0,{FieldSize}}}", RawValue).AsSpan().CopyTo(span.Slice(EqualsPos + 2));

            charsWritten = FieldSize + NameSize + 2;

            if (!isCommentNull)
            {
                Comment.AsSpan().CopyTo(span.Slice(charsWritten + 2));
                span[charsWritten + 1] = '/';
                charsWritten += 2 + Comment.Length;
            }

            return true;
        }

        public int RawValue { get; }
        internal FixedIntKey(string name, int value, string comment = "") : base(name, comment)
        {
            RawValue = value;
        }

    }
}
