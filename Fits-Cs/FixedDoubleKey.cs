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

using System;
using Maybe;


namespace FitsCs
{
    public sealed class FixedDoubleKey : FixedFitsKey, IFitsValue<double>
    {
        private protected override string TypePrefix => @"[double]";

        public override object Value => RawValue.Match(x => (object)x);
        public override bool IsEmpty => false;
        public Maybe<double> RawValue { get; }


        public override bool TryFormat(Span<char> span)
            => TryFormat(
                span,
                //RawValue.Match(x => string.Format($"= {{0,{FixedFieldSize}:0.#############E+00}}", x), string.Empty));
                RawValue.Match(x => $"= {x.FormatDouble(17, FixedFieldSize)}", string.Empty));

        internal FixedDoubleKey(string name, Maybe<double> value, string comment = "") : base(name, comment)
        {
            ValidateInput(name, comment, value.Match(x => FixedFieldSize + 2));
            RawValue = value;
        }

    }
}