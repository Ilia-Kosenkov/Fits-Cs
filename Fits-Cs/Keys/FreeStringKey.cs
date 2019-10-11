using System;

namespace FitsCs.Keys
{
    public sealed class FreeStringKey : FreeFitsKey, IFitsValue<string>
    {
        private protected override string TypePrefix => @"[string]";

        public override object Value => RawValue;
        public override bool IsEmpty => false;
        public string RawValue { get; }
        public override bool TryFormat(Span<char> span)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            var len = NameSize +
                      RawValue.AsSpan().StringSizeWithQuoteReplacement(0) + 2;

            if (span.Length < EntrySizeInBytes)
                return false;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);
            span[EqualsPos] = '=';

            if (!RawValue.AsSpan().TryGetCompatibleString(span.Slice(ValueStart), 0))
            {
                span.Slice(0, EntrySizeInBytes).Fill(' ');
                return false;
            }


            if (!isCommentNull)
            {
            // Comment padding if it can be fit in the entry
                if (len < FixedFitsKey.FixedFieldSize + ValueStart && Comment.Length < EntrySize - FixedFitsKey.FixedFieldSize - ValueStart - 2)
                    len = ValueStart + FixedFitsKey.FixedFieldSize;

                Comment.AsSpan().CopyTo(span.Slice(len + 2));
                span[len + 1] = '/';
            }

            return true;
        }

        internal FreeStringKey(string name, string value, string comment) : base(name, comment)
        {
            ValidateInput(name, comment, value.AsSpan().StringSizeWithQuoteReplacement(0) + 2);
            
            if(!value.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(value));

            RawValue = value;
        }
    }
}
