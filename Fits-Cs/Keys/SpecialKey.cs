#nullable enable
using System;

namespace FitsCs.Keys
{
    public enum SpecialKeyType : byte
    {
        Undefined = 0,
        End = 1,
        Comment = 2,
        History = 3,
        Continue = 4
    }

    public class SpecialKey : FitsKey
    {
        internal SpecialKey(string name, string? data) 
            : base(name, data, 1)
        {
            KeyType = name.ToLower() switch
            {
                @"end" => SpecialKeyType.End,
                @"comment" => SpecialKeyType.Comment,
                @"history" => SpecialKeyType.History,
                @"continue" => SpecialKeyType.Continue,
                _ => SpecialKeyType.Undefined
            };
        }

        private protected override string TypePrefix => @"spcl";
        public override object? Value => null;
        public override bool IsEmpty => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Comment);

        public SpecialKeyType KeyType { get; }

        public override bool TryFormat(Span<char> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            var isCommentNull = string.IsNullOrWhiteSpace(Comment);

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            Name.AsSpan().CopyTo(span);

            if (isCommentNull) return true;

            //Comment.AsSpan().CopyTo(span.Slice(ValueStart));
            // This accounts for the absence of `= ` in special keys
            Comment.AsSpan().CopyTo(span.Slice(ValueStart - 1));

            return true;
        }
    }
}
