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

namespace FitsCs.Keys
{
    public sealed class FixedFloatKey : 
        FixedFitsKey, IFitsValue<float>, IEquatable<IFitsValue<double>>
    {
        private protected override string TypePrefix => @"flt";

        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public float RawValue { get; }


        public override bool TryFormat(Span<char> span)
        {
            Span<char> buff = stackalloc char[FixedFieldSize + 2];
            buff.Fill(' ');
            buff[0] = '=';
            if(!RawValue.TryFormatFloat(9, FixedFieldSize, buff[2..]))
                throw new InvalidOperationException(SR.ShouldNotHappen);

            return TryFormat(span, buff);
        }

        internal FixedFloatKey(string name, float value, string? comment = "") 
            : base(name, comment, FixedFieldSize + 2)
        {
            RawValue = value;
        }

        public bool Equals(IFitsValue<float>? other)
            => other is { }
               && base.Equals(other)
               && RawValue.CorrectEquals(other.RawValue);

        public bool Equals(IFitsValue<double>? other)
            => other is { }
               && base.Equals(other)
               && RawValue.CorrectEquals(other.RawValue);

        public override bool Equals(IFitsValue? other)
            => other is IFitsValue<float> fKey && Equals(fKey)
               || other is IFitsValue<double> dKey && Equals(dKey);
        
        public override int GetHashCode()
            => unchecked((base.GetHashCode() * 397) ^ RawValue.GetHashCode());
    }
}
