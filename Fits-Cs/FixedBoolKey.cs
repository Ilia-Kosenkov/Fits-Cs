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


using Maybe;
using System;

namespace FitsCs
{
    public sealed class FixedBoolKey : FixedFitsKey, IFitsValue<bool>
    {
        private const int FieldSize = 20;
        private const char TrueConst = 'T';
        private const char FalseConst = 'F';
        private protected override string TypePrefix => @"[   int]";

        public override object Value => RawValue.Match(x => (object)x);
        public override bool IsEmpty => false;
        public Maybe<bool> RawValue { get; }

        public override bool TryFormat(Span<char> span, out int charsWritten)
            => FormatFixed(
                span, 
                RawValue.Match(x => string.Format($"= {{0,{FieldSize}}}", x ? TrueConst : FalseConst), string.Empty),
                out charsWritten);


        internal FixedBoolKey(string name, Maybe<bool> value, string comment) : base(name, comment)
        {
            RawValue = value;
        }
    }
}
