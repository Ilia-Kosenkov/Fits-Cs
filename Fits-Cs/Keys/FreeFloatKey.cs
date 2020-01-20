﻿//     MIT License
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
    public sealed class FreeFloatKey : FreeFitsKey, IFitsValue<float>
    {
        private protected override string TypePrefix => @"float";
        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public float RawValue { get; }

        public override bool TryFormat(Span<char> span)
            => TryFormat(
                span,
                $"= {RawValue.FormatFloat(9, 15)}");


        internal FreeFloatKey(string name, float value, string? comment)
            : base(name, comment, 2 + 15)
        {
            // Conservative size estimate - 15 is the total size of %+15.9e+2
            RawValue = value;
        }

        public bool Equals(IFitsValue<float>? other)
            => other is { }
               && base.Equals(other)
               && RawValue.CorrectEquals(other.RawValue);

        public override bool Equals(IFitsValue? other)
            => other is IFitsValue<float> key
               && Equals(key);

        public override int GetHashCode()
            => unchecked((base.GetHashCode() * 397) ^ RawValue.GetHashCode());
    }
}
