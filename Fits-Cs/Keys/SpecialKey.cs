using System;

namespace FitsCs.Keys
{

    public sealed class SpecialKey : FitsKey
    {
        internal SpecialKey(string name, string data) : base(name, data)
        {
            ValidateInput(name, data, 0);
        }

        private protected override string TypePrefix => @"spcl";
        public override object Value => null;
        public override bool IsEmpty => true;

        public override bool TryFormat(Span<char> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            var isCommentNull = string.IsNullOrWhiteSpace(Comment);

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);

            if (isCommentNull) return true;

            Comment.AsSpan().CopyTo(span.Slice(ValueStart));

            return true;
        }
    }
}
