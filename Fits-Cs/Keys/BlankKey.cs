#nullable enable
using System;

namespace FitsCs.Keys
{
    public sealed class BlankKey : FitsKey
    {
        public static BlankKey Blank { get; } = new BlankKey();
        internal BlankKey() 
            : base(string.Empty, string.Empty, 0)
        {
        }

        private protected override string TypePrefix => @"blank";
        public override object? Value => null;
        public override bool IsEmpty => true;
        public override bool TryFormat(Span<char> span)
        {
            if (span.Length < EntrySizeInBytes)
                return false;

            span.Slice(0, EntrySizeInBytes).Fill(' ');
            return true;
        }

        public override bool Equals(IFitsValue? other)
            => other is BlankKey;

        public static bool IsBlank(ReadOnlySpan<char> input)
        {
            foreach (var item in input)
                if (item != ' ')
                    return false;
            return true;
        }
    }
}
