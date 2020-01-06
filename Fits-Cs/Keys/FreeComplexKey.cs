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
        private protected override string TypePrefix => @"cmplx";
        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public Complex RawValue { get; }

        public override bool TryFormat(Span<char> span)
            => TryFormat(
                span,
                $"= {RawValue.Real.FormatDouble(17,24)}:{RawValue.Imaginary.FormatDouble(17, 24)}");


        internal FreeComplexKey(string name, Complex value, string comment) 
            : base(name, comment, 2 + 2 * 24 + 1)
        {
            // Conservative size estimate - 24 is the total size of %+24.17e+3
            // Multiplying by 2 and 1 symbol for column separator
            RawValue = value;
        }
    }
}
