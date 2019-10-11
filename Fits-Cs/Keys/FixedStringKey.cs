﻿using System;

namespace FitsCs.Keys
{
    public sealed class FixedStringKey : FixedFitsKey, IFitsValue<string>
    {
        private protected override string TypePrefix => @"[string]";


        public override object Value => RawValue;
        public string RawValue { get; }
        public override bool TryFormat(Span<char> span)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            var len = NameSize +
                      RawValue.AsSpan().StringSizeWithQuoteReplacement() + 2;

            if (span.Length < EntrySizeInBytes)
                return false;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);
            span[EqualsPos] = '=';
            
            if (!RawValue.AsSpan().TryGetCompatibleString(span.Slice(ValueStart)))
            {
                span.Slice(0, EntrySizeInBytes).Fill(' ');
                return false;
            }

            if (!isCommentNull)
            {
                Comment.AsSpan().CopyTo(span.Slice(len + 2));
                span[len + 1] = '/';
            }

            return true;
        }

        internal FixedStringKey(string name, string value, string comment) : base(name, comment)
        {
            if (value is null)
                throw new ArgumentNullException(SR.NullArgument);
            
            ValidateInput(name, comment, value.AsSpan().StringSizeWithQuoteReplacement() + 2);
            if (!value.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(value));
            RawValue = value;
        }
    }
}