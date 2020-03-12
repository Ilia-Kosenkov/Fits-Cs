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
using System.Numerics;

namespace FitsCs.Keys
{
    public sealed class FreeComplexKey : FreeFitsKey, IFitsValue<Complex>
    {
        private protected override string TypePrefix => @"cmp";
        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public Complex RawValue { get; }

        public override bool TryFormat(Span<char> span)
        {
            const int fieldSize = 24;
            Span<char> buff = stackalloc char[2 * fieldSize + 3];
            buff.Fill(' ');
            buff[0] = '=';
            buff[2 + fieldSize] = ':';

            if (!RawValue.Real.TryFormatDouble(17, fieldSize, buff.Slice(2, fieldSize)))
                throw new InvalidOperationException(SR.ShouldNotHappen);

            if (!RawValue.Imaginary.TryFormatDouble(17, fieldSize, buff.Slice(3 + fieldSize, fieldSize)))
                throw new InvalidOperationException(SR.ShouldNotHappen);

            return TryFormat(span, buff);
        }
        internal FreeComplexKey(string name, Complex value, string? comment) 
            : base(name, comment, 2 + 2 * 24 + 1)
        {
            // Conservative size estimate - 24 is the total size of %+24.17e+3
            // Multiplying by 2 and 1 symbol for column separator
            RawValue = value;
        }

        public bool Equals(IFitsValue<Complex>? other)
            => other is { }
               && base.Equals(other)
               && Internal.UnsafeNumerics.MathOps.AlmostEqual(RawValue.Real, other.RawValue.Real)
               && Internal.UnsafeNumerics.MathOps.AlmostEqual(RawValue.Imaginary, other.RawValue.Imaginary);

        public override bool Equals(IFitsValue? other)
            => other is IFitsValue<Complex> key
               && Equals(key);

        public override int GetHashCode()
            => unchecked((base.GetHashCode() * 397) ^ RawValue.GetHashCode());
    }
}
