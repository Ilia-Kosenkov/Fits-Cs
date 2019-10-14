using System;

namespace FitsCs.Keys
{
    public enum SpecialKeyType : byte
    {
        Undefined = 0,
        End = 1,
        Comment = 2,
        History = 3
    }

    public sealed class SpecialKey : FitsKey
    {
        internal SpecialKey(string name, string data) 
            : base(name, data, 0)
        {
            switch (name.ToLower())
            {
                case @"end":
                    KeyType = SpecialKeyType.End;
                    break;
                case @"comment":
                    KeyType = SpecialKeyType.Comment;
                    break;
                case @"history":
                    KeyType = SpecialKeyType.History;
                    break;
                default:
                    KeyType = SpecialKeyType.Undefined;
                    break;
            }
        }

        private protected override string TypePrefix => @"spcl";
        public override object Value => null;
        public override bool IsEmpty => true;

        public SpecialKeyType KeyType { get; }

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
