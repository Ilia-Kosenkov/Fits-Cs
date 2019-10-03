using System;
using Maybe;

namespace FitsCs
{
    public sealed class FreeStringKey : FreeFitsKey, IFitsValue<string>
    {
        public override object Value => RawValue.Match(x => (object)x);
        public override bool IsEmpty => RawValue.Match(_ => true);
        public Maybe<string> RawValue { get; }
        public override bool TryFormat(Span<char> span)
        {
            var isCommentNull = string.IsNullOrWhiteSpace(Comment);
            var len = NameSize +
                      RawValue.Match(x => x.AsSpan().StringSizeWithQuoteReplacement(0) + 2);

            if (span.Length < EntrySizeInBytes)
                return false;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);
            span[EqualsPos] = '=';

            if (!RawValue.Match(string.Empty).AsSpan().TryGetCompatibleString(span.Slice(ValueStart), 0))
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

        internal FreeStringKey(string name, Maybe<string> value, string comment) : base(name, comment)
        {
            ValidateInput(name, comment, value.Match( x => x.AsSpan().StringSizeWithQuoteReplacement(0) + 2));
            RawValue = value;
        }
    }
}
