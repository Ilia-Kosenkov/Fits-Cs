#nullable enable
using System;

namespace FitsCs.Keys
{
    public interface IStringLikeValue : IFitsValue
    {
        string RawValue { get; }
    }

    public class ContinueSpecialKey : SpecialKey, IStringLikeValue
    {
        internal ContinueSpecialKey(string data, string? comment) : base(@"CONTINUE", comment)
        {
            var strSize = data.AsSpan().StringSizeWithQuoteReplacement(0);
            ValidateInput(@"CONTINUE", strSize + 2, comment?.Length ?? 0);

            if(!data.AsSpan().IsStringHduCompatible())
                throw new ArgumentException(SR.HduStringIllegal, nameof(data));

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

            Comment.AsSpan().CopyTo(span.Slice(len + 2));
            span[len + 1] = '/';

            return true;
        }

        public string RawValue { get; }
    }
}