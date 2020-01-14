#nullable enable
using System;

namespace FitsCs.Keys
{
    public class ContinueSpecialKey : SpecialKey, IFitsValue<string>
    {
        internal ContinueSpecialKey(string name, string data, string? comment) : base(name, comment)
        {
            ValidateInput(name, data.AsSpan().StringSizeWithQuoteReplacement(0) + 2, comment?.Length ?? 0);

            if(!data.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(data));

            if (!name.StartsWith(@"CONTINUE"))
                throw new ArgumentException(SR.InvalidArgument, nameof(name));

            RawValue = data;
        }

        public override bool IsEmpty => false;

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


            if (isCommentNull) return true;

            // Comment padding if it can be fit in the entry
            if (len < FixedFitsKey.FixedFieldSize + ValueStart && Comment.Length <= EntrySize - FixedFitsKey.FixedFieldSize - ValueStart - 2)
                len = ValueStart + FixedFitsKey.FixedFieldSize;

            Comment.AsSpan().CopyTo(span.Slice(len + 2));
            span[len + 1] = '/';

            return true;
        }

        public string RawValue { get; }
    }
}