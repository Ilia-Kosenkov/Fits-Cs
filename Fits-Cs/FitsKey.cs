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
using System.Text;
using FitsCs.Internals;

namespace FitsCs
{
    public class FitsKey
    {
        public const int NameSize = 8;
        public const int EqualsPos = 8;
        public const int ValueStart = 10;
        public const int EntrySize = 80;

        private static readonly int AsciiCharSize = Encoding.ASCII.GetMaxCharCount(1);

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

            if (input.Length == NameSize)
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
    }
}
