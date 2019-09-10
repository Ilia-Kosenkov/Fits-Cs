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
using TextExtensions;


namespace FitsCs
{
    public class FixedIntKey : FixedFitsKey, IFitsValue<int>
    {
        private const int FieldSize = 20;
        private const string TypePrefix = @"[   int]";

        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public int RawValue { get; }

        public override string ToString(bool prefixType) 
            => prefixType ? TypePrefix + ToString() : ToString();

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

        public override bool TryFormat(Span<char> span, out int charsWritten)
            => FormatFixed(span, string.Format($"{{0,{FieldSize}}}", RawValue), out charsWritten);

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

        internal FixedIntKey(string name, int value, string comment = "") : base(name, comment)
        {
            ValidateInput(name, comment, FieldSize);
            RawValue = value;
        }

    }
}
